using NUnit.Framework;

namespace Elfenlabs.Scripting.Tests
{
    public class ListTests
    {
        [Test]
        public void Initialization()
        {
            var result = CompilerUtility.Debug(@"
                var l1 = Int[]
                var l2 = [1, 2, 3]
            ".NormalizeMultiline());
        }

        [Test]
        public void Push()
        {
            var result = CompilerUtility.Debug(@"
                var list = Int[]
                list.Push(1)
                list.Push(2)
                list.Push(3)
            ".NormalizeMultiline());
        }

        [Test]
        public void MemberRead()
        {
            var result = CompilerUtility.Debug(@"
                var list = Int[]
                list.Push(1)
                list.Push(2)
                list.Push(3)

                var a = list[2]
                var b = list[1]
                var c = list[0]
            ".NormalizeMultiline());

            Assert.AreEqual(3, result.Stack[0]);
            Assert.AreEqual(2, result.Stack[1]);
            Assert.AreEqual(1, result.Stack[2]);
        }

        [Test]
        public void MemberAssignment()
        {
            var result = CompilerUtility.Debug(@"
                var numbers = [1, 2, 3]

                numbers[0] = numbers[numbers[1]] - numbers[1] + 5
                numbers[1 + 1] = 100

                var a = numbers[0]
                var b = numbers[2]
            ".NormalizeMultiline());

            Assert.AreEqual(6, result.Stack[0]);
            Assert.AreEqual(100, result.Stack[1]);
        }

        // Pop

        //  

        // [Test]
        // public void ListOfStructs()
        // {
        //     var result = CompilerUtility.Debug(@"
        //         struct Coordinate
        //             field X Int
        //             field Y Int

        //         var points = create [
        //             Coordinate { X = 1, Y = 2 }
        //             Coordinate { X = 3, Y = 1 }
        //             Coordinate { X = 3, Y = 6 }
        //             Coordinate { X = 7, Y = 2 }
        //             Coordinate { X = 9, Y = 0 }
        //         ]

        //         points[2].X = 100
        //         points[points[points[3].Y].X - 1] = points[points[points[0].X].X + 1]

        //         // load constant 3 
        //         // load array ptr to stack
        //         // add
        //         // load .X offset
        //         // add
        //         // load heap from ptr
        //         // load constant -1
        //         // add
        //         // load array ptr to stack
        //         // add

        //         // so we need 1) instruction to load heap with offset



        //         var total = 0
        //         for p in points
        //             total = total + p.X + p.Y
        //     ".NormalizeMultiline());

        //     Assert.AreEqual(3, result.Stack[0]);
        //     Assert.AreEqual(3, result.Stack[1]);
        // }
    }
}
