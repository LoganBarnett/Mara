using System;
using System.Collections.Generic;
using Mara;
using Mara.Drivers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mara {
    public class MaraTest : IDriver {
        public static Mara MaraInstance;

        public static void InitializeMara() {
            Mara.Log("Global MaraTest.InitializeMara");
            if (MaraInstance == null) {
                MaraInstance = new Mara();
                MaraInstance.Initialize();
            }
        }

        public static void CleanupMara() {
            Mara.Log("Global MaraTest.CleanupMara");
            MaraInstance.Shutdown();
        }

        public IDriver Page { get { return MaraInstance; } }

        // Everything below here can be copy/pasted from Mara/MaraInstance.cs

        public string Body { get { return Page.Body; } }
        public string CurrentUrl { get { return Page.CurrentUrl; } }
        public string CurrentPath { get { return Page.CurrentPath; } }
        public bool JavaScriptSupported { get { return Page.JavaScriptSupported; } }

        public IElement Find(string xpath) { return Page.Find(xpath); }
        public IElement Find(string xpath, bool throwExceptionIfNotFound) { return Page.Find(xpath, throwExceptionIfNotFound); }
        public List<IElement> All(string xpath) { return Page.All(xpath); }
        public object EvaluateScript(string script) { return Page.EvaluateScript(script); }
        public string SaveAndOpenPage() { return Page.SaveAndOpenPage(); }
        public bool HasXPath(string xpath) { return Page.HasXPath(xpath); }
        public bool HasContent(string text) { return Page.HasContent(text); }

        public void Refresh() { Page.Refresh(); }
        public void Close() { Page.Close(); }
        public void ResetSession() { Page.ResetSession(); }
        public void Visit(string path) { Page.Visit(path); }
        public void Click(string linkOrButton) { Page.Click(linkOrButton); }
        public void ClickLink(string linkText) { Page.ClickLink(linkText); }
        public void ClickButton(string buttonValue) { Page.ClickButton(buttonValue); }
        public void FillIn(string field, string value) { Page.FillIn(field, value); }
        public void FillInFields(object fieldsAndValues) { Page.FillInFields(fieldsAndValues); }
        public void ExecuteScript(string script) { Page.ExecuteScript(script); }


        public void FillInFields(IDictionary<string, object> fieldsAndValues)
        {
            Page.FillInFields(fieldsAndValues);
        }
    }
}
