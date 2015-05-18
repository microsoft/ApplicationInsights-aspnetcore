using Microsoft.ApplicationInsights;
using Microsoft.AspNet.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mvc6Framework45.FunctionalTests.Controllers
{
    public class UseRequestServicesController : Controller
    {
        public IActionResult Index()
        {
            TelemetryClient telemetryClient = (TelemetryClient)this.Resolver.GetService(typeof(TelemetryClient));

            telemetryClient.TrackEvent("GetMethod");
            telemetryClient.TrackMetric("GetMetric", 10);

            return View();
        }

    }
}
