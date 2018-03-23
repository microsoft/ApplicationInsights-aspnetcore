using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace PerfTests
{
    [TestClass]
    public class UnitTest1
    {
        const double TestDuration = 30000;
        const int TargetRps = 50;
        static long affinityApp = 1;
        static long affinityLoadGen = 2;
        static int CpuCoresUsedByApp = 2;

        [Ignore]
        [TestMethod]        
        public void TestMethod1()
        {
            var s = Directory.GetCurrentDirectory();
            Trace.WriteLine("Current Dir:" + s);

            Trace.WriteLine("Launching App1");
            PerfMeasurements perfMeasurements1 = MeasureApp("D:\\a\\1\\s\\artifacts\\perf\\App1\\netcoreapp2.0\\publish\\App1.dll");
            PrintPerfMeasurements(perfMeasurements1);

            Trace.WriteLine("Launching App2");
            PerfMeasurements perfMeasurements2 = MeasureApp("..\\..\\..\\..\\artifacts\\perf\\App2\\netcoreapp2.0\\publish\\App2.dll");
            PrintPerfMeasurements(perfMeasurements1);

            double overhead = ((perfMeasurements1.rpsPerCpu - perfMeasurements2.rpsPerCpu) / perfMeasurements2.rpsPerCpu) * 100;

            Trace.WriteLine("Overhead is:" + overhead);
            Assert.IsTrue(overhead > 0 && overhead < 10, "Overhead should be 0-10. Value:" +overhead);

        }

        [TestMethod]
        public void TestMethod3()
        {
            var cpuCount = Environment.ProcessorCount;
            Trace.WriteLine("Current process:" + Process.GetCurrentProcess().ProcessName);
            Trace.WriteLine("CPU Count:" + cpuCount);
            if(cpuCount == 2)
            {
                affinityApp = 1;
                affinityLoadGen = 2;
                CpuCoresUsedByApp = 1;
            }
            else if (cpuCount >=4)
            {
                affinityApp = 12;
                affinityLoadGen = 3;
                CpuCoresUsedByApp = 2;
            }

            Trace.WriteLine("Launching App1");
            PerfMeasurements perfMeasurements1 = MeasureApp2($"..\\..\\..\\..\\artifacts\\perf\\App1\\netcoreapp2.0\\App1.dll");
            PrintPerfMeasurements(perfMeasurements1);

            Trace.WriteLine("Launching App2");
            PerfMeasurements perfMeasurements2 = MeasureApp2($"..\\..\\..\\..\\artifacts\\perf\\App2\\netcoreapp2.0\\App2.dll");
            PrintPerfMeasurements(perfMeasurements2);

            double overhead = ((perfMeasurements1.rpsPerCpu - perfMeasurements2.rpsPerCpu) / perfMeasurements2.rpsPerCpu) * 100;

            Trace.WriteLine("Overhead is:" + overhead);
            Assert.IsTrue(overhead > 0 && overhead < 10, "Overhead should be 0-10. Value:" + overhead);
        }

        private static void PrintPerfMeasurements(PerfMeasurements perfMeasurements)
        {
            Trace.WriteLine("Rps:" + perfMeasurements.rps);
            Trace.WriteLine("Cpu:" + perfMeasurements.cpuAverage);
            Trace.WriteLine("RpsPerCpu:" + perfMeasurements.rpsPerCpu);
        }

        private static PerfMeasurements MeasureApp2(string pathToApp)
        {
            // Launch App           
            string output = "";
            string error = "";

            var app = new DotNetCoreProcess(pathToApp)
                .RedirectStandardOutputTo((string outputMessage) =>
                {
                    //output += outputMessage;                    
                })
                .RedirectStandardErrorTo((string errorMessage) =>
                {
                    error += errorMessage;                    
                })
                .Start(affinityApp, ProcessPriorityClass.High);
           
            //Verify App
            try
            {
                HttpClient client = new HttpClient();
                var responsefromApp = client.GetStringAsync("http://localhost:5000/api/values").Result;
                Trace.WriteLine("App output http req:" + responsefromApp);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception while hitting app url: " + ex.Message);
            }

            // Launch Load Generator
            Process loadGenProcess = CommandLineHelpers.ExecuteCommand("dotnet",
                string.Format("..\\..\\..\\..\\artifacts\\perf\\LoadGenerator\\netcoreapp2.0\\LoadGenerator.dll http://localhost:5000/api/values {0} {1}",
                TargetRps, TestDuration));
            loadGenProcess.ProcessorAffinity = (IntPtr) affinityLoadGen;
            loadGenProcess.PriorityClass = ProcessPriorityClass.Normal;
            //Trace.WriteLine("ProcessId (loadgen):" + loadGenProcess.Id);

            // Launch perf counter reader
            Process MeasureCounterProcess = CommandLineHelpers.ExecuteCommand("powershell",
            ".\\ReadCounter.ps1");
            string avgCpu = MeasureCounterProcess.StandardOutput.ReadToEnd();
            MeasureCounterProcess.WaitForExit();
            Trace.WriteLine("AvgCpu:" + avgCpu);

            string requCount = loadGenProcess.StandardOutput.ReadToEnd();
            loadGenProcess.WaitForExit();
            Trace.WriteLine("Total requests:" + requCount);

            double totalRequests = Math.Round(double.Parse(requCount), 2);
            double cpuAverage = Math.Round(double.Parse(avgCpu), 2);
            cpuAverage = Math.Round(cpuAverage / CpuCoresUsedByApp, 2);
            double durationInSecs = Math.Round(TestDuration / 1000, 2);
            double rps = Math.Round(totalRequests / durationInSecs, 2);

            double rpsPerCpu = Math.Round(rps / cpuAverage, 2);

            if (!string.IsNullOrEmpty(output))
            {
                Trace.WriteLine("Output:" + output);
            }
            if (!string.IsNullOrEmpty(error))
            {
                Trace.WriteLine("Error:" + error);
            }
            app.Kill();
            Thread.Sleep(1000);            

            return new PerfMeasurements()
            {
                durationInSecs = durationInSecs,
                rps = rps,
                cpuAverage = cpuAverage,
                rpsPerCpu = rpsPerCpu
            };
        }
        private static PerfMeasurements MeasureApp(string pathToApp)
        {
            // Launch App
            Process app = CommandLineHelpers.ExecuteCommand("dotnet", pathToApp, true);
            app.ProcessorAffinity = (IntPtr)12;
            app.PriorityClass = ProcessPriorityClass.High;
            Trace.WriteLine("ProcessId:" + app.Id);

            //Verify App
            HttpClient client = new HttpClient();
            var responsefromApp = client.GetStringAsync("http://localhost:5000/api/values").Result;

            // Launch Load Generator
            Process loadGenProcess = CommandLineHelpers.ExecuteCommand("dotnet",
                string.Format("..\\..\\..\\..\\artifacts\\perf\\LoadGenerator\\netcoreapp2.0\\LoadGenerator.dll http://localhost:5000/api/values {0} {1}",
                TargetRps, TestDuration));
            loadGenProcess.ProcessorAffinity = (IntPtr)3;
            loadGenProcess.PriorityClass = ProcessPriorityClass.Normal;
            Trace.WriteLine("ProcessId (loadgen):" + loadGenProcess.Id);

            // Launch perf counter reader
            Process MeasureCounterProcess = CommandLineHelpers.ExecuteCommand("powershell",
            ".\\ReadCounter.ps1");
            string avgCpu = MeasureCounterProcess.StandardOutput.ReadToEnd();
            MeasureCounterProcess.WaitForExit();
            Trace.WriteLine("AvgCpu:" + avgCpu);


            string requCount = loadGenProcess.StandardOutput.ReadToEnd();
            loadGenProcess.WaitForExit();
            Trace.WriteLine("Total requests:" + requCount);

            double totalRequests = Math.Round(double.Parse(requCount), 2);
            double cpuAverage = Math.Round(double.Parse(avgCpu), 2);
            cpuAverage = Math.Round(cpuAverage / 2, 2);
            double durationInSecs = Math.Round(TestDuration / 1000, 2);
            double rps = Math.Round(totalRequests / durationInSecs, 2);

            double rpsPerCpu = Math.Round(rps / cpuAverage, 2);

            app.Kill();            
            Thread.Sleep(1000);
            Trace.WriteLine(app.Id + " existed? :" + app.HasExited);

            return new PerfMeasurements()
            {
                durationInSecs = durationInSecs,
                rps = rps,
                cpuAverage = cpuAverage,
                rpsPerCpu = rpsPerCpu
            };
        }
    }




    internal class DotNetCoreProcess
    {
        private readonly Process process;

        public DotNetCoreProcess(string arguments, string workingDirectory = null)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("dotnet.exe", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,               
            };          

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            process = new Process()
            {
                StartInfo = startInfo
            };

        
        }

        /// <summary>
        /// Get the exit code for this process, if the process has exited. If the process hasn't exited, then return null.
        /// </summary>
        public int? ExitCode
        {
            get
            {
                int? result = null;
                if (process.HasExited)
                {
                    //Trace.WriteLine("process exited.");
                    result = process.ExitCode;
                }
                return result;
            }
        }

        /// <summary>
        /// Redirect all of the standard output text to the provided standardOutputHandler.
        /// </summary>
        /// <param name="standardOutputHandler">An action that will be invoked whenever the process writes to its standard output stream.</param>
        /// <returns></returns>
        public DotNetCoreProcess RedirectStandardOutputTo(Action<string> standardOutputHandler)
        {
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null)
                {
                    standardOutputHandler.Invoke(e.Data);
                }
            };
            return this;
        }

        /// <summary>
        /// Redirect all of the standard error text to the provided standardErrorHandler.
        /// </summary>
        /// <param name="standardErrorHandler">An action that will be invoked whenever the process writes to its standard error stream.</param>
        /// <returns></returns>
        public DotNetCoreProcess RedirectStandardErrorTo(Action<string> standardErrorHandler)
        {
            process.StartInfo.RedirectStandardError = true;
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null)
                {
                    standardErrorHandler.Invoke(e.Data);
                }
            };
            return this;
        }

        /// <summary>
        /// Run this process and wait for it to finish.
        /// </summary>
        public DotNetCoreProcess Run()
        {
            //Start();
            WaitForExit();
            return this;
        }

        /// <summary>
        /// Asynchronously start this process. This method will not wait for
        /// the process to finish before it returns.
        /// </summary>
        public DotNetCoreProcess Start(long affinity, ProcessPriorityClass prio)
        {
            //Trace.WriteLine("Process starting...");
            process.Start();
            process.PriorityClass = prio;
            //process.ProcessorAffinity = affinity;

            long AffinityMask = (long)process.ProcessorAffinity;
            Trace.WriteLine("Process Affinity before is:" + AffinityMask);
            AffinityMask &= affinity;
            Trace.WriteLine("Process Affinity after is:" + AffinityMask);
            process.ProcessorAffinity = (IntPtr)AffinityMask;
            Trace.WriteLine("Process Affinity after setting is:" + process.ProcessorAffinity);

            //Trace.WriteLine("Process started with pid:" + process.Id);

            if (process.StartInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
            }
            if (process.StartInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
            }

            return this;
        }

        /// <summary>
        /// Set affinity for process
        /// </summary>
        public void SetAffinity(IntPtr affinityMask)
        {
            process.ProcessorAffinity = (IntPtr)affinityMask;
        }

        /// <summary>
        /// Set affinity for process
        /// </summary>
        public void SetPriority(ProcessPriorityClass priorityClass)
        {
            process.PriorityClass = priorityClass;
        }

        /// <summary>
        /// Wait up to 1 seconds for this process to exit.
        /// </summary>
        public void WaitForExit()
        {
            WaitForExit(TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Block for the provided amount of time or until this started process has exited.
        /// </summary>
        public void WaitForExit(TimeSpan timeout)
        {
            process.WaitForExit((int)timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Terminate the process.
        /// </summary>
        public void Kill()
        {
            process.CancelErrorRead();
            process.CancelOutputRead();
            process.Kill();

            // Kill is an async operation internally.
            //(see https://msdn.microsoft.com/en-us/library/system.diagnostics.process.kill(v=vs.110).aspx#Anchor_2)
            WaitForExit();
        }

        /// <summary>
        /// Get the path to the dotnet.exe file. This will search the current working directory,
        /// the directory specified in the NetCorePath environment variable, and each of the
        /// directories specified in the Path environment variable. If the dotnet.exe file still
        /// can't be found, then this will return null.
        /// </summary>
        public static string DotNetExePath
        {
            get
            {
                if (dotnetExePath == null)
                {
                    List<string> envPaths = new List<string>();
                    envPaths.Add(@".\");
                    envPaths.Add(Environment.GetEnvironmentVariable(NetCorePathEnvVariableName));
                    envPaths.AddRange(Environment.GetEnvironmentVariable(PathEnvVariableName).Split(';'));

                    foreach (string envPath in envPaths)
                    {
                        if (!string.IsNullOrWhiteSpace(envPath))
                        {
                            string tempDotNetExePath = envPath;
                            if (!tempDotNetExePath.EndsWith(dotnetExe, StringComparison.InvariantCultureIgnoreCase))
                            {
                                tempDotNetExePath = Path.Combine(tempDotNetExePath, dotnetExe);
                            }

                            if (File.Exists(tempDotNetExePath))
                            {
                                dotnetExePath = tempDotNetExePath;
                                break;
                            }
                        }
                    }
                }

                Trace.WriteLine("Dotnet.exe path: " + dotnetExePath);
                return dotnetExePath;
            }
        }

        /// <summary>
        /// Check whether or not the dotnet.exe file exists at its expected path.
        /// </summary>
        /// <returns></returns>
        public static bool HasDotNetExe()
        {
            return !string.IsNullOrEmpty(DotNetExePath);
        }

        private static string dotnetExePath;

        private const string dotnetExe = "dotnet.exe";
        private const string NetCorePathEnvVariableName = "NetCorePath";
        private const string PathEnvVariableName = "Path";
    }
}
