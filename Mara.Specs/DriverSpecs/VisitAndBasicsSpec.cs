using System;
using System.IO;
using System.Net;
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

        [Test]
        public void CanVisitAbsolutePath() {
			var port = new Uri(Mara.AppHost).Port;
			Visit(string.Format("http://localhost:{0}/Stuff.aspx", port));
			Assert.That(Page.Body, Is.StringContaining("HostName: localhost"));
			Assert.That(Page.Body, Is.Not.StringContaining("HostName: 127.0.0.1"));
			Assert.That(CurrentUrl, Is.EqualTo(string.Format("http://localhost:{0}/Stuff.aspx", port)));
			Assert.That(CurrentPath, Is.EqualTo("/Stuff.aspx"));

			Visit(string.Format("http://127.0.0.1:{0}/Stuff.aspx", port));
			Assert.That(Page.Body, Is.Not.StringContaining("HostName: localhost"));
			Assert.That(Page.Body, Is.StringContaining("HostName: 127.0.0.1"));
			Assert.That(CurrentUrl, Is.EqualTo(string.Format("http://127.0.0.1:{0}/Stuff.aspx", port)));
			Assert.That(CurrentPath, Is.EqualTo("/Stuff.aspx"));
		}

        [Test]
        public void ChangingMaraAppHostChangesTheHostOfRequests() {
			var port = new Uri(Mara.AppHost).Port;
			Mara.AppHost = "http://localhost:" + port.ToString();
			Visit("/Stuff.aspx");
			Assert.That(Page.Body, Is.StringContaining("HostName: localhost"));
			Assert.That(Page.Body, Is.Not.StringContaining("HostName: 127.0.0.1"));
			Assert.That(CurrentUrl, Is.EqualTo(string.Format("http://localhost:{0}/Stuff.aspx", port)));
			Assert.That(CurrentPath, Is.EqualTo("/Stuff.aspx"));

			Mara.AppHost = "http://127.0.0.1:" + port.ToString();
			Visit("/Stuff.aspx");
			Assert.That(Page.Body, Is.Not.StringContaining("HostName: localhost"));
			Assert.That(Page.Body, Is.StringContaining("HostName: 127.0.0.1"));
			Assert.That(CurrentUrl, Is.EqualTo(string.Format("http://127.0.0.1:{0}/Stuff.aspx", port)));
			Assert.That(CurrentPath, Is.EqualTo("/Stuff.aspx"));
		}

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
