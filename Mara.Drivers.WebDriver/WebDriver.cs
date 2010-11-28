﻿using System;
using System.IO;
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
            Console.WriteLine("InstantiateWebDriver()");
            Console.WriteLine("  Browser: {0}",               Browser);
            Console.WriteLine("  Remote: {0}",                Remote);
            Console.WriteLine("  RunSeleniumStandalone: {0}", RunSeleniumStandalone);

            if (RunSeleniumStandalone == true)
                StartSeleniumStandalone();

            if (Remote == null)
                return InstantiateLocalWebDriver(Browser);
            else
                return InstantiateRemoteWebDriver(Browser, Remote);
        }

        IWebDriver InstantiateLocalWebDriver(string browserName) {
            Console.WriteLine("InstantiateLocalWebDriver({0})", browserName);
			switch (browserName) {
				case "chrome":
                    return new ChromeDriver();
				case "firefox":
					return new FirefoxDriver();
                case "ie":
                    return new InternetExplorerDriver();
				default:
					throw new Exception("Unsupported browser: " + browserName);
            }
        }

        IWebDriver InstantiateRemoteWebDriver(string browserName, string remote) {
            Console.WriteLine("InstantiateRemoteWebDriver({0}, {1})", browserName, remote);
		    var capabilities = new DesiredCapabilities(browserName, string.Empty, new Platform(PlatformType.Any));
		    var remoteUri    = new Uri(remote); // TODO add /wd/hub here so it doesn't have to be used everyplace else
		    return new RemoteWebDriver(remoteUri, capabilities);
        }

        public void Close() {
            StopSeleniumStandalone();
            webdriver.Close();
        }

        public void ResetSession() {
            throw new NotImplementedException();
        }

        public void ClickLink(string linkText) {
            Find("//a[text()='" + linkText + "']", true).Click();

            // TESTING ... wait
            System.Threading.Thread.Sleep(3000);
        }

        public void Visit(string path) {
            Console.WriteLine("WebDriver.Visit navigating to {0}{1}", Mara.AppHost, path);

            // The ChromeDriver hates life ...
            if (Browser == "chrome")
                for (var i = 0; i < 10; i ++)
                    if (TryToVisitInChrome(path))
                        break;
                    else {
                        Console.WriteLine("Chrome didn't want to visit {0} ... trying again ... ", path);
                        System.Threading.Thread.Sleep(100);
                    }   
            else
                webdriver.Navigate().GoToUrl(Mara.AppHost + path);
        }

        public void FillIn(string field, string value) {
            var id_XPath   = "id('" + field + "')";
            var name_XPath = "//*[@name='" + field + "']";

            // TODO we NEED to test this!  like CRAZY!
            //      try ID first, then name ...
            var element = Find(id_XPath);
            if (element == null)
                element = Find(name_XPath);
            if (element == null)
                throw new ElementNotFoundException(id_XPath + " OR " + name_XPath);
            else
                element.Value = value;
        }

        public void FillInFields(object fieldsAndValues) {
            throw new Exception("Not implemented yet ... sad!");
        }

        public string Body {
            get { return webdriver.PageSource; }
        }

        public string CurrentUrl {
            get { return webdriver.Url; }
        }

        public string CurrentPath {
            get { return webdriver.Url.Replace(Mara.Server.AppHost, ""); } // FIXME Don't use a Global Mara.Server
        }

        public object ExecuteScript(string script) {
            throw new Exception("Not implemented yet ...");
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

        // private methods

        bool PageHasLoaded {
            get {
                if (webdriver == null) return false;
                try {
                    if (webdriver.PageSource == null) return false;
                    return true;
                } catch (NullReferenceException) {
                    Console.WriteLine("Page has not loaded ...");
                    return false; // calling driver.PageSource probably blew up (happens in the ChromeDriver)
                }   
            }   
        } 

        Process _seleniumStandalone;

        void StartSeleniumStandalone() {
            Console.WriteLine("StartSeleniumStandalone");
            _seleniumStandalone = new Process();
            _seleniumStandalone.StartInfo.FileName               = "java";
            _seleniumStandalone.StartInfo.Arguments              = string.Format("-jar {0} -port {1}", Path.GetFullPath(SeleniumServerJar), SeleniumServerPort);
            _seleniumStandalone.StartInfo.UseShellExecute        = false;
            _seleniumStandalone.StartInfo.CreateNoWindow         = true;
            if (Environment.GetEnvironmentVariable("SELENIUM_LOG") == null) // if there's no SELENIUM_LOG variable, don't print STDOUT
                _seleniumStandalone.StartInfo.RedirectStandardOutput = true;

            Console.Write("Selenium Standalone starting ... ");
            _seleniumStandalone.Start();
            Mara.WaitForLocalPortToBecomeUnavailable(SeleniumServerPort, 100, 200); // 20 seconds max ... checking every 0.1 seconds
            Console.WriteLine("done");
        }

        void StopSeleniumStandalone() {
            if (_seleniumStandalone != null) {
                Console.Write("Selenium Standalone stopping ... ");
                _seleniumStandalone.Kill();
                Mara.WaitForLocalPortToBecomeAvailable(SeleniumServerPort);
                Console.WriteLine("done");
            }
        }

        bool TryToVisitInChrome(string path) {
            webdriver.Navigate().GoToUrl(Mara.AppHost + path);
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
