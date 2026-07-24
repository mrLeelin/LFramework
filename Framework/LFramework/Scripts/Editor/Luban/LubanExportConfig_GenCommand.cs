using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using CliWrap;
using JetBrains.Annotations;
using Luban.Editor.PrimaryKey;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Luban.Editor
{
    public partial class LubanExportConfig
    {
        internal static readonly bool IS_WINDOWS = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [UsedImplicitly]
        internal static readonly bool IS_MACOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        internal static readonly string BAT_FILE = "gen.bat";
        internal static readonly Encoding CLI_OUTPUT_ENCODING = Encoding.UTF8;
        private static readonly UTF8Encoding Utf8WithBom = new(true);

        internal static readonly string LINE_END = IS_WINDOWS ? "^\n" : "\\\n";

        [TabGroup("Split", "预览命令", TabLayouting = TabLayouting.MultiRow)]
        [TabGroup("Split", "预览命令")]
        [BoxGroup("Split/预览命令/Command")]
        [TextArea(5, 50)]
        [HideLabel]
        public string command;

        [SerializeField]
        [HideInInspector]
        private string _command_args;

        [TabGroup("Split", "预览命令")]
        [BoxGroup("Split/预览命令/Command")]
        [Button("预览")]
        public void PreviewCommand()
        {
            var sb = new StringBuilder();
            string resolvedLubanDllPath = ResolveExternalToolPath(luban_dll);
            string resolvedLubanConfPath = ResolveExternalToolPath(luban_conf_path);

            sb.Append($"\"{resolvedLubanDllPath}\" {LINE_END}");
            sb.Append($"--conf \"{resolvedLubanConfPath}\" {LINE_END}");
            sb.Append($"-t {target} {LINE_END}");

            if(force_load_table_datas)
            {
                sb.Append($"-f {LINE_END}");
            }

            if(verbose)
            {
                sb.Append($"-v {LINE_END}");
            }

            if(validation_fail_as_error)
            {
                sb.Append($"--validationFailAsError {LINE_END}");
            }

            if(!string.IsNullOrEmpty(schema_collector))
            {
                sb.Append($"-s {schema_collector} {LINE_END}");
            }

            if(!string.IsNullOrEmpty(pipeline))
            {
                sb.Append($"-p {pipeline} {LINE_END}");
            }

            if(!string.IsNullOrEmpty(include_tag))
            {
                sb.Append($"-i {include_tag} {LINE_END}");
            }

            if(!string.IsNullOrEmpty(exclude_tag))
            {
                sb.Append($"-e {exclude_tag} {LINE_END}");
            }

            if(!string.IsNullOrEmpty(time_zone))
            {
                sb.Append($"--timeZone {time_zone} {LINE_END}");
            }

            var templateDir = !string.IsNullOrEmpty(custom_template_dir)
                ? custom_template_dir
                : DefaultTemplateDir;
            if(!string.IsNullOrEmpty(templateDir))
            {
                sb.Append($"--customTemplateDir {ResolveExternalToolPath(templateDir)} {LINE_END}");
            }

            if(output_table is not null && output_table.Count > 0)
            {
                foreach(var table in output_table)
                {
                    if(string.IsNullOrEmpty(table))
                    {
                        continue;
                    }

                    sb.Append($"-o {table} {LINE_END}");
                }
            }

            if(multi_code_target)
            {
                foreach(var target in code_targets)
                {
                    if(string.IsNullOrEmpty(target.code_type))
                    {
                        continue;
                    }

                    sb.Append($"-c {target.code_type} {LINE_END}");
                }
            }
            else if(!string.IsNullOrEmpty(code_target))
            {
                sb.Append($"-c {code_target} {LINE_END}");
            }

            if(multi_data_target)
            {
                foreach(var target in data_targets)
                {
                    if(string.IsNullOrEmpty(target.data_type))
                    {
                        continue;
                    }

                    sb.Append($"-d {target.data_type} {LINE_END}");
                }
            }
            else if(!string.IsNullOrEmpty(data_target))
            {
                sb.Append($"-d {data_target} {LINE_END}");
            }

            if(multi_code_target)
            {
                foreach(var target in code_targets)
                {
                    if(string.IsNullOrEmpty(target.output_dir))
                    {
                        continue;
                    }

                    sb.Append($"-x {target.code_type}.outputCodeDir={ResolveExternalToolPath(target.output_dir)} {LINE_END}");
                }
            }
            else if(!string.IsNullOrEmpty(output_code_dir))
            {
                sb.Append($"-x outputCodeDir={ResolveExternalToolPath(output_code_dir)} {LINE_END}");
            }

            if(multi_data_target)
            {
                foreach(var target in data_targets)
                {
                    if(string.IsNullOrEmpty(target.output_dir))
                    {
                        continue;
                    }

                    sb.Append($"-x {target.data_type}.outputDataDir={ResolveExternalToolPath(target.output_dir)} {LINE_END}");
                }
            }
            else if(!string.IsNullOrEmpty(output_data_dir))
            {
                sb.Append($"-x outputDataDir={ResolveExternalToolPath(output_data_dir)} {LINE_END}");
            }

            if(!string.IsNullOrEmpty(code_style))
            {
                sb.Append($"-x codeStyle={code_style} {LINE_END}");
            }

            if(!string.IsNullOrEmpty(data_exporter))
            {
                sb.Append($"-x dataExporter={data_exporter} {LINE_END}");
            }

            if(code_postprocess is not null && code_postprocess.Count > 0)
            {
                sb.Append($"-x codePostprocess={string.Join(",", code_postprocess)}");
            }

            if(data_postprocess is not null && data_postprocess.Count > 0)
            {
                sb.Append($"-x dataPostprocess={string.Join(",", data_postprocess)}");
            }

            if(!string.IsNullOrEmpty(output_saver))
            {
                sb.Append($"-x outputSaver={output_saver} {LINE_END}");
            }

            if(!string.IsNullOrEmpty(l10n_text_provider_name))
            {
                sb.Append($"-x l10n.textProviderName={l10n_text_provider_name} {LINE_END}");
            }

            if(!string.IsNullOrEmpty(l10n_text_provider_file))
            {
                sb.Append($"-x l10n.textProviderFile={ResolveExternalToolPath(l10n_text_provider_file)} {LINE_END}");
            }

            if(!string.IsNullOrEmpty(l10n_text_list_file))
            {
                sb.Append($"-x l10n.textListFile={l10n_text_list_file} {LINE_END}");
            }

            if(!string.IsNullOrEmpty(path_validator_root_dir))
            {
                sb.Append($"-x pathValidator.rootDir={ResolveExternalToolPath(path_validator_root_dir)} {LINE_END}");
            }

            if(custom_args is not null && custom_args.Count > 0)
            {
                foreach(var args in custom_args)
                {
                    if(string.IsNullOrEmpty(args.key))
                    {
                        continue;
                    }

                    sb.Append(
                        string.IsNullOrEmpty(args.value)
                            ? $"-x {args.key} {LINE_END}"
                            : $"-x {args.key}={args.value} {LINE_END}"
                    );
                }
            }

            _command_args = sb.ToString().TrimEnd(LINE_END.ToCharArray());

            if(IS_WINDOWS)
            {
                dotnet_path = "dotnet";
            }

            command = $"{dotnet_path} {_command_args}";
        }

        [TabGroup("Split", "预览命令")]
        [BoxGroup("Split/预览命令/Command")]
        [Button("执行")]
        public void RunCommand()
        {
            PreviewCommand();

            var full_path = ResolveExternalToolPath(luban_conf_path);

            var dir = Path.GetDirectoryName(full_path);

            if(!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if(File.Exists(full_path))
            {
                File.Delete(full_path);
            }

            LubanConfig exportConfig = BuildExportConfig();
            exportConfig.PrepareJson();

            var json = JsonConvert.SerializeObject(exportConfig, Formatting.Indented);

            File.WriteAllText(full_path, json, Encoding.UTF8);

            string exe;
            string args;

            if(IS_WINDOWS)
            {
                exe  = "cmd";
                args = $"/c \"{BAT_FILE}\"";

                File.WriteAllBytes($"./{BAT_FILE}", BuildWindowsBatchFileBytes(command));
            }
            else
            {
                exe  = dotnet_path;
                args = _command_args;
            }

            var cli = Cli.Wrap(exe)
                .WithArguments(args)
                .WithWorkingDirectory(".")
                .WithStandardOutputPipe(PipeTarget.ToDelegate(LogCommandOutput, CLI_OUTPUT_ENCODING))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(Debug.LogError, CLI_OUTPUT_ENCODING));

            try
            {
                cli.ExecuteAsync().GetAwaiter().GetResult();
                LubanPrimaryKeyClassGenerator.GenerateAll(this, LubanPrimaryKeyGenerateConfigRegister.GetOrCreate());
            }
            catch(Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("错误", "请检查日志", "确定");
            }
            finally
            {
                if(File.Exists(BAT_FILE))
                {
                    File.Delete(BAT_FILE);
                }
            }
        }

        internal static byte[] BuildWindowsBatchFileBytes(string commandText)
        {
            string normalizedCommand = string.IsNullOrWhiteSpace(commandText) ? string.Empty : commandText.Trim();
            string batchContent =
                $"@echo off{Environment.NewLine}" +
                $"chcp 65001 >nul{Environment.NewLine}" +
                $"{normalizedCommand}{Environment.NewLine}";
            return Utf8WithBom.GetBytes(batchContent);
        }

        public LubanConfig BuildExportConfig()
        {
            List<LubanConfig> configs = EnumerateLubanConfigs()
                .Where(static item => item != null)
                .ToList();

            if (configs.Count == 0)
            {
                return new LubanConfig();
            }

            if (configs.Count == 1)
            {
                return configs[0].Clone();
            }

            string confAbsolutePath = Path.GetFullPath(luban_conf_path);
            string confDirectory = Path.GetDirectoryName(confAbsolutePath)
                                   ?? throw new InvalidOperationException("Cannot resolve Luban conf directory.");

            var mergedConfig = new LubanConfig
            {
                data_dir = ".",
                groups = MergeGroups(configs),
                schema_files = MergeSchemaFiles(configs, confDirectory),
                targets = MergeTargets(configs)
            };

            return mergedConfig;
        }

        private static List<RawGroup> MergeGroups(IEnumerable<LubanConfig> configs)
        {
            var merged = new Dictionary<string, RawGroup>(StringComparer.Ordinal);

            foreach (LubanConfig config in configs)
            {
                if (config.groups == null)
                {
                    continue;
                }

                foreach (RawGroup group in config.groups)
                {
                    if (group == null || string.IsNullOrWhiteSpace(group.names))
                    {
                        continue;
                    }

                    string key = string.Join(",", RawGroup.SplitCsv(group.names));
                    if (merged.TryGetValue(key, out RawGroup existing))
                    {
                        existing.is_default |= group.is_default;
                        continue;
                    }

                    merged.Add(key, group.Clone());
                }
            }

            return merged.Values.ToList();
        }

        private static List<SchemaFileInfo> MergeSchemaFiles(IEnumerable<LubanConfig> configs, string confDirectory)
        {
            var schemaFiles = new List<SchemaFileInfo>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (LubanConfig config in configs)
            {
                if (config.schema_files == null)
                {
                    continue;
                }

                foreach (SchemaFileInfo schemaFile in config.schema_files)
                {
                    if (schemaFile == null || string.IsNullOrWhiteSpace(schemaFile.file_name))
                    {
                        continue;
                    }

                    string resolvedPath = ResolveSchemaPath(confDirectory, config.data_dir, schemaFile.file_name);
                    if (!seenPaths.Add(resolvedPath))
                    {
                        continue;
                    }

                    SchemaFileInfo clonedSchemaFile = schemaFile.Clone();
                    clonedSchemaFile.file_name = Path.GetRelativePath(confDirectory, resolvedPath).Replace("\\", "/");
                    schemaFiles.Add(clonedSchemaFile);
                }
            }

            return schemaFiles;
        }

        private static string ResolveSchemaPath(string confDirectory, string dataDir, string schemaFileName)
        {
            if (Path.IsPathRooted(schemaFileName))
            {
                return Path.GetFullPath(schemaFileName).Replace("\\", "/");
            }

            string root = string.IsNullOrWhiteSpace(dataDir)
                ? confDirectory
                : ResolveAgainstDirectory(confDirectory, dataDir);

            return Path.GetFullPath(Path.Combine(root, schemaFileName)).Replace("\\", "/");
        }

        private static string ResolveAgainstDirectory(string baseDirectory, string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(baseDirectory, path));
        }

        private static List<RawTarget> MergeTargets(IEnumerable<LubanConfig> configs)
        {
            var merged = new Dictionary<string, RawTarget>(StringComparer.Ordinal);

            foreach (LubanConfig config in configs)
            {
                if (config.targets == null)
                {
                    continue;
                }

                foreach (RawTarget target in config.targets)
                {
                    if (target == null || string.IsNullOrWhiteSpace(target.name))
                    {
                        continue;
                    }

                    if (!merged.TryGetValue(target.name, out RawTarget existing))
                    {
                        RawTarget clonedTarget = target.Clone();
                        clonedTarget.manager = NormalizeTargetManager(clonedTarget.manager);
                        clonedTarget.top_module = NormalizeTargetTopModule(clonedTarget.top_module);
                        merged.Add(target.name, clonedTarget);
                        continue;
                    }

                    if (!string.Equals(NormalizeTargetManager(existing.manager), NormalizeTargetManager(target.manager), StringComparison.Ordinal)
                        || !string.Equals(NormalizeTargetTopModule(existing.top_module), NormalizeTargetTopModule(target.top_module), StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Luban target '{target.name}' has conflicting manager or topModule settings.");
                    }

                    existing.manager = NormalizeTargetManager(existing.manager);
                    existing.top_module = NormalizeTargetTopModule(existing.top_module);
                    existing.groups = MergeCsv(existing.groups, target.groups);
                }
            }

            return merged.Values.ToList();
        }

        private static string NormalizeTargetManager(string manager)
        {
            return string.IsNullOrWhiteSpace(manager) ? "Tables" : manager;
        }

        private static string NormalizeTargetTopModule(string topModule)
        {
            return string.IsNullOrWhiteSpace(topModule) ? string.Empty : topModule;
        }

        private static string MergeCsv(string first, string second)
        {
            var values = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (string item in RawGroup.SplitCsv(first).Concat(RawGroup.SplitCsv(second)))
            {
                if (seen.Add(item))
                {
                    values.Add(item);
                }
            }

            return string.Join(",", values);
        }

        internal static bool ShouldLogCommandOutputAsError(string message)
        {
            return !string.IsNullOrEmpty(message)
                && message.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void LogCommandOutput(string message)
        {
            if(ShouldLogCommandOutputAsError(message))
            {
                Debug.LogError(message);
                return;
            }

            Debug.Log(message);
        }
    }
}
