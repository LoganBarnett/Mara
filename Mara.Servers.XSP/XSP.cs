﻿using System;
using System.Net;
using Mono.WebServer;
using Mara;

namespace Mara.Servers {

    /*
     * ...
     * ...
     */
    public class XSP : Server, IServer {
        ApplicationServer _server;

        public XSP() { Console.WriteLine("Constructed a new XSP server"); }

        public void Start() {
            Console.WriteLine("XSP.Start()");
            _server = new ApplicationServer(new XSPWebSource(IPAddress.Any, Port));
			_server.AddApplicationsFromCommandLine(string.Format("{0}:/:{1}", Port, App));
            _server.Start(true);

            // TODO write code that'll wait for the server to respond instead of sleeping
            Console.WriteLine("XSP2 started");
            Console.WriteLine("Waiting for XSP to response ... TODO: make this faster!");
            System.Threading.Thread.Sleep(4000);
        }

        public void Stop() {
            Console.WriteLine("XSP STOP");
            _server.Stop();
        }
    }
}
