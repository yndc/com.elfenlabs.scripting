using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace Elfenlabs.Scripting.Tests
{
    public class ExpressionTests
    {
        Machine machine;

        [SetUp]
        public void Setup()
        {
            machine = new Machine(1024, Allocator.Temp);
        }

        [Test]
        public void Precedence()
        {
            var stack = CompilerUtility.Debug(@"
                var a = 2 + 5
            ");

            Debug.Log(stack[0]);

            //Assert.AreEqual(7, machine.ReadStackAs<int>());
        }
    }
}
