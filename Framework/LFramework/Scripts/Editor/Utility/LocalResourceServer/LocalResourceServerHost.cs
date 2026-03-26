using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LFramework.Editor
{
    internal sealed class LocalResourceServerHost : ILocalResourceServerHost
    {
        internal readonly struct DirectoryListingEntry
        {
            public DirectoryListingEntry(
                string name,
                string href,
                string type,
                string size,
                string modified,
                bool isDirectory)
            {
                Name = name ?? string.Empty;
                Href = href ?? string.Empty;
                Type = type ?? string.Empty;
                Size = size ?? string.Empty;
                Modified = modified ?? string.Empty;
                IsDirectory = isDirectory;
            }

            public string Name { get; }
            public string Href { get; }
            public string Type { get; }
            public string Size { get; }
            public string Modified { get; }
            public bool IsDirectory { get; }
        }

        private static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".bytes"] = "application/octet-stream",
            [".bundle"] = "application/octet-stream",
            [".hash"] = "text/plain",
            [".json"] = "application/json",
            [".txt"] = "text/plain",
            [".version"] = "text/plain",
            [".xml"] = "application/xml",
            [".html"] = "text/html",
            [".htm"] = "text/html",
            [".bin"] = "application/octet-stream"
        };

        private HttpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listenTask;
        private string _rootDirectory;

        public bool IsRunning => _listener != null && _listener.IsListening;

        public bool TryStart(int port, string rootDirectory, out string errorMessage)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                errorMessage = "Local resource server is only supported on Windows Editor.";
                return false;
            }

            if (!HttpListener.IsSupported)
            {
                errorMessage = "HttpListener is not supported on this machine.";
                return false;
            }

            if (IsRunning)
            {
                errorMessage = string.Empty;
                return true;
            }

            try
            {
                _rootDirectory = Path.GetFullPath(rootDirectory);
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                _listener.Start();

                _cancellationTokenSource = new CancellationTokenSource();
                _listenTask = Task.Run(() => ListenLoop(_cancellationTokenSource.Token));
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                Stop();
                return false;
            }
        }

        public void Stop()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _listener?.Stop();
                _listener?.Close();
                _listenTask?.Wait(250);
            }
            catch
            {
                // Swallow shutdown exceptions to keep the editor tool stable.
            }
            finally
            {
                _listenTask = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _listener = null;
            }
        }

        private async Task ListenLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener != null)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    if (cancellationToken.IsCancellationRequested || _listener == null)
                    {
                        break;
                    }

                    continue;
                }

                _ = Task.Run(() => HandleRequest(context), cancellationToken);
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                if (string.IsNullOrEmpty(_rootDirectory))
                {
                    WriteTextResponse(context.Response, HttpStatusCode.ServiceUnavailable, "Server root is unavailable.");
                    return;
                }

                string relativePath = Uri.UnescapeDataString(context.Request.Url.AbsolutePath.TrimStart('/'));
                string requestedPath = string.IsNullOrEmpty(relativePath)
                    ? string.Empty
                    : relativePath.Replace('/', Path.DirectorySeparatorChar);
                string fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, requestedPath));
                string normalizedRoot = EnsureTrailingSeparator(_rootDirectory);
                if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(fullPath, _rootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    WriteTextResponse(context.Response, HttpStatusCode.Forbidden, "Forbidden.");
                    return;
                }

                if (Directory.Exists(fullPath))
                {
                    string indexPath = Path.Combine(fullPath, "index.html");
                    if (File.Exists(indexPath))
                    {
                        fullPath = indexPath;
                    }
                    else
                    {
                        string html = CreateDirectoryListingHtml(
                            context.Request.Url.AbsolutePath,
                            CreateDirectoryListingEntries(context.Request.Url.AbsolutePath, fullPath));
                        WriteHtmlResponse(context.Response, HttpStatusCode.OK, html);
                        return;
                    }
                }

                if (!File.Exists(fullPath))
                {
                    WriteTextResponse(context.Response, HttpStatusCode.NotFound, "File not found.");
                    return;
                }

                byte[] fileBytes = File.ReadAllBytes(fullPath);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = GetMimeType(fullPath);
                context.Response.ContentLength64 = fileBytes.Length;
                context.Response.OutputStream.Write(fileBytes, 0, fileBytes.Length);
            }
            catch (Exception exception)
            {
                WriteTextResponse(context.Response, HttpStatusCode.InternalServerError, exception.Message);
            }
            finally
            {
                context.Response.OutputStream.Close();
            }
        }

        internal static string CreateDirectoryListingHtml(
            string directoryRequestPath,
            IEnumerable<DirectoryListingEntry> entries)
        {
            string normalizedPath = NormalizeDirectoryRequestPath(directoryRequestPath);
            string pageTitle = "ServerData Browser";
            string parentPath = GetParentDirectoryPath(normalizedPath);
            var entryList = entries?.ToList() ?? new List<DirectoryListingEntry>();
            var builder = new StringBuilder();
            builder.AppendLine("<!DOCTYPE html>");
            builder.AppendLine("<html lang=\"en\">");
            builder.AppendLine("<head>");
            builder.AppendLine("  <meta charset=\"utf-8\">");
            builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            builder.AppendLine($"  <title>{pageTitle} · {WebUtility.HtmlEncode(normalizedPath)}</title>");
            builder.AppendLine("  <style>");
            builder.AppendLine("    :root { color-scheme: light; --bg: #f4f7fb; --surface: #ffffff; --surface-alt: #f8fafc; --border: #d9e2ec; --text: #142033; --muted: #607086; --accent: #1f6feb; --accent-soft: #e9f2ff; --folder: #0f766e; --shadow: 0 18px 40px rgba(15, 23, 42, 0.08); }");
            builder.AppendLine("    * { box-sizing: border-box; }");
            builder.AppendLine("    body { margin: 0; font-family: \"Segoe UI Variable\", \"Segoe UI\", sans-serif; background: linear-gradient(180deg, #eef4ff 0%, var(--bg) 100%); color: var(--text); }");
            builder.AppendLine("    a { color: inherit; text-decoration: none; }");
            builder.AppendLine("    .shell { max-width: 1080px; margin: 0 auto; padding: 32px 20px 48px; }");
            builder.AppendLine("    .hero { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; margin-bottom: 18px; }");
            builder.AppendLine("    .eyebrow { margin: 0 0 10px; font-size: 12px; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; color: var(--muted); }");
            builder.AppendLine("    h1 { margin: 0; font-size: clamp(28px, 4vw, 40px); line-height: 1.05; }");
            builder.AppendLine("    .subtle { margin: 10px 0 0; color: var(--muted); font-size: 14px; }");
            builder.AppendLine("    .action { display: inline-flex; align-items: center; justify-content: center; padding: 11px 14px; border-radius: 12px; border: 1px solid var(--border); background: var(--surface); box-shadow: var(--shadow); color: var(--accent); font-weight: 600; white-space: nowrap; }");
            builder.AppendLine("    .card { background: rgba(255,255,255,0.88); border: 1px solid rgba(217, 226, 236, 0.9); border-radius: 20px; box-shadow: var(--shadow); overflow: hidden; backdrop-filter: blur(6px); }");
            builder.AppendLine("    .pathbar { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 18px 20px; border-bottom: 1px solid var(--border); background: linear-gradient(180deg, #ffffff 0%, var(--surface-alt) 100%); }");
            builder.AppendLine("    .path-label { margin: 0 0 6px; font-size: 12px; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; color: var(--muted); }");
            builder.AppendLine("    .path-value { margin: 0; font-size: 18px; font-weight: 650; word-break: break-all; }");
            builder.AppendLine("    .table-wrap { overflow-x: auto; }");
            builder.AppendLine("    table { width: 100%; border-collapse: collapse; }");
            builder.AppendLine("    thead th { padding: 14px 20px; text-align: left; font-size: 12px; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; color: var(--muted); background: #f8fbff; border-bottom: 1px solid var(--border); }");
            builder.AppendLine("    tbody td { padding: 16px 20px; border-bottom: 1px solid var(--border); font-size: 14px; vertical-align: middle; }");
            builder.AppendLine("    tbody tr:last-child td { border-bottom: 0; }");
            builder.AppendLine("    tbody tr:hover { background: #f9fbff; }");
            builder.AppendLine("    .name-link { display: inline-flex; align-items: center; gap: 10px; min-width: 0; font-weight: 600; color: var(--text); }");
            builder.AppendLine("    .icon { width: 32px; height: 32px; border-radius: 10px; display: inline-flex; align-items: center; justify-content: center; font-size: 15px; font-weight: 700; flex: 0 0 auto; }");
            builder.AppendLine("    .icon-folder { background: rgba(15, 118, 110, 0.12); color: var(--folder); }");
            builder.AppendLine("    .icon-file { background: var(--accent-soft); color: var(--accent); }");
            builder.AppendLine("    .meta { color: var(--muted); white-space: nowrap; }");
            builder.AppendLine("    .empty { padding: 36px 20px 40px; text-align: center; color: var(--muted); }");
            builder.AppendLine("    .empty strong { display: block; margin-bottom: 6px; color: var(--text); font-size: 16px; }");
            builder.AppendLine("    @media (max-width: 720px) { .shell { padding: 20px 14px 28px; } .hero, .pathbar { flex-direction: column; align-items: flex-start; } thead th:nth-child(3), thead th:nth-child(4), tbody td:nth-child(3), tbody td:nth-child(4) { white-space: nowrap; } }");
            builder.AppendLine("  </style>");
            builder.AppendLine("</head>");
            builder.AppendLine("<body>");
            builder.AppendLine("  <main class=\"shell\">");
            builder.AppendLine("    <section class=\"hero\">");
            builder.AppendLine("      <div>");
            builder.AppendLine("        <p class=\"eyebrow\">Local Resource Server</p>");
            builder.AppendLine($"        <h1>{pageTitle}</h1>");
            builder.AppendLine("        <p class=\"subtle\">Browse files under the project root ServerData folder.</p>");
            builder.AppendLine("      </div>");
            if (!string.Equals(normalizedPath, "/", StringComparison.Ordinal))
            {
                builder.AppendLine($"      <a class=\"action\" href=\"{WebUtility.HtmlEncode(parentPath)}\">Up One Level</a>");
            }
            builder.AppendLine("    </section>");
            builder.AppendLine("    <section class=\"card\">");
            builder.AppendLine("      <div class=\"pathbar\">");
            builder.AppendLine("        <div>");
            builder.AppendLine("          <p class=\"path-label\">Current Path</p>");
            builder.AppendLine($"          <p class=\"path-value\">{WebUtility.HtmlEncode(normalizedPath)}</p>");
            builder.AppendLine("        </div>");
            if (!string.Equals(normalizedPath, "/", StringComparison.Ordinal))
            {
                builder.AppendLine($"        <a class=\"action\" href=\"{WebUtility.HtmlEncode(parentPath)}\">Back</a>");
            }
            builder.AppendLine("      </div>");
            if (entryList.Count == 0)
            {
                builder.AppendLine("      <div class=\"empty\">");
                builder.AppendLine("        <strong>This folder is empty.</strong>");
                builder.AppendLine("        Add files under ServerData and refresh the page.");
                builder.AppendLine("      </div>");
            }
            else
            {
                builder.AppendLine("      <div class=\"table-wrap\">");
                builder.AppendLine("        <table>");
                builder.AppendLine("          <thead>");
                builder.AppendLine("            <tr>");
                builder.AppendLine("              <th>Name</th>");
                builder.AppendLine("              <th>Type</th>");
                builder.AppendLine("              <th>Size</th>");
                builder.AppendLine("              <th>Modified</th>");
                builder.AppendLine("            </tr>");
                builder.AppendLine("          </thead>");
                builder.AppendLine("          <tbody>");
                foreach (DirectoryListingEntry entry in entryList)
                {
                    string iconClass = entry.IsDirectory ? "icon icon-folder" : "icon icon-file";
                    string iconText = entry.IsDirectory ? "DIR" : "FILE";
                    builder.AppendLine("            <tr>");
                    builder.AppendLine(
                        $"              <td><a class=\"name-link\" href=\"{WebUtility.HtmlEncode(entry.Href)}\"><span class=\"{iconClass}\">{iconText}</span><span>{WebUtility.HtmlEncode(entry.Name)}</span></a></td>");
                    builder.AppendLine($"              <td class=\"meta\">{WebUtility.HtmlEncode(entry.Type)}</td>");
                    builder.AppendLine($"              <td class=\"meta\">{WebUtility.HtmlEncode(entry.Size)}</td>");
                    builder.AppendLine($"              <td class=\"meta\">{WebUtility.HtmlEncode(entry.Modified)}</td>");
                    builder.AppendLine("            </tr>");
                }
                builder.AppendLine("          </tbody>");
                builder.AppendLine("        </table>");
                builder.AppendLine("      </div>");
            }
            builder.AppendLine("    </section>");
            builder.AppendLine("  </main>");
            builder.AppendLine("</body>");
            builder.AppendLine("</html>");
            return builder.ToString();
        }

        private static List<DirectoryListingEntry> CreateDirectoryListingEntries(string directoryRequestPath, string directoryPath)
        {
            string normalizedPath = NormalizeDirectoryRequestPath(directoryRequestPath);
            var entries = new List<DirectoryListingEntry>();

            foreach (DirectoryInfo directory in new DirectoryInfo(directoryPath)
                         .EnumerateDirectories()
                         .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase))
            {
                entries.Add(new DirectoryListingEntry(
                    directory.Name,
                    CombineUrlPath(normalizedPath, directory.Name, isDirectory: true),
                    "Folder",
                    "--",
                    FormatModifiedTime(directory.LastWriteTime),
                    true));
            }

            foreach (FileInfo file in new DirectoryInfo(directoryPath)
                         .EnumerateFiles()
                         .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase))
            {
                entries.Add(new DirectoryListingEntry(
                    file.Name,
                    CombineUrlPath(normalizedPath, file.Name, isDirectory: false),
                    GetFileTypeLabel(file.Extension),
                    FormatFileSize(file.Length),
                    FormatModifiedTime(file.LastWriteTime),
                    false));
            }

            return entries;
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static string GetMimeType(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            return MimeTypes.TryGetValue(extension, out string mimeType)
                ? mimeType
                : "application/octet-stream";
        }

        private static string GetFileTypeLabel(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return "File";
            }

            string normalized = extension.TrimStart('.');
            return string.IsNullOrEmpty(normalized)
                ? "File"
                : normalized.ToUpperInvariant();
        }

        private static string FormatFileSize(long length)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double size = length;
            int unitIndex = 0;
            while (size >= 1024d && unitIndex < units.Length - 1)
            {
                size /= 1024d;
                unitIndex++;
            }

            string format = unitIndex == 0 ? "0" : "0.#";
            return $"{size.ToString(format)} {units[unitIndex]}";
        }

        private static string FormatModifiedTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm");
        }

        private static string NormalizeDirectoryRequestPath(string directoryRequestPath)
        {
            if (string.IsNullOrWhiteSpace(directoryRequestPath) || string.Equals(directoryRequestPath, "/", StringComparison.Ordinal))
            {
                return "/";
            }

            string normalized = directoryRequestPath.Replace('\\', '/');
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = "/" + normalized;
            }

            return normalized.EndsWith("/", StringComparison.Ordinal)
                ? normalized
                : normalized + "/";
        }

        private static string GetParentDirectoryPath(string normalizedDirectoryPath)
        {
            if (string.Equals(normalizedDirectoryPath, "/", StringComparison.Ordinal))
            {
                return "/";
            }

            string trimmed = normalizedDirectoryPath.TrimEnd('/');
            int lastSeparatorIndex = trimmed.LastIndexOf('/');
            if (lastSeparatorIndex <= 0)
            {
                return "/";
            }

            return trimmed.Substring(0, lastSeparatorIndex + 1);
        }

        private static string CombineUrlPath(string normalizedDirectoryPath, string entryName, bool isDirectory)
        {
            string encodedName = Uri.EscapeDataString(entryName ?? string.Empty);
            string basePath = string.Equals(normalizedDirectoryPath, "/", StringComparison.Ordinal)
                ? "/"
                : normalizedDirectoryPath;
            string combined = basePath + encodedName;
            return isDirectory ? combined + "/" : combined;
        }

        private static void WriteHtmlResponse(HttpListenerResponse response, HttpStatusCode statusCode, string html)
        {
            byte[] content = Encoding.UTF8.GetBytes(html ?? string.Empty);
            response.StatusCode = (int)statusCode;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = content.Length;
            response.OutputStream.Write(content, 0, content.Length);
        }

        private static void WriteTextResponse(HttpListenerResponse response, HttpStatusCode statusCode, string message)
        {
            byte[] content = Encoding.UTF8.GetBytes(message ?? string.Empty);
            response.StatusCode = (int)statusCode;
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = content.Length;
            response.OutputStream.Write(content, 0, content.Length);
        }
    }
}
