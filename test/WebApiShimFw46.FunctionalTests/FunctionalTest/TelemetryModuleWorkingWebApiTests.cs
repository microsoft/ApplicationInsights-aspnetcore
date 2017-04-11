using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace SampleWebAppIntegration.FunctionalTest
{
    using FunctionalTestUtils;

    public class TelemetryModuleWorkingWebApiTests : TelemetryTestsBase
    {
        private const string assemblyName = "WebApiShimFw46.FunctionalTests";

        // The NET46 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
#if NET46
            this.ValidateBasicDependency(assemblyName, "/api/values");
#endif
        }

        [Fact]
        public void TestIfPerformanceCountersAreCollected()
        {
#if NET46
            ValidatePerformanceCountersAreCollected(assemblyName);
#endif
        }
    }
}
