using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class VariableTests
    {
        [Test]
        public void Loading()
        {
            var result = CompilerUtility.Debug(@"
                var a = 1 - 20          // -19
                var b = 2 + (-a * 2)    // 40
                var c = a + b           // 21
            ".NormalizeMultiline());

            Assert.AreEqual(-19, result.Stack[0]);
            Assert.AreEqual(40, result.Stack[1]);
            Assert.AreEqual(21, result.Stack[2]);
        }

        [Test]
        public void Assignment()
        {
            var result = CompilerUtility.Debug(@"
                var a = 1
                var b = 2
                a = a + b
                b = a + 10 + a + b
            ".NormalizeMultiline());

            Assert.AreEqual(3, result.Stack[0]);
            Assert.AreEqual(18, result.Stack[1]);
        }
    }
}
