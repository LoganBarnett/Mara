using System;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;

using Mara;

using HtmlAgilityPack;

namespace Mara.Drivers {

    /*
     * Mara IDriver implementation for System.Net.WebClient
     */
    public partial class WebClient : IDriver {

        // Little WebClient wrapper that tracks cookies for subsequent requests
        public class CookieAwareWebClient : System.Net.WebClient {
            CookieContainer cookies = new CookieContainer();
            protected override WebRequest GetWebRequest(Uri address) {
                WebRequest request = base.GetWebRequest(address);
                if (request is HttpWebRequest)
                    (request as HttpWebRequest).CookieContainer = cookies;
                return request;
            }
        }

        CookieAwareWebClient _client;
        internal CookieAwareWebClient Client {
            get {
                if (_client == null) ResetSession();
                return _client;
            }
            set { _client = value; }
        }

        string AppHost { get { return Mara.AppHost; }}

        public void ResetSession() {
            _client = new CookieAwareWebClient();
            _client.BaseAddress = AppHost;
        }

        public string Body { get; set; }

        public void Visit(string path) {
            Body = Client.DownloadString(path);
            Doc  = null;
            _currentPath = path;
        }

        // also initialize forms here
        HtmlNode _doc;
        internal HtmlNode Doc {
            get {
                if (_doc == null) {
                    HtmlNode.ElementsFlags.Remove("form"); // fix weird <form> behavior
                    var html = new HtmlDocument();
                    html.Load(new StringReader(Body));
                    _doc = html.DocumentNode;
                }
                return _doc;
            }
            set { _doc = value; }
        }

        internal string _currentPath;
        public string CurrentPath {
            get { return _currentPath; } 
        }

        public string CurrentUrl {
            get { return AppHost + CurrentPath; }
        }

        // TODO Move this into some common code?  a Driver baseclass with some common functionality?  or ... SOMETHING?
        public string SaveAndOpenPage() {
            var fileName = Path.Combine(Path.GetTempPath(), "Mara_" + DateTime.Now.ToString("yyyy-MM-dd_HHmmssffff") + ".html");

            using (var writer = new StreamWriter(fileName))
                writer.WriteLine(Body);

            // TODO document and test SAVE_AND_OPEN_PAGE=false
            if (Environment.GetEnvironmentVariable("SAVE_AND_OPEN_PAGE") == null)
                System.Diagnostics.Process.Start(fileName); // open HTML file in user's default browser

            return fileName;
        }

        public IElement Find(string xpath) {
            return Find(xpath, false);
        }

        // This is VERY like the WebDriver implementation ... could be DRY'd up?
        public IElement Find(string xpath, bool throwExceptionIfNotFound) {
            var node = Doc.SelectSingleNode(xpath);
            if (node == null) {
                if (throwExceptionIfNotFound)
                    throw new ElementNotFoundException(xpath);
                else
                    return null;
            } else
                return new Element(node, this);
        }

        public List<IElement> All(string xpath) {
            var list  = new List<IElement>();
            var nodes = Doc.SelectNodes(xpath);
            if (nodes != null)
                foreach (var node in nodes)
                    list.Add(new Element(node, this));
            return list;
        }

        public bool HasXPath(string xpath) {
            return Doc.SelectSingleNode(xpath) != null;
        }

        public bool HasContent(string text) {
            return Body.Contains(text);
        }

        // TODO this is another one that's pretty much the same in all drivers nomatter what ... REFACTOR/DRY up!
        public void Click(string linkOrButton) {
            var link_XPath   = "//a[text()='" + linkOrButton + "']";
            var button_XPath = "//input[@type='submit'][@value='" + linkOrButton + "']";

            // try link first, then button ...
            var element = Find(link_XPath);
            if (element == null)
                element = Find(button_XPath);
            if (element == null)
                throw new ElementNotFoundException(link_XPath + " OR " + button_XPath);
            else
                element.Click();
        }

        // TODO this is the same as other drivers ...
        public void ClickLink(string linkText) {
            Find("//a[text()='" + linkText + "']", true).Click();
        }

        // TODO this is the same as other drivers ...
        public void ClickButton(string buttonValue) {
            Find("//input[@type='submit'][@value='" + buttonValue + "']", true).Click();
        }

        public void Refresh() {
            Visit(CurrentUrl);
        }

        public void Close() {
            // Do nothing
        }

        public bool JavaScriptSupported { get { return false; }}

        public object EvaluateScript(string script) {
            throw new NotSupportedException("WebClient does not support JavaScript.  You can check IDriver.JavaScriptSupported to detect this.");
        }

        public void ExecuteScript(string script) {
            throw new NotSupportedException("WebClient does not support JavaScript.  You can check IDriver.JavaScriptSupported to detect this.");
        }

        // TODO all FillIn[Fields] method implementations are identical between WebClient and WebDriver ... refactor!!!!!!
        public void FillIn(string field, string value) {
            var id_XPath   = "id('" + field + "')";
            var name_XPath = "//*[@name='" + field + "']";

            // try ID first, then name ...
            var element = Find(id_XPath);
            if (element == null)
                element = Find(name_XPath);
            if (element == null)
                throw new ElementNotFoundException(id_XPath + " OR " + name_XPath);
            else
                element.Value = value;
        }
        public void FillInFields(object fieldsAndValues) {
            FillInFields(fieldsAndValues.ToDictionary());
        }
        public void FillInFields(IDictionary<string, object> fieldsAndValues) {
            foreach (var field in fieldsAndValues)
                FillIn(field.Key, field.Value.ToString());
        }

        public class Element : IElement {
            WebClient Driver { get; set; }
            HtmlNode  Node   { get; set; }

            internal Element(HtmlNode node, WebClient driver) {
                Driver = driver;
                Node   = node;
            }

            string Name { get { return Node.Name.ToLower(); }}

            HtmlNode _parentForm;
            HtmlNode ParentForm {
                get {
                    if (_parentForm == null)
                        foreach (var node in Node.Ancestors("form"))
                            _parentForm = node;
                    return _parentForm;
                }
            }

            public void Click() {
                if (Name == "a")
                    FollowLink();
                else if (Name == "input" && this["type"] == "submit")
                    SubmitForm();
                else
                    throw new Exception("I don't know how to Click() element: " + this.ToString());
            }

            void FollowLink() {
                if (this["href"] != null)
                    Driver.Visit(this["href"]);
                else
                    throw new Exception("Cannot click link with no href: " + this.ToString());
            }

            void SubmitForm() {
                if (ParentForm == null)
                    throw new Exception("Cannot click button that is not in a form: " + this.ToString());
                
                var action = (ParentForm.Attributes.Contains("action")) ? ParentForm.Attributes["action"].Value : Driver.CurrentUrl;
                if (action.StartsWith("/"))
                    action = Driver.AppHost + action;
                else if (! action.StartsWith("http"))
                    action = Driver.AppHost + "/" + action;

                var formFields = new NameValueCollection();
                foreach (var node in ParentForm.DescendantNodes()) {
                    switch (node.Name.ToLower()) {
                        case "input":
                            if (node.Attributes.Contains("name") && node.Attributes.Contains("value"))
                                formFields[node.Attributes["name"].Value] = node.Attributes["value"].Value;
                            break;
                    }
                }

                // Make the actual request.  We also update some variables on the Driver, like Visit() normally does
                byte[] response     = Driver.Client.UploadValues(action, "POST", formFields);
                Driver.Body         = Encoding.UTF8.GetString(response);
                Driver.Doc          = null;
                Driver._currentPath = action.Replace(Driver.AppHost, "");
            }

            string _value;
            public string Value {
                get { return _value ?? this["value"]; }
                set {
                    if (Name == "input")
                        this["value"] = value;
                    else
                        throw new Exception("We don't know how to set the value of non-input element: " + this.ToString());
                }
            }

            public string Text {
                get { return Node.InnerText.Cleanup(); }
            }

            public string this[string attributeName] {
                get {
                    if (Node.Attributes.Contains(attributeName))
                        return Node.Attributes[attributeName].Value;
                    else
                        return null;
                }
                set { Node.SetAttributeValue(attributeName, value); }
            }

            public override string ToString() {
                var attributesString = string.Join(" ", Node.Attributes.Select(a => string.Format("{0}=\"{1}\"", a.Name, a.Value)).ToArray<string>());
                if (attributesString.Length > 0) attributesString = " " + attributesString;
                return string.Format("<{0}{1}>{2}</{0}>", Node.Name, attributesString, Text);
            }
        }
    }

    // TODO this is copy/pasted from the WebDriver driver ... DRY up!
    public static class WebClient_CompactStringExtension {

        static readonly Regex AnyNumberOfSpaces = new Regex(@"\s+");

        // Take all spaces and newlines and compact them into a single space
        public static string Compact(this string str) {
            return AnyNumberOfSpaces.Replace(str, " ");
        }

        // Trim the string AND Compact it
        public static string Cleanup(this string str) {
            return str.Compact().Trim();
        }

        public static string Inspect(this HtmlNode node) {
            var attributesString = string.Join(" ", node.Attributes.Select(a => string.Format("{0}=\"{1}\"", a.Name, a.Value)).ToArray<string>());
            if (attributesString.Length > 0) attributesString = " " + attributesString;
            return string.Format("<{0}{1}>{2}</{0}>", node.Name, attributesString, node.InnerHtml);
        }
    }
}
