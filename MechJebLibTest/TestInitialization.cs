using MechJebLib.Utils;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("MechJebLibTest.TestInitialization", "MechJebLibTest")]
namespace MechJebLibTest
{
    public class TestInitialization : XunitTestFramework
    {
        public TestInitialization(IMessageSink messageSink) :base(messageSink)
        {
            // use per-thread object pools instead of global object pools, in order to isolate them per-test.
            ObjectPoolBase.UseGlobal = false;
        }
    }
}
