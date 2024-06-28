using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class ModuleTests
    {
        [Test]
        public void SimpleImport()
        {
            var stack = CompilerUtility.Debug(@"
                module MyLibrary

                function Add(a, b)
                    return a + b
                end
            ".NormalizeMultiline());

            Assert.AreEqual(8, stack[0]);
        }

    }
}