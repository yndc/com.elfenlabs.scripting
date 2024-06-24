using NUnit.Framework;
using System;
using System.Linq;

namespace Elfenlabs.Scripting.Tests
{
    public class StringTests
    {
        [Test]
        public void Initialization()
        {
            var stack = CompilerUtility.Debug(@"
                var str = 'Hello, World!'
            ".NormalizeMultiline());

            Assert.AreEqual(stack[0], 0);
        }
    }
}
