using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class ModuleTests
    {
        [Test]
        public void SimpleImport()
        {
            var stack = CompilerUtility.Debug(@"
                //module MyLibrary

                function Add(Int a, Int b) returns Int
                    return a + b
                
            ".NormalizeMultiline());

            //Assert.AreEqual(8, stack[0]);
        }

    }
}