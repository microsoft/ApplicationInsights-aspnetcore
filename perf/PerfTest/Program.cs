using System;
using System.Diagnostics;
using System.Net.Http;

namespace PerfTest
{
    class Program
    {
        const double TestDuration = 60000;
        const int TargetRps = 50;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            PerfMeasurements perfMeasurements1 = MeasureApp("..\\..\\artifacts\\perf\\App1\\netcoreapp2.0\\App1.dll");

            PerfMeasurements perfMeasurements2 = MeasureApp("..\\..\\artifacts\\perf\\App2\\netcoreapp2.0\\App2.dll");

            double overhead = ((perfMeasurements1.rpsPerCpu - perfMeasurements2.rpsPerCpu) / perfMeasurements2.rpsPerCpu) * 100;

            string[] contents = { string.Format("\n\n {0}  Overhead: {9} BaseDuration:{1} BaseRPS:{2} BaseAvgCPU:{3} BaseRpsPerCpu:{4} SDKDuration:{5} SDKRPS:{6} SDKAvgCPU:{7} SDKRpsPerCpu:{8}",
                DateTime.UtcNow.ToString(), 
                perfMeasurements1.durationInSecs,
                perfMeasurements1.rps,
                perfMeasurements1.cpuAverage,
                perfMeasurements1.rpsPerCpu,
                perfMeasurements2.durationInSecs,
                perfMeasurements2.rps,
                perfMeasurements2.cpuAverage,
                perfMeasurements2.rpsPerCpu,
                overhead) };

            System.IO.File.AppendAllLines(@"c:\perftests\cijo.txt", contents);

            Console.WriteLine(contents);
        }

        private static PerfMeasurements MeasureApp(string pathToApp)
        {
            // Launch App
            Process app = CommandLineHelpers.ExecuteCommand("dotnet", pathToApp, true);
            app.ProcessorAffinity = (IntPtr)12;
            app.PriorityClass = ProcessPriorityClass.High;

            //Verify App
            HttpClient client = new HttpClient();
            var responsefromApp = client.GetStringAsync("http://localhost:5000/api/values").Result;

            // Launch Load Generator
            Process loadGenProcess = CommandLineHelpers.ExecuteCommand("dotnet",
                string.Format("..\\..\\artifacts\\perf\\LoadGenerator\\netcoreapp2.0\\LoadGenerator.dll http://localhost:5000/api/values {0} {1}",
                TargetRps, TestDuration));
            loadGenProcess.ProcessorAffinity = (IntPtr)3;
            loadGenProcess.PriorityClass = ProcessPriorityClass.Normal;

            // Launch perf counter reader
            Process MeasureCounterProcess = CommandLineHelpers.ExecuteCommand("powershell",
            ".\\ReadCounter.ps1");
            string avgCpu = MeasureCounterProcess.StandardOutput.ReadToEnd();            
            MeasureCounterProcess.WaitForExit();


            string requCount = loadGenProcess.StandardOutput.ReadToEnd();            
            loadGenProcess.WaitForExit();

            double totalRequests = Math.Round(double.Parse(requCount), 2);
            double cpuAverage = Math.Round(double.Parse(avgCpu), 2);
            cpuAverage = Math.Round(cpuAverage / 2, 2);
            double durationInSecs = Math.Round(TestDuration / 1000, 2);
            double rps = Math.Round(totalRequests / durationInSecs, 2);

            double rpsPerCpu = Math.Round(rps / cpuAverage, 2);
           
            app.Kill();

            return new PerfMeasurements()
            {
                durationInSecs = durationInSecs,
                rps = rps,
                cpuAverage = cpuAverage,
                rpsPerCpu = rpsPerCpu
            };
        }
    }
}
