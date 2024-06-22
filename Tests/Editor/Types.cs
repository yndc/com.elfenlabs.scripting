using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class TypeTests
    {
        [Test]
        public void Spans()
        {
            var stack = CompilerUtility.Debug(@"
                var a = { 1, 2, 3 }
                var b = { 1.0, 2.0, 3.0 }

                // initializing empty spans
                var z = Int<64>

                // spans from expressions
                var c = { a.0 + 10, a.1 / 2, 5 * a.2 }
            ".NormalizeMultiline());

            Assert.AreEqual(-19, stack[0]);
            Assert.AreEqual(40, stack[1]);
            Assert.AreEqual(21, stack[2]);
        }
    }
}
