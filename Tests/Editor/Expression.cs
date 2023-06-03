using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace Elfenlabs.Scripting.Tests
{
    public class ExpressionTests
    {
        Machine machine;

        [SetUp]
        public void Setup()
        {
            machine = new Machine(1024, Allocator.Temp);
        }

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
