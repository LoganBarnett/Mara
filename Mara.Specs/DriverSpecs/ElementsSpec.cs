using System;
using System.Linq;
using NUnit.Framework;
using Mara;

namespace Mara.DriverSpecs {

    /*
     * The spec covers the basics of finding and interacting with elements:
     *
     *   Find()
     *   All()
     *   Find("...").Click()
     *   Find("...").Text
     *   ClickLink("...")
     */
    [TestFixture]
    public class ElementsSpec : MaraTest {

        [SetUp] public void Setup(){ Visit("/"); }

        [Test]
        public void CanFindElementUsingXPath_ReturnsNullIfNotFound() {
             Assert.That(Find("//h2").Text, Is.EqualTo("Why is this styled?"));
             Assert.Null(Find("//h5"));
        }

        [Test]
        public void CanFindElementUsingXPath_BlowsUpIfNotFound() {
            Assert.That(Find("//h2").Text, Is.EqualTo("Why is this styled?"));

            this.AssertThrows<ElementNotFoundException>("Could not find element with XPath: //h5", () => {
                Find("//h5", true);
            });
        }

        [Test]
        public void CanFindElementsUsingXPath_ReturnsEmptyIfNotFound() {
            Assert.That(All("//p").Count, Is.EqualTo(5));
            Assert.That(All("//p").Select(e => e.Text).ToList<string>(), Has.Member("If you see this text, you're running our test suite!"));

            Assert.IsEmpty(All("//strong"));
            Assert.That(All("//strong").Count, Is.EqualTo(0));
        }

        [Test]
        public void CanGetTheValueOfAnElement_NullIfNotPresent() {
            ClickLink("Stuff");
            Assert.That(Find("//*[@name='DogName']").Value, Is.EqualTo("Rover"));
            Assert.Null(Find("//pre").Value);
        }

        [Test]
        public void CanGetTheTextOfAnElement_EmptyStringIfNotPresent() {
            ClickLink("Stuff");
            Assert.That(Find("//h1").Text, Is.EqualTo("Mara"));
            Assert.That(Find("id('content')//h1").Text, Is.EqualTo("Miscellaneous Stuff"));

            // p with newlines
            //
            // We try to normalize by compressing any spaces/newlines into a *single* newline
            //
            // After all, that fits the web very well because ANY number of spaces in 
            // your HTML gets rendered as a single space (hence the need for &nbsp;)
            //
            Assert.That(Find("//p").Text, Is.EqualTo("Hi, how goes? Does it go well?"));
            // HtmlUnit: Hi, how goes? \n Does it go well?
            // FF:       Hi, how goes?\nDoes it go well?
            // Chrome:   Hi, how goes? \n Does it go well?
            // IE:       Hi, how goes? \r\nDoes it go well?

            Assert.That(Find("id('i_haz_pre')//pre").Text, Is.EqualTo("I am another pre"));
            // HtmlUnit: "I\nam\nanother\npre\n  \n\n I am another pre"
            // FF:       I am another pre ... killed the newlines
            // Chrome:   I am another pre
            // IE:       I\ram\ranother\rpre
            
            Assert.That(Find("//pre").Text, Is.EqualTo("Dogs have names"));
            Assert.That(Find("//*[@name='DogName']").Text, Is.EqualTo(""));
        }

        [Test][Ignore]
        public void CanGetTheValueOfAnElementsAttribute() {}

        [Test]
        public void CanClickLink_BlowsUpIfNotFound() {
            this.AssertThrows<ElementNotFoundException>("Could not find element with XPath: //a[text()='No Link With This Text']", () => {
                ClickLink("No Link With This Text");
            });

            Assert.That(CurrentPath, Is.EqualTo("/"));
            ClickLink("Stuff");
            Assert.That(CurrentPath, Is.EqualTo("/Stuff.aspx"));
        }

        [Test]
        public void CanCheckIfPageHasContent() {
            Assert.True(  Page.HasContent("If you see this text, you're running our test suite!"));
            Assert.True(  Page.HasContent("Mara test application"));
            Assert.False( Page.HasContent("This text does not show up on the page"));
            Assert.False( Page.HasContent("Nor does this text"));
        }

        [Test]
        public void CanCheckIfPageHasXPath() {
            Assert.True(  Page.HasXPath("//h1"));
            Assert.True(  Page.HasXPath("//h2"));
            Assert.False( Page.HasXPath("//h3"));
            Assert.False( Page.HasXPath("//h4"));
        }
    }
}
