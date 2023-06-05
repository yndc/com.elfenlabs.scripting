using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class VariableTests
    {
        [Test]
        public void Loading()
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

        [Test]
        public void Assignment()
        {
            var stack = CompilerUtility.Debug(@"
                var a = 1
                var b = 2
                a = a + b
                b = a + 10 + a + b
            ");

            Assert.AreEqual(3, stack[0]);
            Assert.AreEqual(18, stack[1]);
        }
    }
}
