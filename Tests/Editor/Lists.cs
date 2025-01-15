using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class ListTests
    {
        [Test]
        public void AssignmentToStructField()
        {
            var result = CompilerUtility.Debug(@"
                struct Coordinate
                    field X Int
                    field Y Int

                var a = Coordinate {
                    X = 1
                    Y = 2
                }

                a.X = 3
                a.Y = a.X
            ".NormalizeMultiline());

            Assert.AreEqual(3, result.Stack[0]);
            Assert.AreEqual(3, result.Stack[1]);
        }

        [Test]
        public void AssignmentToStructFieldStruct()
        {
            var result = CompilerUtility.Debug(@"
                struct Rectangle
                    field TopLeft Coordinate
                    field BottomRight Coordinate
                
                struct Coordinate
                    field X Int
                    field Y Int

                var r = Rectangle {
                    TopLeft = Coordinate {
                        X = 1
                        Y = 2
                    }
                    BottomRight = Coordinate {
                        X = 3
                        Y = 4
                    }
                }

                r.TopLeft.X = 3
                r.BottomRight.Y = r.TopLeft.Y
            ".NormalizeMultiline());

            Assert.AreEqual(3, result.Stack[0]);
            Assert.AreEqual(3, result.Stack[1]);
        }

        [Test]
        public void AssignmentToArray()
        {
            var result = CompilerUtility.Debug(@"
                var numbers = create [1, 2, 3]

                numbers[1 + 1] = 100
                numbers[0] = numbers[1] + numbers[2]
            ".NormalizeMultiline());

            Assert.AreEqual(3, result.Stack[0]);
            Assert.AreEqual(3, result.Stack[1]);
        }

        [Test]
        public void ListOfStructs()
        {
            var result = CompilerUtility.Debug(@"
                struct Coordinate
                    field X Int
                    field Y Int

                var points = create [
                    Coordinate { X = 1, Y = 2 }
                    Coordinate { X = 3, Y = 1 }
                    Coordinate { X = 3, Y = 6 }
                    Coordinate { X = 7, Y = 2 }
                    Coordinate { X = 9, Y = 0 }
                ]

                points[2].X = 100
                points[points[points[3].Y].X - 1] = points[points[points[0].X].X + 1]

                // load constant 3 
                // load array ptr to stack
                // add
                // load .X offset
                // add
                // load heap from ptr
                // load constant -1
                // add
                // load array ptr to stack
                // add

                // so we need 1) instruction to load heap with offset



                var total = 0
                for p in points
                    total = total + p.X + p.Y
            ".NormalizeMultiline());

            Assert.AreEqual(3, result.Stack[0]);
            Assert.AreEqual(3, result.Stack[1]);
        }
    }
}
