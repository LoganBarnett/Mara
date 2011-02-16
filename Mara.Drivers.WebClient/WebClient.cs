using System;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Net;
using System.Web;
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

		public class SelectOption {
			public string   Value      { get; set; }
			public bool     IsSelected { get; set; }
			public string   Text       { get; set; }
			public HtmlNode Node       { get; set; }

			public string FormValue {
				get { return (Value != null) ? Value.Trim() : Text.Trim(); }
			}

			public SelectOption() {
				IsSelected = false;
			}

            public void Select() {
                if (Node.Attributes.Contains("selected"))
					Node.Attributes["selected"].Value = "seleted";
				else
					Node.Attributes.Append("selected", "selected");
            }

            public void Deselect() {
                if (Node.Attributes.Contains("selected"))
                    Node.Attributes.Remove("selected");
            }

			public static List<SelectOption> GetOptionsFromSelectNode(HtmlNode selectNode) {
				var options = new List<SelectOption>();

				// to get options and their values, we have to go through all of this <select>'s child nodes.
				// <option> text shows up in text nodes following <option> nodes.
				// we build up a list of SelectOptions that we can use to determine the value;
				foreach (var childNode in selectNode.ChildNodes) {
					// add <option> to list
					if (childNode.Name == "option") {
						var option        = new SelectOption();
						option.IsSelected = (childNode.Attributes.Contains("selected") && childNode.Attributes["selected"].Value != "false");
						option.Node       = childNode;
						if (childNode.Attributes.Contains("value") && childNode.Attributes["value"].Value.Trim().Length > 0)
							option.Value = childNode.Attributes["value"].Value;
						options.Add(option);

						// add this text to the last <option> in the list
					} else if (childNode.NodeType == HtmlNodeType.Text && options.Count > 0) {
						options[options.Count - 1].Text += childNode.InnerText;
					}
				}

				return options;
			}
		}

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
        }

        public string Body { get; set; }

        public void Visit(string path) {
			var url = (path.StartsWith("/")) ? (Mara.AppHost + path) : path;

            Body        = Client.DownloadString(url);
            Doc         = null;
            _currentUrl = url;
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

        public string CurrentPath {
            get { return new Uri(CurrentUrl).LocalPath; } 
        }

		internal string _currentUrl;
        public string CurrentUrl {
            get { return _currentUrl; }
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

		public void Check(string checkbox) {
            var id_XPath   = "//input[@type='checkbox'][@id='" + checkbox + "']";
            var name_XPath = "//input[@type='checkbox'][@name='" + checkbox + "']";

            // try ID first, then name ...
            var element = Find(id_XPath);
            if (element == null)
                element = Find(name_XPath);
            if (element == null)
                throw new ElementNotFoundException(id_XPath + " OR " + name_XPath);
            else
                element.Value = "on";
		}

		public void Uncheck(string checkbox) {
            var id_XPath   = "//input[@type='checkbox'][@id='" + checkbox + "']";
            var name_XPath = "//input[@type='checkbox'][@name='" + checkbox + "']";

            // try ID first, then name ...
            var element = Find(id_XPath);
            if (element == null)
                element = Find(name_XPath);
            if (element == null)
                throw new ElementNotFoundException(id_XPath + " OR " + name_XPath);
            else
                element.Value = null;
		}

		public void Select(string dropdown, string option) {
            // Console.WriteLine("Select({0}, {1})", dropdown, option);
            var id_XPath   = "//select[@id='" + dropdown + "']";
            var name_XPath = "//select[@name='" + dropdown + "']";

            // try ID first, then name ...
            var element = Find(id_XPath);
            if (element == null)
                element = Find(name_XPath);
            if (element == null)
                throw new ElementNotFoundException(id_XPath + " OR " + name_XPath);

			Element nativeElement = element as WebClient.Element;
			var options = SelectOption.GetOptionsFromSelectNode(nativeElement.Node);

			// find an option with the matching text OR value for this select
			var matchingOption = options.FirstOrDefault(o => {
				var text  = (o.Text  == null) ? "" : o.Text.Trim();		
				var value = (o.Value == null) ? "" : o.Value.Trim();		
				return (text == option.Trim() || value == option.Trim());
			});

            // if we found it, make sure to DESELECT all other <options>
            if (matchingOption != null)
                foreach (var o in options)
                    o.Deselect();

            // Console.WriteLine("matching option value:{0} selected:{1} html:{2}", matchingOption.Value, matchingOption.IsSelected, matchingOption.Node.InnerHtml);

			// update the <option> node to have selected="selected" or, if not found, throw ElementNotFoundException);
			if (matchingOption != null) {
				matchingOption.Select();
                // Console.WriteLine("Set selected = selected");
			} else
                throw new ElementNotFoundException("<option> with Text or Value: " + option);
		}

        public class Element : IElement {
            public WebClient Driver { get; set; }
            public HtmlNode  Node   { get; set; }

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
                else if (Name == "input" && (this["type"] == "submit" || this["type"] == "image"))
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

                var host   = Driver.CurrentUrl.Replace(new Uri(Driver.CurrentUrl).PathAndQuery, ""); // http://localhost:1234
                var action = (ParentForm.Attributes.Contains("action")) ? ParentForm.Attributes["action"].Value : Driver.CurrentUrl;
                if (action.StartsWith("/"))
                    action = host + action;
                else if (! action.StartsWith("http"))
                    action = host + "/" + action;

                var formFields = new NameValueCollection();
                foreach (var node in ParentForm.DescendantNodes()) {
					string id    = node.Attributes.Contains("id")    ? node.Attributes["id"].Value.Trim()    : null;
					string name  = node.Attributes.Contains("name")  ? node.Attributes["name"].Value.Trim()  : null;
					string type  = node.Attributes.Contains("type")  ? node.Attributes["type"].Value.Trim()  : null;
					string value = node.Attributes.Contains("value") ? node.Attributes["value"].Value.Trim() : "";

					// can't add a form field without a name
					if (name == null || name.Trim().Length == 0)
						continue;

                    if (new string[] { "input", "textarea", "select" }.Contains((node.Name))) {
                        // Console.WriteLine("Processing for submit: <{0} name='{1}' type='{2}' value='{3}' />", node.Name, name, type, value);
                    }

                    switch (node.Name.ToLower()) {

                        case "input":
							// if a checkbox's value is blank, we don't POST it
							if (type == "checkbox") {
								if (value != "")
									formFields[name] = value;
							} else if (name != null && name.Length > 0) {
								formFields[name] = (value == null) ? "" : value;
							}
                            break;

						case "textarea":
							formFields[name] = node.InnerText;
							break;

						case "select":
                            // Console.WriteLine("Found a <select> to submit: {0}", name);
							value = null;
							var options = SelectOption.GetOptionsFromSelectNode(node);

                            // Console.WriteLine("options");
                            //foreach (var option in options)
                                // Console.WriteLine("\t{0} selected? {1}", option.FormValue, option.IsSelected);

							// now that we have all of the options parsed, let's get the selected value););
							if (options.Count > 0) {
								value = options.First().FormValue; // default to first
								foreach (var option in options) {
									if (option.IsSelected) {
										value = option.FormValue;
										break;
									}
								}
							}

                            // Console.WriteLine("This select's value is: {0}", value);

							if (value != null)
								formFields[name] = value;

							break;
                    }
                }

				var method = "POST";
				if (ParentForm.Attributes.Contains("method"))
					method = ParentForm.Attributes["method"].Value.ToUpper();

				Console.WriteLine("{0} {1}", method, action);
				for (int i = 0; i < formFields.Count; i++)
					Console.WriteLine("\t{0} = {1}", formFields.Keys[i], formFields[i]);
                Console.WriteLine("");

                // Make the actual request.  We also update some variables on the Driver, like Visit() normally does
                byte[] response;
			
				if (method == "POST") {
					response = Driver.Client.UploadValues(action, "POST", formFields);
					Driver.Body        = Encoding.UTF8.GetString(response);
					Driver.Doc         = null;
					Driver._currentUrl = action;

				} else {
					var url = action;

					if (formFields.Count > 0) {
						if (action.Contains("?"))
							action += "&";
						else
							action += "?";
					}

					// loop through the GET form's parameters and add them as query strings ...
					for (int i = 0; i < formFields.Count; i++) {
						action += string.Format("{0}={1}", formFields.Keys[i], HttpUtility.UrlEncode(formFields[i]));
						if (i != formFields.Count - 1)
							action += "&";
					}
					
					Console.WriteLine("GET {0}", action);

					// simply Visit() the url
					Driver.Visit(action);
				}
            }

            string _value;
            public string Value {
                get { return _value ?? this["value"]; }
                set {
                    if (Name == "input")
                        this["value"] = value;
					else if (Name == "textarea")
						this.Text = value;
                    else
                        throw new Exception("We don't know how to set the value of non-input element: " + this.ToString());
                }
            }

            public string Text {
                get { return (Name == "textarea") ? Node.InnerHtml : Node.InnerText.Cleanup(); } // don't cleanup textarea! return the raw InnerHtml content
				set { Node.InnerHtml = value; }
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
