using System;

namespace Mara {

    // Thrown when a driver cannot Find() an element that is required
    // 
    // Typically thrown by IDriver.Find(xpath, throwExceptionIfNotFound);
    public class ElementNotFoundException : Exception {
        public ElementNotFoundException(string xpath) : base("Could not find element with XPath: " + xpath) {}
    }

}
