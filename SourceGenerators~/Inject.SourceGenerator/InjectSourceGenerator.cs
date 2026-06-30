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

            var targets = FindTargets(context.Compilation, receiver.Candidates);
            if (targets.Count == 0)
            {
                return;
            }

            context.AddSource("LFramework.Inject.Generated.g.cs", SourceText.From(BuildSource(targets), Encoding.UTF8));
        }

        private static List<InjectTarget> FindTargets(Compilation compilation, IEnumerable<TypeDeclarationSyntax> candidates)
        {
            var targets = new List<InjectTarget>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var candidate in candidates)
            {
                if (!(candidate is ClassDeclarationSyntax))
                {
                    continue;
                }

                var model = compilation.GetSemanticModel(candidate.SyntaxTree);
                if (!(model.GetDeclaredSymbol(candidate) is INamedTypeSymbol type))
                {
                    continue;
                }

                if (type.TypeKind != TypeKind.Class || type.IsGenericType || type.ContainingType != null)
                {
                    continue;
                }

                var members = FindInjectMembers(type);
                if (members.Count == 0)
                {
                    continue;
                }

                var key = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (seen.Add(key))
                {
                    targets.Add(new InjectTarget(type, members));
                }
            }

            targets.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return targets;
        }

        private static List<InjectMember> FindInjectMembers(INamedTypeSymbol type)
        {
            var members = new List<InjectMember>();
            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic)
                {
                    continue;
                }

                if (member is IFieldSymbol field)
                {
                    if (TryGetMetadata(field, out var metadata))
                    {
                        members.Add(new InjectMember(field.Name, field.Type, metadata.Identifier, metadata.Optional));
                    }
                }
                else if (member is IPropertySymbol property)
                {
                    if (property.SetMethod == null)
                    {
                        continue;
                    }

                    if (TryGetMetadata(property, out var metadata))
                    {
                        members.Add(new InjectMember(property.Name, property.Type, metadata.Identifier, metadata.Optional));
                    }
                }
            }

            return members;
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
                .Append(target.TypeName).AppendLine(" : IInjectable");
            builder.AppendLine("    {");
            builder.AppendLine("        void IInjectable.Inject(IServiceResolver resolver)");
            builder.AppendLine("        {");

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
            public InjectTarget(INamedTypeSymbol type, List<InjectMember> members)
            {
                FullName = type.ToDisplayString();
                Namespace = type.ContainingNamespace.IsGlobalNamespace ? string.Empty : type.ContainingNamespace.ToDisplayString();
                TypeName = type.Name;
                Members = members;
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

            public string DeclarationPrefix { get; }

            public List<InjectMember> Members { get; }
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
