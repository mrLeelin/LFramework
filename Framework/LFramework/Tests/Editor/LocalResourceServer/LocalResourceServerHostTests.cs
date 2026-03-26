using NUnit.Framework;

namespace LFramework.Editor.Tests.LocalResourceServer
{
    public class LocalResourceServerHostTests
    {
        [Test]
        public void CreateDirectoryListingHtml_IncludesDirectoriesAndFilesAsLinks()
        {
            string html = LocalResourceServerHost.CreateDirectoryListingHtml(
                directoryRequestPath: "/",
                directories: new[] { "Bundles" },
                files: new[] { "catalog.json" });

            Assert.That(html, Does.Contain("Index of /"));
            Assert.That(html, Does.Contain("href=\"/Bundles/\""));
            Assert.That(html, Does.Contain("href=\"/catalog.json\""));
            Assert.That(html, Does.Contain(">Bundles/</a>"));
            Assert.That(html, Does.Contain(">catalog.json</a>"));
        }

        [Test]
        public void CreateDirectoryListingHtml_EscapesDisplayedNames()
        {
            string html = LocalResourceServerHost.CreateDirectoryListingHtml(
                directoryRequestPath: "/nested/",
                directories: new[] { "<Dir>" },
                files: new[] { "data&manifest.json" });

            Assert.That(html, Does.Contain("Index of /nested/"));
            Assert.That(html, Does.Contain("&lt;Dir&gt;/"));
            Assert.That(html, Does.Contain("data&amp;manifest.json"));
        }
    }
}
