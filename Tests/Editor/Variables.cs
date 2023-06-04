using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class VariableTests
    {
        [Test]
        public void Order()
        {
            var stack = CompilerUtility.Debug(@"
                var a = 1 - 20          // -19
                var b = 2 + (-a * 2)    // 40
                var c = a + b           // 21
            ");

            Assert.AreEqual(-19, stack[0]);
            Assert.AreEqual(40, stack[1]);
            Assert.AreEqual(21, stack[2]);
        }
    }
}