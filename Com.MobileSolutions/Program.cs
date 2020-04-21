using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;

namespace Com.MobileSolutions
{
    public static class Program
    {
        private static System.Timers.Timer aTimer;

        public static void Main()
        { 
            SetTimer();

            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
            Console.ReadLine();
            aTimer.Stop();
            aTimer.Dispose();

            Console.WriteLine("Terminating the application...");
        }

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(5000);
            // Hook up the Elapsed event for the timer. 
            Server server = new Server();
            aTimer.Elapsed += server.OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }



    }


}

