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
                    field Name String
                    field Age Int 

                    // in centimetres
                    field Height Float 

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
        public void FieldRead()
        {
            var result = CompilerUtility.Debug(@"
                structure Coordinate
                    field X Int
                    field Y Int 

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

        [Test]
        public void FieldWrite()
        {
            var result = CompilerUtility.Debug(@"
                structure Coordinate
                    field X Int
                    field Y Int 

                var a = Coordinate {
                    X = 1
                    Y = 2
                }

                a.X = 3 

                var total = a.X + a.Y
                
            ".NormalizeMultiline());

            Assert.AreEqual(result.Stack[0], 3);
            Assert.AreEqual(result.Stack[1], 2);
            Assert.AreEqual(result.Stack[2], 5);
        }

        [Test]
        public void Methods()
        {
            var result = CompilerUtility.Debug(@"
                structure Vector 
                    field X Float
                    field Y Float

                    function Magnitude() returns Float 
                        // return Math.Sqrt(self.X ** 2 + self.Y ** 2)
                        return self.X ** 2 + self.Y ** 2

                var v = Vector { X = 5.0, Y = 10.0 }
                var x = 3.0
                var mag = v.Magnitude()
                
            ".NormalizeMultiline());

            Assert.AreEqual(result.Stack[0], 1);
            Assert.AreEqual(result.Stack[1], 2);
            Assert.AreEqual(result.Stack[2], 3);
        }
    }
}
