using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class FlowTests
    {
        [Test]
        public void IfTrue()
        {
            var stack = CompilerUtility.Debug(@"
                var a = 5

                if a > 0 then
                    a = a + 1
                
                a = a + 2
            ");

            Assert.AreEqual(8, stack[0]);
        }

        [Test]
        public void IfFalse()
        {
            var stack = CompilerUtility.Debug(@"
                var a = 0

                if a > 0 then
                    a = a + 1
                
                a = a + 2
            ");

            Assert.AreEqual(2, stack[0]);
        }

        [Test]
        public void IfElse()
        {
            var stack = CompilerUtility.Debug(@"
                var a = 5

                if a > 100 then
                    a = a + 1
                else
                    a = a * a
                
                a = a + 1
            ");

            Assert.AreEqual(26, stack[0]);
        }

        [Test]
        public void IfTrueSkipElse()
        {
            var stack = CompilerUtility.Debug(@"
                var a = 5

                if a > 0 then
                    a = a + 1
                else
                    a = 1000

                a = a + 1
            ");

            Assert.AreEqual(7, stack[0]);
        }
    }
}
