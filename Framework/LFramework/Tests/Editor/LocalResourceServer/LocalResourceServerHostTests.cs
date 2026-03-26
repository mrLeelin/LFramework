using NUnit.Framework;

namespace LFramework.Editor.Tests.LocalResourceServer
{
    public class LocalResourceServerHostTests
    {
        [Test]
        public void CreateDirectoryListingHtml_RendersFilePanelLayoutForRoot()
        {
            var entries = new[]
            {
                new LocalResourceServerHost.DirectoryListingEntry("Bundles", "/Bundles/", "Folder", "--", "2026-03-26 20:15", true),
                new LocalResourceServerHost.DirectoryListingEntry("catalog.json", "/catalog.json", "JSON", "12 KB", "2026-03-26 20:16", false)
            };

            string html = LocalResourceServerHost.CreateDirectoryListingHtml(
                directoryRequestPath: "/",
                entries: entries);

            Assert.That(html, Does.Contain("ServerData Browser"));
            Assert.That(html, Does.Contain("Current Path"));
            Assert.That(html, Does.Contain("<th>Name</th>"));
            Assert.That(html, Does.Contain("<th>Type</th>"));
            Assert.That(html, Does.Contain("<th>Size</th>"));
            Assert.That(html, Does.Contain("<th>Modified</th>"));
            Assert.That(html, Does.Contain("href=\"/Bundles/\""));
            Assert.That(html, Does.Contain("href=\"/catalog.json\""));
            Assert.That(html, Does.Contain(">Bundles</a>"));
            Assert.That(html, Does.Contain(">catalog.json</a>"));
            Assert.That(html, Does.Contain(">Folder<"));
            Assert.That(html, Does.Contain(">JSON<"));
            Assert.That(html, Does.Contain(">12 KB<"));
            Assert.That(html, Does.Not.Contain("Up One Level"));
        }

        [Test]
        public void CreateDirectoryListingHtml_RendersParentLinkAndEscapesEntryNames()
        {
            var entries = new[]
            {
                new LocalResourceServerHost.DirectoryListingEntry("<Dir>", "/nested/child/%3CDir%3E/", "Folder", "--", "2026-03-26 20:15", true),
                new LocalResourceServerHost.DirectoryListingEntry("data&manifest.json", "/nested/child/data%26manifest.json", "JSON", "1 KB", "2026-03-26 20:16", false)
            };

            string html = LocalResourceServerHost.CreateDirectoryListingHtml(
                directoryRequestPath: "/nested/child/",
                entries: entries);

            Assert.That(html, Does.Contain("/nested/child/"));
            Assert.That(html, Does.Contain("href=\"/nested/\""));
            Assert.That(html, Does.Contain("Up One Level"));
            Assert.That(html, Does.Contain("&lt;Dir&gt;"));
            Assert.That(html, Does.Contain("data&amp;manifest.json"));
        }

        [Test]
        public void CreateDirectoryListingHtml_ShowsEmptyStateWhenDirectoryHasNoEntries()
        {
            string html = LocalResourceServerHost.CreateDirectoryListingHtml(
                directoryRequestPath: "/empty/",
                entries: System.Array.Empty<LocalResourceServerHost.DirectoryListingEntry>());

            Assert.That(html, Does.Contain("This folder is empty."));
        }
    }
}
