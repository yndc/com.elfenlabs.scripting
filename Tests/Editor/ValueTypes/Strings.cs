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
        public void Interpolation()
        {
            var result = CompilerUtility.Debug(@"
                var name = `John`
                var message = `Hello, {name}!`
            ".NormalizeMultiline());

            Assert.AreEqual("Hello, John!", Utility.GetStringFromHeap(result, 1));
        }

        [Test]
        public void InterpolationNested()
        {
            var result = CompilerUtility.Debug(@"
                var name = `teve`
                var time = `morning`
                var message = `Hello, {`S{name}`}! Good {time}, how are you?`
            ".NormalizeMultiline());

            Assert.AreEqual("Hello, Steve! Good morning, how are you?", Utility.GetStringFromHeap(result, 2));
        }

        //[Test]
        //public void InterpolationNestedBraces()
        //{
        //    var result = CompilerUtility.Debug(@"
        //        structure Person
        //            String Name
        //            Int Age

        //        var message = `Hello, {Person { Name = `Mark`, Age = 25 }}!`
        //    ".NormalizeMultiline());

        //    Assert.AreEqual("Hello, Steve!", Utility.GetStringFromHeap(result, 1));
        //}

        //[Test]
        //public void NestedBraces()
        //{
        //    var result = CompilerUtility.Debug(@"
        //        structure Person
        //            String Name
        //            Int Age

        //        var message = `Hello, {`{Person { Name = `Mark`, Age = 25 }`}!`
        //    ".NormalizeMultiline());

        //    Assert.AreEqual("Hello, Steve!", Utility.GetStringFromHeap(result, 1));
        //}
    }
}
