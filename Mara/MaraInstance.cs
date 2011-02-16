using System;
using System.Collections.Generic;
using Mara.Drivers;
using Mara.Servers;

namespace Mara {

    /*
     * This contains the instance methods for Mara.
     *
     * If your class inherits from Mara, it inherits Mara Magic!
     *
     * You'll be able to automagically Visit() your web application 
     * and Find() elements on the page, etc etc.
     *
     * This basically just wraps a Mara.Driver.  Actually, it *is* an IDriver!
     *
     * You definitely don't have to use this, but it makes things much easier.
     *
     */
    public partial class Mara : IDriver { // TODO ... inherit from Mara.Driver?  Maybe?  Dunno what to use Mara.Driver for *yet*

        IDriver _page;

        // This can be used in the [SetUp] part of your testing framework to setup Mara
        public void Initialize() {
            Mara.Log("Mara.Initialize()");
            if (Mara.RunServer)
                Mara.Server.Start();
        }

        // This can be used in the [TearDown] part of your testing framework to teardown Mara
        public void Shutdown() {
            Mara.Log("Mara.Shutdown()");
            if (Mara.RunServer) {
                Mara.Log("  Server.Stop ...");
                Mara.Server.Stop(); // <--- oh noes!  Mara.Server is GLOBAL?  icky.  hmm ... Server/Driver need to be in instances ... TODO FIXME
                Mara.Log("  Close() driver ...");
            }
            Close();
        }

        // This is the actual IDriver that Mara uses in the background
        // You can set this to use a different driver.
        public IDriver Page {
            get {
                if (_page == null) _page = Mara.Driver;
                return _page;
            }
            set {
                if (_page != null) _page.Close();
                _page = value;
            }
        }

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
        public void Check(string checkbox)                                    { Page.Check(checkbox);               }
        public void Uncheck(string checkbox)                                  { Page.Uncheck(checkbox);             }
        public void Select(string dropdown, string option)                    { Page.Select(dropdown, option);      }
        public void ExecuteScript(string script)                              { Page.ExecuteScript(script);         }
    }
}
