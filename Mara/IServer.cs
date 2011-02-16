using System;

namespace Mara.Servers {

    /*
     * All servers that are used to boot up an ASP.NET web application 
     * for us to execute tests against it must implement Mara.IServer
     *
     * This is used for booting up things like Cassini, XSP, and hopefully eventually IIS Express
     *
     * It could even be used to setup the application on IIS on a real server, if desired!
     */
    public interface IServer {

        // TODO ? add bool Started or bool IsRunning or something?

        void Start();
        void Stop();
        string App     { get; set; }
        int    Port    { get; set; }
        string Host    { get; set; }
        string AppHost { get; }
    }
}
