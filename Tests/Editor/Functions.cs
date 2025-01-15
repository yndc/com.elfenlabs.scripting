using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class FunctionTests
    {
        [Test]
        public void DeclarationAndUsage()
        {
            var result = CompilerUtility.Debug(@"
                function Add (Int a, Int b) returns Int
                    return a + b
    
                var result = Add(3, 5) 
            ".NormalizeMultiline());

            Assert.AreEqual(8, result.Stack[0]);
        }

        [Test]
        public void IfInsideFunction()
        {
            var result = CompilerUtility.Debug(@"
                function SafeDiv (Int number, Int divisor) returns Int
                    if divisor == 0 then
                        // asdasa
                        
                        return 0
                    return number / divisor
    
                var result = SafeDiv(10, 5) 
            ".NormalizeMultiline());

            Assert.AreEqual(2, result.Stack[0]);
        }

        [Test]
        public void CallFramePosition()
        {
            var result = CompilerUtility.Debug(@"
                var a = 1
                var b = 2
                var c = 4
                function Div (Int x, Int y) returns Int
                    return x / y
                var d = Div(c, b)               // 2
            ".NormalizeMultiline());

            Assert.AreEqual(4, result.Stack.Length);
            Assert.AreEqual(1, result.Stack[0]);
            Assert.AreEqual(2, result.Stack[1]);
            Assert.AreEqual(4, result.Stack[2]);
            Assert.AreEqual(2, result.Stack[3]);
        }

        [Test]
        public void Shadowing()
        {
            var result = CompilerUtility.Debug(@"
                var a = 1
                var b = 2
                var c = 3
                function Div (Int a, Int b) returns Int
                    return a / b
                var d = 4
                var e = Div(d, b)               // 2
                var result = a + b + c + d + e  // 12
            ".NormalizeMultiline());

            Assert.AreEqual(6, result.Stack.Length);
            Assert.AreEqual(1, result.Stack[0]);
            Assert.AreEqual(2, result.Stack[1]);
            Assert.AreEqual(3, result.Stack[2]);
            Assert.AreEqual(4, result.Stack[3]);
            Assert.AreEqual(2, result.Stack[4]);
            Assert.AreEqual(12, result.Stack[5]);
        }

        //[Test]
        //public void Closures()
        //{
        //    var result = CompilerUtility.Debug(@"
        //        var x = 1
        //        function Increment () returns Void
        //            x = x + 1

        //        Increment()
        //        Increment()
        //        Increment()
        //        Increment()
        //    ".NormalizeMultiline());

        //    Assert.AreEqual(5, result.Stack[0]);
        //}
    }
}
