using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LFramework.Editor
{
    internal sealed class LocalResourceServerHost : ILocalResourceServerHost
    {
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
                            Directory.EnumerateDirectories(fullPath),
                            Directory.EnumerateFiles(fullPath));
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
            IEnumerable<string> directories,
            IEnumerable<string> files)
        {
            string normalizedPath = NormalizeDirectoryRequestPath(directoryRequestPath);
            var builder = new StringBuilder();
            builder.AppendLine("<!DOCTYPE html>");
            builder.AppendLine("<html>");
            builder.AppendLine("<head>");
            builder.AppendLine("  <meta charset=\"utf-8\">");
            builder.AppendLine($"  <title>Index of {WebUtility.HtmlEncode(normalizedPath)}</title>");
            builder.AppendLine("</head>");
            builder.AppendLine("<body>");
            builder.AppendLine($"  <h1>Index of {WebUtility.HtmlEncode(normalizedPath)}</h1>");
            builder.AppendLine("  <ul>");

            if (!string.Equals(normalizedPath, "/", StringComparison.Ordinal))
            {
                string parentPath = GetParentDirectoryPath(normalizedPath);
                builder.AppendLine($"    <li><a href=\"{WebUtility.HtmlEncode(parentPath)}\">../</a></li>");
            }

            foreach (string directory in directories)
            {
                string directoryName = Path.GetFileName(directory);
                string href = CombineUrlPath(normalizedPath, directoryName, isDirectory: true);
                builder.AppendLine(
                    $"    <li><a href=\"{WebUtility.HtmlEncode(href)}\">{WebUtility.HtmlEncode(directoryName)}/</a></li>");
            }

            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                string href = CombineUrlPath(normalizedPath, fileName, isDirectory: false);
                builder.AppendLine(
                    $"    <li><a href=\"{WebUtility.HtmlEncode(href)}\">{WebUtility.HtmlEncode(fileName)}</a></li>");
            }

            builder.AppendLine("  </ul>");
            builder.AppendLine("</body>");
            builder.AppendLine("</html>");
            return builder.ToString();
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
