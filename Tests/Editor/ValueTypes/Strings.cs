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
            var result = CompilerUtility.Debug(@"
                var str = `Hello, World!`
            ".NormalizeMultiline());

            Assert.AreEqual(Utility.GetStringFromHeap(result, 0), "Hello, World!");
        }

        [Test]
        public void Formatting()
        {
            var result = CompilerUtility.Debug(@"
                var name = `John`
                var message = `Hello, AA!`
            ".NormalizeMultiline());

            Assert.AreEqual("Hello, John!", Utility.GetStringFromHeap(result, 1));
        }
    }
}
