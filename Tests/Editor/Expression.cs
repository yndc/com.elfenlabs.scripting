using NUnit.Framework;
using Unity.Collections;

namespace Elfenlabs.Scripting.Tests
{
    public class ExpressionTests
    {
        [Test]
        public void Precedence()
        {
            var stack = CompilerUtility.Debug(@"
                (8 - 1 + 3) * 6 - ((3 + 7) * 2) - 24 / 2 + 1 + (((2 - 5 * 4) / 2) + 1 * 100) * 2 - 5 + 5 * 2
            ");

            Assert.AreEqual(216, stack[0]);
        }
    }
}
