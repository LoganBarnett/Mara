using System;

namespace Mara.Servers {

    /*
     * Optional baseclass for IServer implementations
     *
     * If you use this implementation, your App, Host, and Port 
     * properties will lazily load default values from Mana's 
     * static App, Host, and Port properties
     *
     * You also get a default implementation of AppHost
     *
     * You still must implement Start() and Stop()
     */
    public abstract class Server {

        string _app;
        public string App  {
            get {
                if (_app == null) _app = Mara.App;
                return _app;
            }
            set { _app = value; }
        }

        string _host;
        public string Host  {
            get {
                if (_host == null) _host = Mara.Host;
                return _host;
            }
            set { _host = value; }
        }

        int _port = -1;
        public int Port  {
            get {
                if (_port == -1) _port = Mara.Port;
                return _port;
            }
            set { _port = value; }
        }

        public string AppHost {
            get { return string.Format("http://{0}:{1}", Host, Port); }
        }
    }
}
