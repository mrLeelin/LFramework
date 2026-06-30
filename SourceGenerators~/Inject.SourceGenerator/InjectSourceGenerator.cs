using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LFramework.Inject.SourceGenerator
{
    /// <summary>
    /// Builds generated partial injectors for LFramework assemblies.
    /// </summary>
    /// <remarks>
    /// Unity loads this analyzer during C# compilation. The generated source is emitted into Unity's
    /// compiler-generated output area instead of the UPM package source, matching the DOTS-style model
    /// where generated partials are not committed.
    /// </remarks>
    [Generator]
    public sealed class InjectSourceGenerator : ISourceGenerator
    {
        private const string InjectAttributeName = "LFramework.Runtime.InjectAttribute";
        private const string InjectHelperName = "__LFrameworkInjectGenerated";
        private static readonly DiagnosticDescriptor PartialClassRequired = new DiagnosticDescriptor(
            "LFI001",
            "Inject target must be partial",
            "Type '{0}' contains [Inject] members and must be declared partial so LFramework can generate a zero-reflection injector",
            "LFramework.Inject",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor PropertySetterRequired = new DiagnosticDescriptor(
            "LFI002",
            "Inject property must have a setter",
            "Property '{0}' is marked [Inject] but has no setter; use a private setter or inject a field",
            "LFramework.Inject",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor StaticMemberUnsupported = new DiagnosticDescriptor(
            "LFI003",
            "Inject member cannot be static",
            "Member '{0}' is marked [Inject] but static injection is not supported",
            "LFramework.Inject",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor UnsupportedTargetType = new DiagnosticDescriptor(
            "LFI004",
            "Inject target type is unsupported",
            "Type '{0}' contains [Inject] members but generated injection only supports top-level classes",
            "LFramework.Inject",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor UnsupportedInjectMember = new DiagnosticDescriptor(
            "LFI005",
            "Inject member is unsupported",
            "Member '{0}' is marked [Inject] but generated injection only supports fields and properties",
            "LFramework.Inject",
            DiagnosticSeverity.Error,
            true);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new Receiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (!(context.SyntaxReceiver is Receiver receiver))
            {
                return;
            }

            if (context.Compilation.GetTypeByMetadataName("LFramework.Runtime.IInjectable") == null ||
                context.Compilation.GetTypeByMetadataName("LFramework.Runtime.IServiceResolver") == null)
            {
                return;
            }

            var targets = FindTargets(context.Compilation, receiver.Candidates, context.ReportDiagnostic);
            if (targets.Count == 0)
            {
                return;
            }

            context.AddSource("LFramework.Inject.Generated.g.cs", SourceText.From(BuildSource(targets), Encoding.UTF8));
        }

        private static List<InjectTarget> FindTargets(
            Compilation compilation,
            IEnumerable<TypeDeclarationSyntax> candidates,
            Action<Diagnostic> reportDiagnostic)
        {
            var targets = new List<InjectTarget>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var candidate in candidates)
            {
                var model = compilation.GetSemanticModel(candidate.SyntaxTree);
                if (!(model.GetDeclaredSymbol(candidate) is INamedTypeSymbol type))
                {
                    continue;
                }

                var validTarget = true;
                if (!(candidate is ClassDeclarationSyntax) ||
                    type.TypeKind != TypeKind.Class ||
                    type.ContainingType != null)
                {
                    reportDiagnostic(Diagnostic.Create(
                        UnsupportedTargetType,
                        GetLocation(candidate.Identifier),
                        type.ToDisplayString()));
                    validTarget = false;
                }

                if (!candidate.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    reportDiagnostic(Diagnostic.Create(
                        PartialClassRequired,
                        GetLocation(candidate.Identifier),
                        type.ToDisplayString()));
                    validTarget = false;
                }

                var members = FindInjectMembers(type, reportDiagnostic);
                if (!validTarget)
                {
                    continue;
                }

                if (members.Count == 0)
                {
                    continue;
                }

                var key = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (seen.Add(key))
                {
                    targets.Add(new InjectTarget(type, members, HasInjectableBase(type.BaseType)));
                }
            }

            targets.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return targets;
        }

        private static List<InjectMember> FindInjectMembers(INamedTypeSymbol type, Action<Diagnostic> reportDiagnostic)
        {
            var members = new List<InjectMember>();
            foreach (var member in type.GetMembers())
            {
                if (!TryGetMetadata(member, out var metadata))
                {
                    continue;
                }

                if (member.IsStatic)
                {
                    reportDiagnostic(Diagnostic.Create(
                        StaticMemberUnsupported,
                        GetLocation(member),
                        member.ToDisplayString()));
                    continue;
                }

                if (member is IFieldSymbol field)
                {
                    members.Add(new InjectMember(field.Name, field.Type, metadata.Identifier, metadata.Optional));
                }
                else if (member is IPropertySymbol property)
                {
                    if (property.SetMethod == null)
                    {
                        reportDiagnostic(Diagnostic.Create(
                            PropertySetterRequired,
                            GetLocation(property),
                            property.ToDisplayString()));
                        continue;
                    }

                    members.Add(new InjectMember(property.Name, property.Type, metadata.Identifier, metadata.Optional));
                }
                else
                {
                    reportDiagnostic(Diagnostic.Create(
                        UnsupportedInjectMember,
                        GetLocation(member),
                        member.ToDisplayString()));
                }
            }

            return members;
        }

        private static Location GetLocation(ISymbol symbol)
        {
            return symbol.Locations.FirstOrDefault(location => location.IsInSource) ?? Location.None;
        }

        private static Location GetLocation(SyntaxToken token)
        {
            return token.GetLocation();
        }

        private static bool TryGetMetadata(ISymbol member, out InjectMetadata metadata)
        {
            foreach (var attribute in member.GetAttributes())
            {
                var attributeName = attribute.AttributeClass == null
                    ? string.Empty
                    : attribute.AttributeClass.ToDisplayString();
                if (attributeName != InjectAttributeName)
                {
                    continue;
                }

                object identifier = null;
                var optional = false;
                foreach (var argument in attribute.NamedArguments)
                {
                    if (argument.Key == "Id" || argument.Key == "Identifier")
                    {
                        identifier = argument.Value.Value;
                    }
                    else if (argument.Key == "Optional" && argument.Value.Value is bool value)
                    {
                        optional = value;
                    }
                }

                metadata = new InjectMetadata(identifier, optional);
                return true;
            }

            metadata = default(InjectMetadata);
            return false;
        }

        private static string BuildSource(IReadOnlyList<InjectTarget> targets)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated />");
            builder.AppendLine();
            builder.AppendLine("#pragma warning disable");
            builder.AppendLine();
            builder.AppendLine("using LFramework.Runtime;");
            builder.AppendLine();

            var currentNamespace = string.Empty;
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (!string.Equals(currentNamespace, target.Namespace, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(currentNamespace))
                    {
                        builder.AppendLine("}");
                        builder.AppendLine();
                    }

                    currentNamespace = target.Namespace;
                    if (!string.IsNullOrEmpty(currentNamespace))
                    {
                        builder.Append("namespace ").AppendLine(currentNamespace);
                        builder.AppendLine("{");
                    }
                }

                AppendTarget(builder, target);
            }

            if (!string.IsNullOrEmpty(currentNamespace))
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        private static void AppendTarget(StringBuilder builder, InjectTarget target)
        {
            builder.Append("    ").Append(target.DeclarationPrefix).Append(" partial class ")
                .Append(target.TypeDeclaration).AppendLine(" : IInjectable");
            if (!string.IsNullOrEmpty(target.TypeConstraints))
            {
                builder.Append("        ").AppendLine(target.TypeConstraints);
            }

            builder.AppendLine("    {");
            builder.AppendLine("        void IInjectable.Inject(IServiceResolver resolver)");
            builder.AppendLine("        {");
            builder.Append("            ").Append(InjectHelperName).AppendLine("(resolver);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.Append("        protected void ").Append(InjectHelperName).AppendLine("(IServiceResolver resolver)");
            builder.AppendLine("        {");

            if (target.InjectsBase)
            {
                builder.Append("            base.").Append(InjectHelperName).AppendLine("(resolver);");
            }

            for (var i = 0; i < target.Members.Count; i++)
            {
                AppendAssignment(builder, target.Members[i]);
            }

            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        private static void AppendAssignment(StringBuilder builder, InjectMember member)
        {
            var typeName = member.MemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (member.Optional)
            {
                builder.Append("            if (resolver.TryGet<").Append(typeName).Append(">(");
                if (member.Identifier == null)
                {
                    builder.Append("out var value");
                }
                else
                {
                    builder.Append(GetLiteral(member.Identifier)).Append(", out var value");
                }

                builder.AppendLine("))");
                builder.AppendLine("            {");
                builder.Append("                ").Append(member.MemberName).AppendLine(" = value;");
                builder.AppendLine("            }");
                return;
            }

            builder.Append("            ").Append(member.MemberName).Append(" = resolver.Get<").Append(typeName).Append(">(");
            if (member.Identifier != null)
            {
                builder.Append(GetLiteral(member.Identifier));
            }

            builder.AppendLine(");");
        }

        private static string GetLiteral(object value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is string text)
            {
                return "@\"" + text.Replace("\"", "\"\"") + "\"";
            }

            if (value is bool boolValue)
            {
                return boolValue ? "true" : "false";
            }

            return value.ToString();
        }

        private static bool HasInjectableBase(INamedTypeSymbol type)
        {
            while (type != null && type.SpecialType != SpecialType.System_Object)
            {
                if (IsSupportedInjectTarget(type) &&
                    (HasInjectHelper(type) || IsSourcePartialType(type) && HasOwnInjectableMembers(type)))
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static bool HasInjectHelper(INamedTypeSymbol type)
        {
            return type.GetMembers(InjectHelperName).Any(member => member is IMethodSymbol);
        }

        private static bool IsSourcePartialType(INamedTypeSymbol type)
        {
            foreach (var syntaxReference in type.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is TypeDeclarationSyntax declaration &&
                    declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSupportedInjectTarget(INamedTypeSymbol type)
        {
            return type.TypeKind == TypeKind.Class &&
                   type.ContainingType == null;
        }

        private static bool HasOwnInjectableMembers(INamedTypeSymbol type)
        {
            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic || !TryGetMetadata(member, out _))
                {
                    continue;
                }

                if (member is IFieldSymbol)
                {
                    return true;
                }

                if (member is IPropertySymbol property && property.SetMethod != null)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class Receiver : ISyntaxReceiver
        {
            public readonly List<TypeDeclarationSyntax> Candidates = new List<TypeDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (!(syntaxNode is TypeDeclarationSyntax typeDeclaration))
                {
                    return;
                }

                if (typeDeclaration.Members.Any(HasInjectAttribute))
                {
                    Candidates.Add(typeDeclaration);
                }
            }

            private static bool HasInjectAttribute(MemberDeclarationSyntax member)
            {
                foreach (var attributeList in member.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var name = attribute.Name.ToString();
                        if (name == "Inject" ||
                            name.EndsWith(".Inject", StringComparison.Ordinal) ||
                            name == "InjectAttribute" ||
                            name.EndsWith(".InjectAttribute", StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private readonly struct InjectMetadata
        {
            public InjectMetadata(object identifier, bool optional)
            {
                Identifier = identifier;
                Optional = optional;
            }

            public object Identifier { get; }

            public bool Optional { get; }
        }

        private sealed class InjectTarget
        {
            public InjectTarget(INamedTypeSymbol type, List<InjectMember> members, bool injectsBase)
            {
                FullName = type.ToDisplayString();
                Namespace = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
                TypeName = type.Name;
                TypeDeclaration = GetTypeDeclaration(type);
                TypeConstraints = GetTypeConstraints(type);
                Members = members;
                InjectsBase = injectsBase;
                DeclarationPrefix = type.DeclaredAccessibility == Accessibility.Public ? "public" : "internal";
                if (type.IsAbstract)
                {
                    DeclarationPrefix += " abstract";
                }
                else if (type.IsSealed)
                {
                    DeclarationPrefix += " sealed";
                }
            }

            public string FullName { get; }

            public string Namespace { get; }

            public string TypeName { get; }

            public string TypeDeclaration { get; }

            public string TypeConstraints { get; }

            public string DeclarationPrefix { get; }

            public List<InjectMember> Members { get; }

            public bool InjectsBase { get; }
        }

        private static string GetTypeDeclaration(INamedTypeSymbol type)
        {
            if (type.TypeParameters.Length == 0)
            {
                return type.Name;
            }

            return type.Name + "<" + string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name)) + ">";
        }

        private static string GetTypeConstraints(INamedTypeSymbol type)
        {
            if (type.TypeParameters.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            foreach (var parameter in type.TypeParameters)
            {
                var constraints = new List<string>();
                if (parameter.HasNotNullConstraint)
                {
                    constraints.Add("notnull");
                }

                if (parameter.HasReferenceTypeConstraint)
                {
                    constraints.Add(parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
                        ? "class?"
                        : "class");
                }
                else if (parameter.HasUnmanagedTypeConstraint)
                {
                    constraints.Add("unmanaged");
                }
                else if (parameter.HasValueTypeConstraint)
                {
                    constraints.Add("struct");
                }

                constraints.AddRange(parameter.ConstraintTypes.Select(typeSymbol =>
                    typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

                if (parameter.HasConstructorConstraint)
                {
                    constraints.Add("new()");
                }

                if (constraints.Count == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.Append("        ");
                }

                builder.Append("where ").Append(parameter.Name).Append(" : ")
                    .Append(string.Join(", ", constraints));
            }

            return builder.ToString();
        }

        private readonly struct InjectMember
        {
            public InjectMember(string memberName, ITypeSymbol memberType, object identifier, bool optional)
            {
                MemberName = memberName;
                MemberType = memberType;
                Identifier = identifier;
                Optional = optional;
            }

            public string MemberName { get; }

            public ITypeSymbol MemberType { get; }

            public object Identifier { get; }

            public bool Optional { get; }
        }
    }
}
