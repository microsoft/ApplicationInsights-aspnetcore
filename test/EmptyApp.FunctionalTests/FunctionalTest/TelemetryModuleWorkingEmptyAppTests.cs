namespace EmptyApp.FunctionalTests.FunctionalTest
{
    using FunctionalTestUtils;
    using Xunit;

    public class TelemetryModuleWorkingEmptyAppTests : TelemetryTestsBase
    {
        private const string assemblyName = "EmptyApp.FunctionalTests";

        // The NET46 conditional check is wrapped inside the test to make the tests visible in the test explorer. We can move them to the class level once if the issue is resolved.

        [Fact]
        public void TestBasicDependencyPropertiesAfterRequestingBasicPage()
        {
#if NET46
            this.ValidateBasicDependency(assemblyName, "/");
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
