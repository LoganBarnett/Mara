using System;
using NUnit.Framework;
using Mara;

namespace Mara.DriverSpecs {

    /*
     * The spec covers the basics of calling JavaScript 
     * in drivers that support it
     *
     *   bool   JavaScriptSupported
     *   void   ExecuteScript("...")
     *   object EvaluateScript("...")
     */
    [TestFixture]
    public class JavaScriptSpec : MaraTest {

        [SetUp]
        public void JavaScriptSetup() {
            if (! Page.JavaScriptSupported)
                Assert.Ignore("This driver does not support JavaScript");
        }

        [Test]
        public void CanExecuteScript() {
            Visit("/Form.aspx");
            Click("POST some stuff");
            Assert.That(Page.Body, Is.Not.StringContaining("Set via JavaScript"));

            Visit("/Form.aspx");
            ExecuteScript("$('#DogName').val('Set via JavaScript');");
            Click("POST some stuff");
            Assert.That(Page.Body, Is.StringContaining("Set via JavaScript"));
        }

        [Test]
        public void CanEvaluateScript() {
            // NOTE these function definitions can be found in WebApp/public/application.js
            Visit("/");
            Assert.That(EvaluateScript("return iReturnStaticString()"), Is.EqualTo("static string!"));
            Assert.That(EvaluateScript("return iReturnStringUsingParameterPassed(42)"), Is.EqualTo("you passed: 42"));
            Assert.That(EvaluateScript("return iReturnStringUsingParametersPassed(42, 'hi', 12.34)"), Is.EqualTo("you passed: 42, hi, 12.34"));
        }
    }
}
