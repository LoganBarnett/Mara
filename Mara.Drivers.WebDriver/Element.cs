using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Mara;
using Mara.Drivers;

using OpenQA.Selenium;

namespace Mara.Drivers {

    public static class CompactStringExtension {

        static readonly Regex AnyNumberOfSpaces = new Regex(@"\s+");

        // Take all spaces and newlines and compact them into a single space
        public static string Compact(this string str) {
            return AnyNumberOfSpaces.Replace(str, " ");
        }

        // Trim the string AND Compact it
        public static string Cleanup(this string str) {
            return str.Compact().Trim();
        }
    }

    public partial class WebDriver : IDriver {

        /*
         * WebDriver.Element
         *
         * WebDriver's implementation of Mara.IElement
         */
        public class Element : IElement {

            const string NO_VALUE_ATTRIBUTE_MESSAGE = "Element does not have a value attribute";

            internal static List<IElement> List(ReadOnlyCollection<IWebElement> nativeElements, WebDriver webDriver) {
                var list = new List<IElement>();
                foreach (var webElement in nativeElements)
                    list.Add(new Element(webElement, webDriver));
                return list;
            }

            IWebElement NativeElement { get; set; }
            WebDriver   ParentDriver  { get; set; }

            internal Element(IWebElement nativeElement, WebDriver webDriver) {
                NativeElement = nativeElement;
                ParentDriver  = webDriver;
            }

            public void Click() {
                NativeElement.Click();
            }

            public string Value {
                get { return this["value"]; }
                set {
                    NativeElement.Clear();
                    NativeElement.SendKeys(value);
                }
            }

            public string Text {
                get {
                    // HtmlUnit formats the content of <pre> tags as: [content with newlines]\n\n[content without newlines]
                    if (ParentDriver.Browser == "htmlunit" && NativeElement.TagName == "pre") {
                        var lastIndex = NativeElement.Text.LastIndexOf("\n\n");
                        return NativeElement.Text.Substring(lastIndex + 2).Cleanup(); // return everything after the last \n\n
                    }
                    
                    try {
                        // Normalize by replacing any number of spaces/newlines with a single space
                        return NativeElement.Text.Cleanup();
                    } catch (InvalidOperationException) { // InternetExplorer
                        return "";
                    }
                }
            }

            public string this[string attributeName] {
                get { return NativeElement.GetAttribute(attributeName); }
            }
        }
    }
}
