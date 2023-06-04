using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class FlowTests
    {
        [Test]
        public void If()
        {
            var stack = CompilerUtility.Debug(@"
                var a = 0

                if a > 0 then
                    a = a + 1
                a = a + 2
            ");

            Assert.AreEqual(15, stack[0]);
        }
    }
}
