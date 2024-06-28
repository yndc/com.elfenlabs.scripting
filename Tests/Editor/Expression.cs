using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class ExpressionTests
    {
        [Test]
        public void Unary()
        {
            var stack = CompilerUtility.Debug(@"
                -5
            ".NormalizeMultiline());

            Assert.AreEqual(-5, stack[0]);
        }
        
        [Test]
        public void Precedence()
        {
            var stack = CompilerUtility.Debug(@"
                (8 - 1 + 3) * 6 - ((3 + 7) * 2) - 24 / 2 + 1 + (((2 - 5 * 4) / 2) + 1 * 100) * 2 - 5 + 5 * 2
            ".NormalizeMultiline());

            Assert.AreEqual(216, stack[0]);
        }

        [Test]
        public void Increment()
        {
            var stack = CompilerUtility.Debug(@"
                var a = 5
                a++
            ".NormalizeMultiline());

            Assert.AreEqual(stack[0], 6);
        }
    }
}
