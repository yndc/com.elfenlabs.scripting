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
        public void MethodRead()
        {
            var result = CompilerUtility.Debug(@"
                structure Vector 
                    field X Float
                    field Y Float

                    function Magnitude() returns Float 
                        return self.X ** 2.0 + self.Y ** 2.0

                    function Transpose() returns Void
                        var temp = self.X
                        self.X = self.Y
                        self.Y = temp

                var a = 10000
                var v = Vector { X = 5.0, Y = 10.0 }
                var mag = v.Magnitude()  
            ".NormalizeMultiline());

            Assert.AreEqual(result.Stack[0], 10000);
            Assert.AreEqual(5f, BitConverter.Int32BitsToSingle(result.Stack[1]));
            Assert.AreEqual(10f, BitConverter.Int32BitsToSingle(result.Stack[2]));
            Assert.AreEqual(125f, BitConverter.Int32BitsToSingle(result.Stack[3]));
        }

        [Test]
        public void MethodMutate()
        {
            var result = CompilerUtility.Debug(@"
                structure Vector 
                    field X Float
                    field Y Float
                    function Transpose() returns Void
                        var temp = self.X
                        self.X = self.Y
                        self.Y = temp

                var a = 10000
                var v = Vector { X = 5.0, Y = 10.0 }
                v.Transpose()
                
                var b = 222
                var w = Vector { Y = 3.0, X = 2.0 }
                w.Transpose()
                
            ".NormalizeMultiline());

            Assert.AreEqual(10000, result.Stack[0]);
            Assert.AreEqual(10f, BitConverter.Int32BitsToSingle(result.Stack[1]));
            Assert.AreEqual(5f, BitConverter.Int32BitsToSingle(result.Stack[2]));
            Assert.AreEqual(222, result.Stack[3]);
            Assert.AreEqual(3f, BitConverter.Int32BitsToSingle(result.Stack[4]));
            Assert.AreEqual(2f, BitConverter.Int32BitsToSingle(result.Stack[5]));
        }

        [Test]
        public void MethodWrite()
        {
            var result = CompilerUtility.Debug(@"
                structure Vector 
                    field X Float
                    field Y Float

                    function Transpose() returns Void
                        var temp = self.X
                        self.X = self.Y
                        self.Y = temp

                var a = 10000
                var v = Vector { X = 5.0, Y = 10.0 }
                var w = Vector { X = 2.0, Y = 3.0 }
                
                w.Transpose()   
                v.Transpose()   
                
            ".NormalizeMultiline());

            Assert.AreEqual(result.Stack[0], 10000);
            Assert.AreEqual(10f, BitConverter.Int32BitsToSingle(result.Stack[1]));
            Assert.AreEqual(5f, BitConverter.Int32BitsToSingle(result.Stack[2]));
            Assert.AreEqual(3f, BitConverter.Int32BitsToSingle(result.Stack[3]));
            Assert.AreEqual(2f, BitConverter.Int32BitsToSingle(result.Stack[4]));
        }

        [Test]
        public void Nested()
        {
            var result = CompilerUtility.Debug(@"
                structure Vector 
                    field X Float
                    field Y Float

                structure Rectangle
                    field TopLeft Vector
                    field BottomRight Vector

                var rect = Rectangle {
                    BottomRight = Vector { Y = 2.0, X = 10.0 }
                    TopLeft = Vector { X = 1.0, Y = 5.0 }
                }
            ".NormalizeMultiline());

            Assert.AreEqual(1f, BitConverter.Int32BitsToSingle(result.Stack[0]));
            Assert.AreEqual(5f, BitConverter.Int32BitsToSingle(result.Stack[1]));
            Assert.AreEqual(10f, BitConverter.Int32BitsToSingle(result.Stack[2]));
            Assert.AreEqual(2f, BitConverter.Int32BitsToSingle(result.Stack[3]));
        }

        [Test]
        public void NestedMethod()
        {
           var result = CompilerUtility.Debug(@"
               structure Vector 
                   field X Float
                   field Y Float

                   function Magnitude() returns Float 
                       return self.X ** 2.0 + self.Y ** 2.0

                   function Transpose() returns Void
                       var temp = self.X
                       self.X = self.Y
                       self.Y = temp

               structure Rectangle
                   field BottomLeft Vector
                   field TopRight Vector

                   function Area() returns Float
                       var width = self.TopRight.X - self.BottomLeft.X
                       var height = self.TopRight.Y - self.BottomLeft.Y
                       return width * height

                   function Transpose() returns Void
                       self.TopRight.Transpose()
                       self.BottomLeft.Transpose()

               var rect = Rectangle {
                   TopRight = Vector { X = 3.0, Y = 4.0 }
                   BottomLeft = Vector { Y = 2.0, X = 1.0 }
               }

               var area1 = rect.Area()

               rect.TopRight.X = 10.0
               rect.TopRight.Transpose()

               var area2 = rect.Area()

           ".NormalizeMultiline());

           Assert.AreEqual(1f, BitConverter.Int32BitsToSingle(result.Stack[0]));
           Assert.AreEqual(2f, BitConverter.Int32BitsToSingle(result.Stack[1]));
           Assert.AreEqual(4f, BitConverter.Int32BitsToSingle(result.Stack[2]));
           Assert.AreEqual(10f, BitConverter.Int32BitsToSingle(result.Stack[3]));
           Assert.AreEqual(4f, BitConverter.Int32BitsToSingle(result.Stack[4]));
           Assert.AreEqual(24f, BitConverter.Int32BitsToSingle(result.Stack[5]));
        }
    }
}
