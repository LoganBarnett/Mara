using System;
using System.IO;
using NUnit.Framework;
using Mara;

namespace Mara.DriverSpecs {

    /*
     * This spec covers some of the very basics:
     *
     *   Visit()
     *   Page.Body
     *   CurrentUrl
     *   CurrentPath
     *   SaveAndOpenPage()
     */
    [TestFixture]
    public class VisitAndBasicsSpec : MaraTest {

        [Test]
        public void CanVisitRelativePath() {
            Visit("/");
            Assert.That(Page.Body, Is.StringContaining("Mara test application"));
            Assert.That(Page.Body, Is.Not.StringContaining("About this site"));
            
            Visit("/About.aspx");
            Assert.That(Page.Body, Is.StringContaining("About this site"));
            Assert.That(Page.Body, Is.Not.StringContaining("Mara test application"));
        }

        [Test][Ignore]
        public void CanVisitAbsolutePath() { }

        [Test]
        public void CanGetCurrentUrl() {
            Visit("/");
            Assert.That(CurrentUrl, Is.EqualTo(Mara.AppHost + "/"));

            Visit("/About.aspx");
            Assert.That(CurrentUrl, Is.EqualTo(Mara.AppHost + "/About.aspx")); // FIXME Don't use a Global Mara.
        }

        [Test]
        public void CanGetCurrentPath() {
            Visit("/");
            Assert.That(CurrentPath, Is.EqualTo("/"));

            Visit("/About.aspx");
            Assert.That(CurrentPath, Is.EqualTo("/About.aspx"));
        }

        // This should go in a group of specs that are ONLY for Drivers that support JavaScript
        // We could use NUnit categories to specify this?
        [Test][Ignore]
        public void CanGetCurrentPathAfterBeingRedirectedViaJavaScript() { }

        [Test]
        public void CanSaveAndOpenPage() {
            Visit("/");
            Assert.True(Page.Body.Contains("If you see this text, you're running our test suite!"));

            // SaveAndOpenPage() returns the full path to the saved .html file
            var html = "";
            using (var reader = new StreamReader(SaveAndOpenPage()))
                html = reader.ReadToEnd();
            Assert.True(html.Contains("If you see this text, you're running our test suite!"));
        }
    }
}
