using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Mara;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.IE;

namespace Mara.Drivers {

    /*
     * Mara IDriver implementation for Selenium WebDriver
     *
     * TODO this is getting BIG!  Split into multiple organized files!  IDriver implementation should be in its own file (if not multiple)
     */
    public partial class WebDriver : IDriver {

        public static string DefaultBrowser            = "firefox";
        public static int    DefaultSeleniumServerPort = 4444;
        public static string DefaultSeleniumServerJar  = "selenium-server-standalone.jar";

        public WebDriver() {
            SetSeleniumSettingsFromEnvironmentVariables();
            SetBrowserAndRemoteFromEnvironmentVariables();

            // Override whether or not to run the selenium standalone server
            var runSelenium = Environment.GetEnvironmentVariable("RUN_SELENIUM");
            if (runSelenium != null) {
                if (runSelenium == "true")
                    RunSeleniumStandalone = true;
                else if (runSelenium == "false")
                    RunSeleniumStandalone = false;
            }
        }

        void SetBrowserAndRemoteFromEnvironmentVariables() {
            // TODO test this
            // Handle environment variables like CHROME_PATH or FIREFOX_PATH
            WebDriver.OverrideBrowserPathsFromEnvironmentVariables();

			var browserName = Environment.GetEnvironmentVariable("BROWSER");
			var remoteUri   = Environment.GetEnvironmentVariable("REMOTE");
			var htmlUnitUri = Environment.GetEnvironmentVariable("HTMLUNIT");

			// HTMLUNIT is a shortcut for setting BROWSER=HtmlUnit and REMOTE=[the remote uri]
			if (htmlUnitUri != null) {
				browserName = "HtmlUnit";
				remoteUri   = htmlUnitUri;
			}

            // REMOTE=true is a shortcut for telling us to run the default, local selenium server
            if (remoteUri != null && remoteUri.ToLower() == "true")
                remoteUri = DefaultRemote;

            if (browserName != null) Browser = browserName;
            if (remoteUri   != null) Remote  = remoteUri;
        }

        void SetSeleniumSettingsFromEnvironmentVariables() {
            var seleniumJar  = Environment.GetEnvironmentVariable("SELENIUM_JAR");
            var seleniumPort = Environment.GetEnvironmentVariable("SELENIUM_PORT");

            if (seleniumJar != null)
                SeleniumServerJar = seleniumJar;

            if (seleniumPort != null)
                SeleniumServerPort = int.Parse(seleniumPort);
        }

        static void OverrideBrowserPathsFromEnvironmentVariables() {
            // TODO is there any harm in simply looking for all environment variables ending in _PATH?
            foreach (var browser in new List<String>(){ "chrome", "firefox", "ie" }){
                var path = Environment.GetEnvironmentVariable(browser.ToUpper() + "_PATH");
                if (path != null) {
                    // Override the webdriver.[browser].bin variable
                    Environment.SetEnvironmentVariable("webdriver." + browser + ".bin", path);

                    // Set the BROWSER environment variable to this browser, if it's not already set
                    if (Environment.GetEnvironmentVariable("BROWSER") == null)
                        Environment.SetEnvironmentVariable("BROWSER", browser);
                }
            }
        }

        public bool? RunSeleniumStandalone { get; set; }

        string _seleniumServerJar;
        public string SeleniumServerJar {
            get {
                if (_seleniumServerJar == null) return WebDriver.DefaultSeleniumServerJar;
                return _seleniumServerJar;
            }
            set { _seleniumServerJar = value; }
        }

        int _seleniumServerPort = -1;
        public int SeleniumServerPort {
            get {
                if (_seleniumServerPort == -1) return WebDriver.DefaultSeleniumServerPort;
                return _seleniumServerPort;
            }
            set { _seleniumServerPort = value; }
        }

        string DefaultRemote {
            get { return String.Format("http://localhost:{0}/wd/hub", SeleniumServerPort); }
        }

        string _browser;
        public string Browser {
            get {
                if (_browser == null) return WebDriver.DefaultBrowser;
                return _browser.ToLower();
            }
            set {
                if (value != null && value.ToLower() == "htmlunit" && Remote == null)
                    Remote = DefaultRemote;
                _browser = value;
            }
        }

        public string SeleniumServerUrl {
            get { return string.Format("http://localhost:{0}/wd/hub", SeleniumServerPort); }
        }

        string _remote;
        public string Remote {
            get { return _remote; }
            set {
                // If you set Remote to localhost using the SeleniumServerPort, set RunSeleniumStandalone to true (unless already set)
                if (RunSeleniumStandalone == null)
                    if (value != null && value == SeleniumServerUrl)
                        RunSeleniumStandalone = true;

                // If you set Remote to something besides localhost, there's no way we can run it!
                if (RunSeleniumStandalone == true)
                    if (value != null && value != SeleniumServerUrl)
                        RunSeleniumStandalone = false;

                _remote = value;
            }
        }

        IWebDriver _webdriver; // TODO webdriver could be a confusing name, maybe?  I think NativeDriver is a better name.  Could be standard in all drivers
        IWebDriver webdriver {
            get {
                if (_webdriver == null) _webdriver = InstantiateWebDriver();
                return _webdriver;
            }
        }

        IWebDriver InstantiateWebDriver() {
            Mara.Log("InstantiateWebDriver()");
            Mara.Log("  Browser: {0}",               Browser);
            Mara.Log("  Remote: {0}",                Remote);
            Mara.Log("  RunSeleniumStandalone: {0}", RunSeleniumStandalone);

            if (RunSeleniumStandalone == true)
                StartSeleniumStandalone();

            if (Remote == null)
                return InstantiateLocalWebDriver(Browser);
            else
                return InstantiateRemoteWebDriver(Browser, Remote);
        }

        IWebDriver InstantiateLocalWebDriver(string browserName) {
            Mara.Log("InstantiateLocalWebDriver({0}) ...", browserName);
			switch (browserName) {
				case "chrome":
                    return new ChromeDriver();
				case "firefox":
					return new FirefoxDriver();
                case "ie":
                    //return new InternetExplorerDriver();
                    Mara.Log("launching IE ...");
                    var ie = new InternetExplorerDriver();
                    Mara.Log("launched? returning ...");
                    return ie;
				default:
					throw new Exception("Unsupported browser: " + browserName);
            }
        }

        IWebDriver InstantiateRemoteWebDriver(string browserName, string remote) {
            Mara.Log("InstantiateRemoteWebDriver({0}, {1})", browserName, remote);
		    var capabilities = new DesiredCapabilities(browserName, string.Empty, new Platform(PlatformType.Any));
		    var remoteUri    = new Uri(remote); // TODO add /wd/hub here so it doesn't have to be used everyplace else

            // Note: we might want to make it easy to get access to an HtmlUnit driver that does *NOT* execute 
            //       JavaScrpt *AND* one that *DOES* execute JavaScript for performance ... way faster tests?
            if (Environment.GetEnvironmentVariable("NO_JAVASCRIPT") == null)
                capabilities.IsJavaScriptEnabled = true; // Run with JavaScript support, please!  (Needed for HtmlUnit)

		    return new RemoteWebDriver(remoteUri, capabilities);
        }

        public void Close() {
            StopSeleniumStandalone();
            webdriver.Close();
        }

        public void ResetSession() {
            // TODO test me!
            webdriver.Manage().DeleteAllCookies(); // FIXME this might not be working with firefox at the moment???
        }

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

        public void ClickLink(string linkText) {
            Find("//a[text()='" + linkText + "']", true).Click();
        }

        public void ClickButton(string buttonValue) {
            Find("//input[@type='submit'][@value='" + buttonValue + "']", true).Click();
        }

        public void Refresh() {
            // IE - probably have to "F5" ...
            webdriver.Navigate().Refresh();
        }

        // TODO I don't like using Mara.AppHost ... ?  using something static feels icky?  maybe?
        public void Visit(string path) {
			var url = (path.StartsWith("/")) ? (Mara.AppHost + path) : path;

            Mara.Log("Visit({0})", url);

            // The ChromeDriver hates life sometimes ...
            if (Browser == "chrome")
                for (var i = 0; i < 10; i ++)
                    if (TryToVisitInChrome(url))
                        break;
                    else {
                        Mara.Log("Chrome didn't want to visit {0} ... trying again ... ", url);
                        System.Threading.Thread.Sleep(100);
                    }   
            else
                webdriver.Navigate().GoToUrl(url);
        }

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

        // TODO TEST!
        public void FillInFields(IDictionary<string, object> fieldsAndValues) {
            foreach (var field in fieldsAndValues)
                FillIn(field.Key, field.Value.ToString());
        }

		public void Check(string checkbox) {
			(CheckBox(checkbox) as WebDriver.Element).NativeElement.Select();
		}

		public void Uncheck(string checkbox) {
			var element = CheckBox(checkbox) as WebDriver.Element;
			if (element.NativeElement.Selected)
				element.NativeElement.Toggle();
		}

		IElement CheckBox(string checkbox) {
            var id_XPath   = "//input[@type='checkbox'][@id='" + checkbox + "']";
            var name_XPath = "//input[@type='checkbox'][@name='" + checkbox + "']";

            // try ID first, then name ...
            var element = Find(id_XPath);
            if (element == null)
                element = Find(name_XPath);
            if (element == null)
                throw new ElementNotFoundException(id_XPath + " OR " + name_XPath);

			return element;
		}

		public void Select(string dropdown, string option) {
			// Find the <select>
            var id_XPath   = "//select[@id='" + dropdown + "']";
            var name_XPath = "//select[@name='" + dropdown + "']";
            var element = Find(id_XPath);
            if (element == null)
                element = Find(name_XPath);
            if (element == null)
                throw new ElementNotFoundException(id_XPath + " OR " + name_XPath);

			// Find the <option>
			//
			// Mara needs to add IElement.Find() and IElement.All() to search for subqueries ... right now, we need to get the native WebDriver element
			//
			Element nativeElement = element as WebDriver.Element;
			var options           = WebDriver.Element.List(nativeElement.NativeElement.FindElements(By.TagName("option")), this);

			// find an option with the matching text OR value for this select
			var matchingOption = options.FirstOrDefault(o => {
				var text  = (o.Text  == null) ? "" : o.Text.Trim();		
				var value = (o.Value == null) ? "" : o.Value.Trim();		
				return (text == option.Trim() || value == option.Trim());
			});

			if (matchingOption != null)
				(matchingOption as WebDriver.Element).NativeElement.Select();
			else
                throw new ElementNotFoundException("<option> with Text or Value: " + option);
		}

        public string Body {
            get { return webdriver.PageSource; }
        }

        public string CurrentUrl {
            get { return webdriver.Url; }
        }

        public string CurrentPath {
            get { return new Uri(CurrentUrl).LocalPath; } 
        }

        public bool JavaScriptSupported { get { return true; }}

        public void ExecuteScript(string script) {
            EvaluateScript(script);
        }

        public object EvaluateScript(string script) {
            return (webdriver as IJavaScriptExecutor).ExecuteScript(script);
        }

        public IElement Find(string xpath) {
            return Find(xpath, false);
        }

        public IElement Find(string xpath, bool throwExceptionIfNotFound) {
            try {
                return new Element(FindByXPath(xpath), this);
            } catch (NoSuchElementException) {
                if (throwExceptionIfNotFound)
                    throw new ElementNotFoundException(xpath);
                else
                    return null;
            }
        }

        public List<IElement> All(string xpath) {
            return Element.List(webdriver.FindElements(By.XPath(xpath)), this);
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
        
        public bool HasXPath(string xpath) {
            return (Find(xpath) != null);
        }
        
        public bool HasContent(string text) {
            return Body.Contains(text);
        }

        // private methods

        bool PageHasLoaded {
            get {
                if (webdriver == null) return false;
                try {
                    if (webdriver.PageSource == null) return false;
                    return true;
                } catch (NullReferenceException) {
                    Mara.Log("Page has not loaded ...");
                    return false; // calling driver.PageSource probably blew up (happens in the ChromeDriver)
                }   
            }   
        } 

        Process _seleniumStandalone;

        void StartSeleniumStandalone() {
            Mara.Log("StartSeleniumStandalone");
            _seleniumStandalone = new Process();
            _seleniumStandalone.StartInfo.FileName               = "java";
            _seleniumStandalone.StartInfo.Arguments              = string.Format("-jar {0} -port {1}", Path.GetFullPath(SeleniumServerJar), SeleniumServerPort);
            _seleniumStandalone.StartInfo.UseShellExecute        = false;
            _seleniumStandalone.StartInfo.CreateNoWindow         = true;

            // TODO document and test SELENIUM_LOG variable
            if (Environment.GetEnvironmentVariable("SELENIUM_LOG") == null) // if there's no SELENIUM_LOG variable, don't print STDOUT
                _seleniumStandalone.StartInfo.RedirectStandardOutput = true;

            Mara.Log("Selenium Standalone starting ... ");
            _seleniumStandalone.Start();
            Mara.WaitForLocalPortToBecomeUnavailable(SeleniumServerPort, 100, 200); // 20 seconds max ... checking every 0.1 seconds
            Mara.Log("done");
        }

        void StopSeleniumStandalone() {
            if (_seleniumStandalone != null) {
                Mara.Log("Selenium Standalone stopping ... ");
                _seleniumStandalone.Kill();
                Mara.WaitForLocalPortToBecomeAvailable(SeleniumServerPort);
                Mara.Log("done");
            }
        }

        bool TryToVisitInChrome(string url) {
            webdriver.Navigate().GoToUrl(url);
            try {
                webdriver.GetWindowHandle();
                return true;
            } catch (NullReferenceException) {
                return false;
            } 
        }

        IWebElement FindByXPath(string xpath) {
            return webdriver.FindElement(By.XPath(xpath));
        }
    }
}
