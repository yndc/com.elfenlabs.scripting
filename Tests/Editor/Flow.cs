using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class FlowTests
    {
        [Test]
        public void If()
        {
            var stack = CompilerUtility.Debug(@"
                var a = 5
                if a > 0 
                    a = a + 10
            ");

            Assert.AreEqual(-19, stack[0]);
            Assert.AreEqual(40, stack[1]);
            Assert.AreEqual(21, stack[2]);
        }
    }
}
