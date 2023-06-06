using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class FunctionTests
    {
        [Test]
        public void DeclarationAndUsage()
        {
            var stack = CompilerUtility.Debug(@"
                function Add (Int a, Int b) returns Int
                    return a + b
    
                var result = Add(1, 5) 
            ");

            Assert.AreEqual(6, stack[0]);
        }
    }
}
