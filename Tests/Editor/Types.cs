using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class TypeTests
    {
        [Test]
        public void Spans()
        {
            var stack = CompilerUtility.Debug(@"
                var ints = { 1, 2, 3 }
                var floats = { 1.0, 2.0, 3.0 }

                // initializing empty spans
                var zeroes = Int<64>

                // spans from expressions
                var ints2 = { ints[0] + 10, ints[1] / 2, 5 }
            ".NormalizeMultiline());

            Assert.AreEqual(-19, stack[0]);
            Assert.AreEqual(40, stack[1]);
            Assert.AreEqual(21, stack[2]);
        }
    }
}
