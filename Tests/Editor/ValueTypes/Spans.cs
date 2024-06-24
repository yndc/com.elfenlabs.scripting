using NUnit.Framework;
using System;
using System.Linq;

namespace Elfenlabs.Scripting.Tests
{
    public class SpanTests
    {
        [Test]
        public void Spans_Initialization()
        {
            var stack = CompilerUtility.Debug(@"
                var a = { 1, 2, 3 }
                var b = { 1.0, 2.0, 3.0 }

                // initializing empty spans
                var z = Int<64>

                // spans from expressions
                var c = { a.0 + 10, a.1 / 2, 5 * a.2 }

                // spans as function arguments
                function mul(Int<3> vector) returns Int 
                    return vector.0 * vector.1 * vector.2

                var d = mul({ mul(a), mul(c), mul(a) * mul(c) })
            ".NormalizeMultiline());

            Assert.AreEqual(stack[0], 1);
            Assert.AreEqual(stack[1], 2);
            Assert.AreEqual(stack[2], 3);
            Assert.AreEqual(BitConverter.Int32BitsToSingle(stack[3]), 1f);
            Assert.AreEqual(BitConverter.Int32BitsToSingle(stack[4]), 2f);
            Assert.AreEqual(BitConverter.Int32BitsToSingle(stack[5]), 3f);
            Assert.AreEqual(stack[6..70], Enumerable.Repeat(0, 64).ToArray());
            Assert.AreEqual(stack[70], 11);
            Assert.AreEqual(stack[71], 1);
            Assert.AreEqual(stack[72], 15);
            Assert.AreEqual(stack[73], 980100);
        }

        //[Test]
        //public void Tuples_Initialization()
        //{
        //    var stack = CompilerUtility.Debug(@"
        //        var a = (1, 2.0, true, 5)

        //        // initializing empty spans
        //        var z = Int<64>

        //        // spans from expressions
        //        var c = { a.0 + 10, a.1 / 2, 5 * a.2 }

        //        // spans as function arguments
        //        function mul(Int<3> vector) returns Int 
        //            return vector.0 * vector.1 * vector.2

        //        var d = mul({ mul(a), mul(c), mul(a) * mul(c) })
        //".NormalizeMultiline());
        //}
    }
}
