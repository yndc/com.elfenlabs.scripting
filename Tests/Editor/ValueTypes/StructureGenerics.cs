using NUnit.Framework;
using System;
using System.Linq;

namespace Elfenlabs.Scripting.Tests
{
    public class StructureGenericsTests
    {
        [Test]
        public void Initialization()
        {
            var result = CompilerUtility.Debug(@"
                structure Vector
                    field X Float
                    field Y Float

                structure WithCounter<T>
                    field Value T
                    field Counter Int
                    function Increment() {
                        self.Counter = self.Counter + 1
                    }

                var a = WithCounter<Int> {
                    Value = 10
                    Counter = 0
                }

                var b = WithCounter<Vector> {
                    Value = Vector {
                        X = 1.0
                        Y = 2.0
                    }
                    Counter = 0
                }

                a.Increment()
                b.Increment()
                b.Increment()
                b.Value.X = b.Value.X + 14.0

                var c = b.Value 
                
            ".NormalizeMultiline());

            Assert.AreEqual(10, result.Stack[0]);
            Assert.AreEqual(1, result.Stack[1]);
            Assert.AreEqual(15f, BitConverter.Int32BitsToSingle(result.Stack[0]));
            Assert.AreEqual(2f, BitConverter.Int32BitsToSingle(result.Stack[0]));
            Assert.AreEqual(2, result.Stack[1]);
            Assert.AreEqual(15f, BitConverter.Int32BitsToSingle(result.Stack[0]));
            Assert.AreEqual(2f, BitConverter.Int32BitsToSingle(result.Stack[0]));
        }
    }
}
