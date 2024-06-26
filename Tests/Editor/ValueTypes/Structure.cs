using NUnit.Framework;
using System;
using System.Linq;

namespace Elfenlabs.Scripting.Tests
{
    public class StructureTests
    {
        [Test]
        public void Initialization()
        {
            var result = CompilerUtility.Debug(@"
                structure Person
                    String Name
                    Int Age

                    // in centimetres
                    Float Height

                var a = Person {
                    Name = `John`
                    Age = 30
                    Height = 1.8
                }

                var b = Person {
                    Name = `Jane`
                    Age = 25
                    Height = 1.6
                }

                var name = a.Name
                var ageDiff = a.Age - b.Age
                
            ".NormalizeMultiline());

            Assert.AreEqual(result.Stack[0], 0);
        }

        [Test]
        public void Ooo()
        {
            var result = CompilerUtility.Debug(@"
                structure Coordinate
                    Int X
                    Int Y   

                var a = Coordinate {
                    X = 1
                    Y = 2
                }

                var total = a.X + a.Y
                
            ".NormalizeMultiline());

            Assert.AreEqual(result.Stack[0], 1);
            Assert.AreEqual(result.Stack[1], 2);
            Assert.AreEqual(result.Stack[2], 3);
        }
    }
}
