using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class LoopTests
    {
        [Test]
        public void While()
        {
            var result = CompilerUtility.Debug(@"
                var x = 2
                while x < 10000
                    x = x * x

                // Print(x)
            ".NormalizeMultiline());

            Assert.AreEqual(65536, result.Stack[0]);
        }

        [Test]
        public void WhileBreak()
        {
            var result = CompilerUtility.Debug(@"
                var x = 2
                while 1 == 1
                    x = x + 1
                    if x > 100 then
                        break

                // Print(x)
            ".NormalizeMultiline());

            Assert.AreEqual(101, result.Stack[0]);
        }

        [Test]
        public void WhileContinue()
        {
            var result = CompilerUtility.Debug(@"
                var sum = 0
                var x = 0
                while x < 100
                    x++
                    if x % 2 == 0 then
                        continue
                    sum = sum + x

                // Print(x)
            ".NormalizeMultiline());

            Assert.AreEqual(2500, result.Stack[0]);
        }
    }
}