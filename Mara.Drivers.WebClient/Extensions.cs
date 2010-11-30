// THIS IS COPY/PASTED FROM WEBDRIVER DRIVER ........ REFACTOR! TODO
using System;
using System.Reflection;
using System.Collections.Generic;

namespace Mara.Drivers {
    public static class WebClientExtensions {

        // Convert an anonymous object to a Dictionary
        public static IDictionary<string, object> ToDictionary(this object anonymousType) {
            var attr = BindingFlags.Public | BindingFlags.Instance;
            var dict = new Dictionary<string, object>();
            foreach (var property in anonymousType.GetType().GetProperties(attr))
                if (property.CanRead)
                    dict.Add(property.Name, property.GetValue(anonymousType, null));
            return dict;
        }   
    }
}
