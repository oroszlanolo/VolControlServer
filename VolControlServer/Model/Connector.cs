using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace VolControlServer.Model
{
    public class Connector
    {
        private const int FIXED_NUMBER_LEN = 5;

        private IPAddress ipAddress;
        private int port = 8765;
        private Socket listener;
        private Socket sck;

        private VolumeController volControl;

        private bool stop = false;

        public void SetIpAddress(string ip)
        {
            ipAddress = IPAddress.Parse(ip);
        }

        public int Port
        {
            get { return Port; }
            set { Port = value; }
        }

        public void Stop()
        {
            stop = true;
        }

        public Connector()
        {
            volControl = new VolumeController();
        }

        public bool SetupSocket(AddressFamily addrFamily = AddressFamily.InterNetwork, SocketType sockType = SocketType.Stream, ProtocolType protType = ProtocolType.Tcp)
        {
            if (ipAddress == null)
                return false;

            listener = new Socket(addrFamily, sockType, protType);
            listener.Bind(new IPEndPoint(ipAddress, port));
            Console.WriteLine("Socket is set up");
            return true;
        }

        /// <summary>
        /// Starts listening on the set up IP and Port.
        /// This is a locking function, call this on a new thread!
        /// </summary>
        public void Run()
        {

            listener.Listen(1);
            Console.WriteLine("Listening on port " + port.ToString());
            sck = listener.Accept();
            Console.WriteLine("Connected!");

            while (!stop)
            {
                byte[] data = new byte[1];
                sck.Receive(data, 1, SocketFlags.None);
                string resp = Encoding.ASCII.GetString(data);

                switch (resp)
                {
                    case "r":
                        {
                            Send();
                            break;
                        }
                    case "s":
                        {
                            Receive();
                            break;
                        }
                    default:
                        {
                            throw new Exception("Something went fckin' wrong!");
                        }
                }
            }
        }

        private void Send()
        {
            volControl.Refresh();
            sck.Send(Encoding.ASCII.GetBytes(GetFixedNumberString(volControl.Count())));

            for (int j = 0; j < volControl.Count(); j++)
            {
                byte[] datatosend = Encoding.ASCII.GetBytes(volControl.GetName(j));
                sck.Send(Encoding.ASCII.GetBytes(GetFixedNumberString(datatosend.Length)));
                sck.Send(datatosend); //Name sent

                string volToSend = ((int)(volControl.GetVolume(j) * 100)).ToString();
                datatosend = Encoding.ASCII.GetBytes(volToSend);
                sck.Send(Encoding.ASCII.GetBytes(GetFixedNumberString(datatosend.Length)));
                sck.Send(datatosend); //Volume sent
            }

        }
        private void Receive()
        {
            byte[] data = new byte[FIXED_NUMBER_LEN];
            sck.Receive(data, FIXED_NUMBER_LEN, SocketFlags.None);
            int len = int.Parse(Encoding.ASCII.GetString(data));
            data = new byte[len];
            sck.Receive(data, len, SocketFlags.None);
            string name = Encoding.ASCII.GetString(data);

            data = new byte[FIXED_NUMBER_LEN];
            sck.Receive(data, FIXED_NUMBER_LEN, SocketFlags.None);
            int vol = int.Parse(Encoding.ASCII.GetString(data));

            volControl.SetVolume(name, (float)(vol * 1.0 / 100));

        }
        
        static string GetFixedNumberString(int number)
        {
            return number.ToString().PadLeft(FIXED_NUMBER_LEN, '0').Substring(0, FIXED_NUMBER_LEN);
        }

    }
}
