using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System.Runtime.CompilerServices;
using Unity.Jobs;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine : INativeDisposable
    {
        public int InstructionPointer;
        public int StackPointer;
        public float YieldDuration;
        public float YieldStartTime;
        public NativeList<int> Stack;
        public ExecutionState State;
        public Code Code;
        int* stackPtr;

        public Machine(int initialStackCapacity, Allocator allocator)
        {
            Stack = new NativeList<int>(initialStackCapacity, allocator);
            InstructionPointer = 0;
            StackPointer = 0;
            YieldDuration = 0f;
            YieldStartTime = 0f;
            State = ExecutionState.Running;
            Code = default;
            stackPtr = Stack.GetUnsafePtr();
        }

        public void Run(string sourceCode)
        {
            Insert(sourceCode);
            Run();
        }

        public void Dispose()
        {
            Stack.Dispose();
        }

        public JobHandle Dispose(JobHandle handle)
        {
            return Stack.Dispose(handle);
        }
    }
}