using System;
using System.IO;
using System.Reflection;
using Mara.Drivers;
using Mara.Servers;

namespace Mara {

    /*
     * This contains the static methods for Mara
     *
     * This offers the main API for working with Mara configuration 
     */
    public partial class Mara {

        public static int    Port = 8090;
        public static string Host = "localhost";

        static string _defaultDriverName = null;
        public static string DefaultDriverName {
            get { return _defaultDriverName ?? Environment.GetEnvironmentVariable("DRIVER_NAME") ?? "Mara.Drivers.WebClient"; }
            set { _defaultDriverName = value; }
        }

        static string _defaultServerName;
        public static string DefaultServerName {
            get {
                if (_defaultServerName != null)
                    return _defaultServerName;

                if (Type.GetType("Mono.Runtime") != null)
                    return "Mara.Servers.XSP";
                else
                    return "Mara.Servers.Cassini";
            }
            set { _defaultServerName = value; }
        }

        static string  _app;
        static string  _appHost;
        static IDriver _driver;
        static IServer _server;

        // The path to an ASP.NET (MVC) web application that Mara.Server 
        // will boot up if Mara.RunServer isn't set to false
        public static string App {
            get {
                if (_app == null) return TryToFindWebAppDirectory();
                else              return _app;
            }
            set {
                if (value != null) _app = Path.GetFullPath(value);
                else               _app = null;
            }
        }

        static bool _runServer = true;
        public static bool RunServer {
            get {
                if (Environment.GetEnvironmentVariable("RUN_SERVER") != null)
                    return bool.Parse(Environment.GetEnvironmentVariable("RUN_SERVER"));
                else
                    return _runServer;
            }
            set { _runServer = value; }
        }

        public static IServer Server {
            get {
                if (_server == null) _server = InstantiateDefaultServer();
                return _server;
            }
            set {
                if (_server != null) _server.Stop();
                _server = value;
            }
        }

        public static string AppHost {
            get {
                // TODO test ...
                if (Environment.GetEnvironmentVariable("APP_HOST") != null)
                    return Environment.GetEnvironmentVariable("APP_HOST");
                if (_appHost != null) return _appHost;
                else                  return (Server == null) ? null : Server.AppHost;
            }
            set { _appHost = value; }
        }

        public static IDriver Driver {
            get {
                if (_driver == null) _driver = InstantiateDefaultDriver();
                return _driver;
            }
            set {
                if (_driver != null) _driver.Close();
                _driver = value;
            }
        }

        public static void Log(string message, params object[] args) {
            // TODO document and test MARA_LOG variable
            if (Environment.GetEnvironmentVariable("MARA_LOG") != null)
                Console.WriteLine(message, args);
        }

        // Private Helper Methods

        static string TryToFindWebAppDirectory() {
            DirectoryInfo parent = null;
            string        match  = null;
            var           dir    = Directory.GetCurrentDirectory();

            while (match == null && dir != null) {
                match  = FindDirectoryThatHasWebConfig(dir, true);
                parent = Directory.GetParent(dir);

                if (parent == null)
                    dir = null;
                else
                    dir = parent.FullName;
            }

            return match;
        }

        static string FindDirectoryThatHasWebConfig(string rootDirectoryToLookIn, bool lookInSubdirectories) {
            // if this directory has a Web.config, return it
            if (File.Exists(Path.Combine(rootDirectoryToLookIn, "Web.config")))
                return rootDirectoryToLookIn;
            
            // if any of the directories IN this directory have a Web.config, return it
            if (lookInSubdirectories == true) {
                foreach (var dir in Directory.GetDirectories(rootDirectoryToLookIn)) {
                    var result = FindDirectoryThatHasWebConfig(dir, false); // if this has the Web.config, it won't be null
                    if (result != null)
                        return result;
                }
            }
            
            // else return null, meaning that we couldn't find it
            return null;
        }

        static IDriver InstantiateDefaultDriver() {
            Mara.Log("InstantiateDefaultDriver");
            Type driverType = GetTypeFromWhereverWeCan(Mara.DefaultDriverName);
            if (driverType != null)
                return Activator.CreateInstance(driverType) as IDriver;
            // TODO set some default properties on the driver?

            throw new Exception("Failed to instantiate Mana Default Driver: " + Mara.DefaultDriverName + 
                                ".  Please set Mana.Driver manually or ensure that " + Mara.DefaultDriverName + ".dll is in the current directory");
        }

        static IServer InstantiateDefaultServer() {
            Mara.Log("InstantiateDefaultServer");
            Type serverType = GetTypeFromWhereverWeCan(Mara.DefaultServerName);
            if (serverType != null)
                return Activator.CreateInstance(serverType) as IServer;
            // TODO set some default properties on the server?

            throw new Exception("Failed to instantiate Mana Default Server: " + Mara.DefaultServerName + 
                                ".  Please set Mana.Server manually or ensure that " + Mara.DefaultServerName + ".dll is in the current directory");
        }

        static Type GetTypeFromWhereverWeCan(string typeFullName) {
            // Try finding the type in this assembly
            Type type = Type.GetType(typeFullName);
            if (type != null) return type;

            // Try finding the type in any of our references assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                type = assembly.GetType(typeFullName);
                if (type != null) return type;
            }

            // Try loading up a DLL with the same name as the type and finding the type in there
            var dll = Path.GetFullPath(typeFullName + ".dll");
            if (File.Exists(dll)) {
                type = Assembly.LoadFile(dll).GetType(typeFullName);
                if (type != null) return type;
            }

            return null;
        }
    }
}
