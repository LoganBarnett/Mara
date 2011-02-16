using System;
using System.Collections.Generic;

namespace Mara.Drivers {

    /*
     * All Mara drivers must implement Mara.IDriver
     * 
     * This interface has the Visit(), CurrentPath(), FillIn(), etc methods
     */
    public interface IDriver {

        string Body                { get; }
        string CurrentUrl          { get; }
        string CurrentPath         { get; }
        bool   JavaScriptSupported { get; }

        void Refresh();
        void Close();
        void ResetSession();
        void Visit(string path);
        void Click(string linkOrButton);
        void ClickLink(string linkText);
        void ClickButton(string buttonValue);
        void FillIn(string field, string value);
        void FillInFields(object fieldsAndValues);
        void FillInFields(IDictionary<string, object> fieldsAndValues);
		void Check(string checkbox);
		void Uncheck(string checkbox);
		void Select(string option, string select);

        IElement Find(string xpath);
        IElement Find(string xpath, bool throwExceptionIfNotFound);

        // TODO Instead of returning a simple List<IElement>, All should return something that you can chain additional finders on
        List<IElement> All(string xpath);

        void   ExecuteScript(string script);
        object EvaluateScript(string script);

        string SaveAndOpenPage();

        bool HasXPath(string xpath);
        bool HasContent(string text);
    }
}
