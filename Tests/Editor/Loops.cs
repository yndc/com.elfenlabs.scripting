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
    }
}