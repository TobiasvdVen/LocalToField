using Xunit.Abstractions;

namespace LocalToField.Tests
{
    internal class TestOutputDebugLog : IDebugLog
    {
        private readonly ITestOutputHelper _testOutput;

        public TestOutputDebugLog(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        public void Log(string message)
        {
            _testOutput.WriteLine(message);
        }
    }
}
