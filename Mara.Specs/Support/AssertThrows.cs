using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NUnit.Framework;

namespace Mara {

    // Usage:
    //
    //   this.AssertThrows(...)
    //
    public static class AssertThrowsExtension {

        // AssertThrows<SpecialException>(() => { ... })
        public static void AssertThrows<T>(this object o, Action action) {
            o.AssertThrows<T>(action);
        }

        // AssertThrows<SpecialException>("BOOM!", () => { ... })
        public static void AssertThrows<T>(this object o, string messagePart, Action action) {
            o.AssertThrows(action, messagePart, typeof(T));
        }

        // AssertThrows("BOOM!", () => { ... })
        public static void AssertThrows(this object o, string messagePart, Action action) {
            o.AssertThrows(action, messagePart);
        }

        // AssertThrows(() => { ... })
        // AssertThrows(() => { ... }, "BOOM!")                           // <--- AssertThrows(Message, Action) is preferred
        // AssertThrows(() => { ... }, "BOOM!", typeof(SpecialException)) // <--- AssertThrows<T>(Message) is preferred
        public static void AssertThrows(this object o, Action action, string messagePart = null, Type exceptionType = null) {
            try {
                action.Invoke();
                Assert.Fail("Expected Exception to be thrown, but none was.");
            } catch (Exception ex) {
                // check exception type, if provided
                if (exceptionType != null)
                    if (!exceptionType.IsAssignableFrom(ex.GetType()))
                        Assert.Fail("Expected Exception of type {0} to be thrown, but got an Exception of type {1}", exceptionType, ex.GetType());

                // check exception message part, if provided
                if (messagePart != null)
                    if (! ex.Message.Contains(messagePart))
                        Assert.Fail("Expected {0} Exception to be thrown with a message containing {1}, but message was: {2}",
                            exceptionType, messagePart, ex.Message);
            }
        }
    }
}
