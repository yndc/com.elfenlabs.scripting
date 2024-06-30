using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class BindingsTest
    {
        [Test]
        public void FunctionBinding()
        {
            var result = CompilerUtility.Debug(@"
                    // external function Print(String message)

                    Print(`Hello World`)
                
                ".NormalizeMultiline());

            Assert.AreEqual(result.Stack[0], 0);
        }
    }
}