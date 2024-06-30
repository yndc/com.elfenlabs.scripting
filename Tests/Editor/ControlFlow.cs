using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class ControlFlowTests
    {
        [Test]
        public void IfTrue()
        {
            var result = CompilerUtility.Debug(@"
                var a = 5

                if a > 0 then
                    a = a + 1
                
                a = a + 2
            ".NormalizeMultiline());

            Assert.AreEqual(8, result.Stack[0]);
        }

        [Test]
        public void IfFalse()
        {
            var result = CompilerUtility.Debug(@"
                var a = 0

                if a > 0 then
                    a = a + 1
                
                a = a + 2
            ".NormalizeMultiline());

            Assert.AreEqual(2, result.Stack[0]);
        }

        [Test]
        public void IfElse()
        {
            var result = CompilerUtility.Debug(@"
                var a = 5

                if a > 100 then
                    a = a + 1
                else
                    a = a * a
                
                a = a + 1
            ".NormalizeMultiline());

            Assert.AreEqual(26, result.Stack[0]);
        }

        [Test]
        public void IfTrueSkipElse()
        {
            var result = CompilerUtility.Debug(@"
                var a = 5

                if a > 0 then
                    a = a + 1
                else
                    a = 1000

                a = a + 1
            ".NormalizeMultiline());

            Assert.AreEqual(7, result.Stack[0]);
        }

        [Test]
        public void SimpleLoop()
        {
            var result = CompilerUtility.Debug(@"
                var a = 5

                if a > 0 then
                    a = a + 1
                else
                    a = 1000

                a = a + 1
            ".NormalizeMultiline());

            Assert.AreEqual(7, result.Stack[0]);
        }
    }
}
