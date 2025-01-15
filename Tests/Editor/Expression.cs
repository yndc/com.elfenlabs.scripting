using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class ExpressionTests
    {
        [Test]
        public void Unary()
        {
            var result = CompilerUtility.Debug(@"
                -5
            ".NormalizeMultiline());

            Assert.AreEqual(-5, result.Stack[0]);
        }

        [Test]
        public void Precedence()
        {
            var result = CompilerUtility.Debug(@"
                (8 - 1 + 3) * 6 - ((3 + 7) * 2) - 24 / 2 + 1 + (((2 - 5 * 4) / 2) + 1 * 100) * 2 - 5 + 5 * 2
            ".NormalizeMultiline());

            Assert.AreEqual(216, result.Stack[0]);
        }

        [Test]
        public void Operators()
        {
            var result = CompilerUtility.Debug(@"
                7 ** 5
            ".NormalizeMultiline());

            Assert.AreEqual(16807, result.Stack[0]);
        }

        [Test]
        public void Increment()
        {
            var result = CompilerUtility.Debug(@"
                var a = 5
                a++
            ".NormalizeMultiline());

            Assert.AreEqual(result.Stack[0], 6);
        }
    }
}
