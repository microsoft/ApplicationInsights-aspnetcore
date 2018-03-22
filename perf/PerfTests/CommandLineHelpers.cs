using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PerfTests
{
    class CommandLineHelpers
    {
        public static Process ExecuteCommand(string exec, string command, bool useShellExecute = false)
        {            
            Console.WriteLine("Executing cmd command: " + command);
            ProcessStartInfo commandInfo = new ProcessStartInfo(exec, command);
            Process process = new Process { StartInfo = commandInfo };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = useShellExecute;
            if(!useShellExecute)
            {
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
            }                      
            process.Start();

            return process;
        }
    }
}
