using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine : INativeDisposable
    {
        public Program Program;
        public ExecutionState State;

        Instruction* instructionPtr;

        Frame currentFrame;

        int currentChunkIndex;

        public Machine(int initialStackCapacity, Allocator allocator)
        {
            Values = new NativeList<int>(initialStackCapacity, allocator);
            Frames = new NativeList<Frame>(10, allocator);
            heap = new Arena(256, allocator);
            State = ExecutionState.Running;
            heapPtr = heap.GetUnsafePtr();
            stackHeadPtr = Values.GetUnsafePtr();
            frameValuesPtr = Values.GetUnsafePtr();
            instructionPtr = null;
            constantsPtr = null;
            currentFrame = default;
            Program = default;
            currentChunkIndex = 0;
        }

        /// <summary>
        /// Prepare the virtual machine with the given program
        /// </summary>
        /// <param name="code"></param>
        public unsafe void Boot(Program program)
        {
            Program = program;
            State = ExecutionState.Running;

            currentChunkIndex = program.EntryPoint;

            var chunk = program.Chunks[program.EntryPoint];
            instructionPtr = (Instruction*)chunk.Instructions.GetUnsafePtr();
            constantsPtr = (int*)chunk.Constants.GetUnsafePtr();
        }

        public void Dispose()
        {
            Values.Dispose();
        }

        public JobHandle Dispose(JobHandle handle)
        {
            return Values.Dispose(handle);
        }
    }
}