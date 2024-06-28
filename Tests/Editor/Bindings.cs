using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class BindingsTest
    {
        [Test]
        public void FunctionBinding()
        {
            var stack = CompilerUtility.Debug(@"
                    // external function Print(String message)

                    Print('Hello World')
                
                ".NormalizeMultiline());

            Assert.AreEqual(stack[0], 0);
        }
    }
}