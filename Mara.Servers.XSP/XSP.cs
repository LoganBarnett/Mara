using System;
using System.Net;
using System.Net.Sockets;
using Mono.WebServer;
using Mara;

namespace Mara.Servers {

    /*
     * ...
     * ...
     */
    public class XSP : Server, IServer {
        ApplicationServer _server;
        bool _started = false;

        public XSP() { Mara.Log("Constructed a new XSP server"); }

        public void Start() {
            if (_started == true) return;
            _started = true;

            Mara.Log("XSP.Start()");
            _server = new ApplicationServer(new XSPWebSource(IPAddress.Any, Port));
			_server.AddApplicationsFromCommandLine(string.Format("{0}:/:{1}", Port, App));

            Mara.Log("XSP2 starting ... ");
            try {
                _server.Start(true);
            } catch (SocketException ex) {
                // it gets mad sometimes?
                Mara.Log("SocketException while starting XSP: {0}", ex.Message);
            }
            Mara.WaitForLocalPortToBecomeUnavailable(Port);
            Mara.Log("done");
        }

        public void Stop() {
            if (_server == null || _started == false) return;

            Mara.Log("XSP2 stopping ... ");
            try {
                _server.Stop();
                Mara.Log("done");
            } catch (InvalidOperationException ex) {
                if (ex.Message == "The server is not started.")
                    return; // this happens a lot? why ...
                else
                    throw ex;
            }
            // Mara.WaitForLocalPortToBecomeAvailable(Port); // meh, just kill this process ... it doesn't like to stop ...
        }
    }
}
