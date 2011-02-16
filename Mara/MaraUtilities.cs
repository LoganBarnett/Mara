using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Mara {

    /*
     * This contains the static utility methods that might be useful to 
     * verious different Mara.* libraries and we feel are important enough 
     * to provide globally as Mara.SomeUtilityMethod()
     *
     * This is your typical "bucket" of utilities.
     *
     */
    public partial class Mara {

        public static void WaitForLocalPortToBecomeUnavailable(int port, int sleepTimeInMilliseconds = 100, int timesToCheck = 100) {
            for (var i = 0; i < timesToCheck; i++) {
                bool portIsAvailable = LocalPortIsAvailable(port);
                if (portIsAvailable == false)
                    return; // we're done waiting, the port is not available
                else
                    Thread.Sleep(sleepTimeInMilliseconds); // let's keep waiting
            }
            throw new Exception(string.Format("Tried waiting for local port {0} to become unavailable for {1} seconds, but it's still available",
                port, (sleepTimeInMilliseconds * timesToCheck / 1000.0)));
        }

        public static void WaitForLocalPortToBecomeAvailable(int port, int sleepTimeInMilliseconds = 100, int timesToCheck = 100) {
            for (var i = 0; i < timesToCheck; i++) {
                bool portIsAvailable = LocalPortIsAvailable(port);
                if (portIsAvailable == true)
                    return; // we're done waiting, the port is available
                else
                    Thread.Sleep(sleepTimeInMilliseconds); // let's keep waiting
            }
            throw new Exception(string.Format("Tried waiting for local port {0} to become available for {1} seconds, but it's still unavailable",
                port, (sleepTimeInMilliseconds * timesToCheck / 1000.0)));
        }

        public static bool LocalPortIsAvailable(int port) {
            var localhost = (IPAddress) Dns.GetHostAddresses("localhost")[0];

            try {
                var sock = new Socket(localhost.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                sock.Connect(localhost, port);
                if (sock.Connected == true)  // Port is in use and connection is successful
                    return false;
                else
                    throw new Exception("Not connected to port ... but no Exception was thrown?");
            } catch (SocketException ex) {
                if (ex.ErrorCode == 10061)  // Port is unused and could not establish connection 
                    return true;
                else
                    throw ex;
            }
        }

    }
}