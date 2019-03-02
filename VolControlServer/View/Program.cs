using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Interops;
using VolControlServer.Model;

namespace VolControlServer
{
    class Program
    {
        static Connector connector;

        public static void StartOnNewThread()
        {
            Console.WriteLine("Child thread starts");
            connector.SetupSocket();
            connector.Run();
        }

        static void Main(string[] args)
        {
            connector = new Connector();
            Console.WriteLine("Ip address:");
            string ipString = Console.ReadLine();
            connector.SetIpAddress(ipString);

            ThreadStart childref = new ThreadStart(StartOnNewThread);
            Thread childThread = new Thread(childref);
            childThread.Start();

            bool toQuit = false;
            while(!toQuit)
            {
                Console.WriteLine("type q to quit");
                string tmp = Console.ReadLine();
                if (tmp == "q")
                {
                    toQuit = true;
                }
            }
            childThread.Abort();
        }

    }
}
