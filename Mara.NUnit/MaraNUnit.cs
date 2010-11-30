using System;
using System.Collections.Generic;
using Mara;
using Mara.Drivers;
using NUnit.Framework;

namespace Mara {

    /*
     * In your own test suites, create a [SetUpFixture] in the namespace 
     * you want to use for your tests and inherit it from MaraSetUpFixture.
     *
     *     namespace MyTests {
     *         [SetUpFixture] public class Fixture : MaraSetUpFixture {}
     *     }
     */
    public class MaraSetUpFixture {

        public static Mara MaraInstance;

        [SetUp]
        public void MaraSetUp() {
            Mara.Log("Global MaraSetUpFixture.SetUp");
            if (MaraInstance == null) {
                MaraInstance = new Mara();
                MaraInstance.Initialize();
            }
        }

        [TearDown]
        public void MaraTearDown() {
            Mara.Log("Global MaraSetUpFixture.TearDown");
            MaraInstance.Shutdown();
        }
    }

    /*
     * If you use the IntegrationTests namespace, inheriting from 
     * IntegrationTest will give you a DSL with access to the global 
     * Mara instance
     */
    public class MaraTest : IDriver {
        public IDriver Page { get { return MaraSetUpFixture.MaraInstance; }}

        // Everything below here can be copy/pasted from Mara/MaraInstance.cs

        public string Body              { get { return Page.Body;                }}
        public string CurrentUrl        { get { return Page.CurrentUrl;          }}
        public string CurrentPath       { get { return Page.CurrentPath;         }}
        public bool JavaScriptSupported { get { return Page.JavaScriptSupported; }}

        public IElement       Find(string xpath)                                { return Page.Find(xpath);                           }
        public IElement       Find(string xpath, bool throwExceptionIfNotFound) { return Page.Find(xpath, throwExceptionIfNotFound); }
        public List<IElement> All(string  xpath)                                { return Page.All(xpath);                            }
        public object         EvaluateScript(string script)                     { return Page.EvaluateScript(script);                }
        public string         SaveAndOpenPage()                                 { return Page.SaveAndOpenPage();                     }
        public bool           HasXPath(string xpath)                            { return Page.HasXPath(xpath);                       }
        public bool           HasContent(string text)                           { return Page.HasContent(text);                      }

        public void Refresh()                                                 { Page.Refresh();                     }
        public void Close()                                                   { Page.Close();                       }
        public void ResetSession()                                            { Page.ResetSession();                }
        public void Visit(string path)                                        { Page.Visit(path);                   }
        public void Click(string linkOrButton)                                { Page.Click(linkOrButton);           }
        public void ClickLink(string linkText)                                { Page.ClickLink(linkText);           }
        public void ClickButton(string buttonValue)                           { Page.ClickButton(buttonValue);      }
        public void FillIn(string field, string value)                        { Page.FillIn(field, value);          }
        public void FillInFields(object fieldsAndValues)                      { Page.FillInFields(fieldsAndValues); }
        public void FillInFields(IDictionary<string, object> fieldsAndValues) { Page.FillInFields(fieldsAndValues); }
        public void ExecuteScript(string script)                              { Page.ExecuteScript(script);         }
    }
}
