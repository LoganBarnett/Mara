using System;
using System.Collections.Generic;

namespace Mara {

    /*
     * Represents an element on the page
     */
    public interface IElement {
        void Click();
        string Value { get; set; }
        string Text  { get; }
        string this[string attributeName] { get; }
    }

}
