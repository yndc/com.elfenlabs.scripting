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
                var message = `Hello, {name}!`
            ".NormalizeMultiline());

            Assert.AreEqual("Hello, John!", Utility.GetStringFromHeap(result, 1));
        }

        [Test]
        public void NestedInterpolation()
        {
            var result = CompilerUtility.Debug(@"
                var name = `teve`
                var message = `Hello, {`S{name}`}!`
            ".NormalizeMultiline());

            Assert.AreEqual("Hello, Steve!", Utility.GetStringFromHeap(result, 1));
        }

        [Test]
        public void InsideBraces()
        {
            var result = CompilerUtility.Debug(@"
                structure Person
                    String Name
                    Int Age

                var message = `Hello, {Person { Name = `Mark`, Age = 25 }}!`
            ".NormalizeMultiline());

            Assert.AreEqual("Hello, Steve!", Utility.GetStringFromHeap(result, 1));
        }

        [Test]
        public void NestedBraces()
        {
            var result = CompilerUtility.Debug(@"
                structure Person
                    String Name
                    Int Age

                var message = `Hello, {`{Person { Name = `Mark`, Age = 25 }`}!`
            ".NormalizeMultiline());

            Assert.AreEqual("Hello, Steve!", Utility.GetStringFromHeap(result, 1));
        }
    }
}
