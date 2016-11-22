using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Linq;

namespace reset
{
    class Program
    {
        static string MONO_VID = "04B4";
        static string MONO_PID = "F232";

        static bool TestSerial(string comPortName, string VID, string PID)
        {
            var pattern = String.Format("VID_{0}.PID_{1}", VID, PID);
            var rgx = new Regex (pattern, RegexOptions.IgnoreCase);

            var rk1 = Registry.LocalMachine;
            var rk2 = rk1.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum");
            foreach(var s3 in rk2.GetSubKeyNames())
            {
                var rk3 = rk2.OpenSubKey(s3);

                foreach(var s in rk3.GetSubKeyNames())
                {
                    if (rgx.IsMatch(s))
                    {
                        var rk4 = rk3.OpenSubKey(s);
                        foreach(var s2 in rk4.GetSubKeyNames())
                        {
                            var rk5 = rk4.OpenSubKey(s2);
                            var rk6 = rk5.OpenSubKey("Device Parameters");
                            var name = rk6.GetValue("PortName") as string;

                            if (name == comPortName)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }


        static void ResetPort(string comport)
        {
            var port = new SerialPort(comport, 9600, Parity.None, 8, StopBits.One);
                try
                {
                    port.Open();

                    if (port.DtrEnable == false)
                    {
                        port.DtrEnable = true;
                    }

                    port.DtrEnable = false;
                    port.Close();
                }
                catch(Exception)
                {
                    Console.WriteLine("failed to open: {0}", comport);
                }
        }

        static void resetAllMonos()
        {
            var ports = SerialPort.GetPortNames();
            Console.WriteLine("Found {0} port{1}", ports.Count(), ports.Count() != 1 ? "s" : "");
            foreach (var com in ports)
            {
                if (TestSerial(com, MONO_VID, MONO_PID))
                {
                    Console.WriteLine("Found mono on {0}", com);
                    ResetPort(com);
                }
                
            }
        }

        static bool findArg(string needle, string[] args)
        {
            foreach (var a in args)
            {
                if (a == needle)
                {
                    return true;
                }
            }

            return false;
        }

        static int findArgNum(string needle, string[] args)
        {
            int cnt = 0;
            foreach (var a in args)
            {
                if (a == needle)
                {
                    return cnt;
                }
                cnt++;
            }

            return -1;
        }

        static void Main(string[] args)
        {

            bool listOnly = findArg("--list", args) || findArg("-l", args);
            bool definedComPort = findArg("--port", args) || findArg("-p", args);
            bool bare = findArg("--bare", args);
            
            if (listOnly)
            {
                var ports = SerialPort.GetPortNames();
                foreach(var p in ports)
                {
                    if (TestSerial(p, MONO_VID, MONO_PID))
                    {
                        if (bare)
                            Console.WriteLine(p);
                        else
                            Console.WriteLine(" - Found Mono Serial Port at: {0}", p);
                    }
                }
            }
            else if (definedComPort)
            {
                var num = findArgNum("-p",args);
                if (num == -1)
                    num = findArgNum("--port",args);

                var comport = args[num+1];
                ResetPort(comport);
            }
            else
            {
                resetAllMonos();
            }
            

#if DEBUG
            Console.ReadKey();
#endif
        }
    }
}
