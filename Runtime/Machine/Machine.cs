using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine : INativeDisposable
    {
        public int ValueStackPointer;
        public float YieldDuration;
        public float YieldStartTime;
        public NativeList<int> Values;
        public NativeList<Frame> Frames;
        public Program Program;
        public ExecutionState State;
        int* stackPtr;

        Instruction* instructionPtr;
        int* constantsPtr;

        Frame current;

        int currentChunkIndex;

        public Machine(int initialStackCapacity, Allocator allocator)
        {
            Values = new NativeList<int>(initialStackCapacity, allocator);
            Frames = new NativeList<Frame>(10, allocator);
            ValueStackPointer = 0;
            YieldDuration = 0f;
            YieldStartTime = 0f;
            State = ExecutionState.Running;
            stackPtr = Values.GetUnsafePtr();
            instructionPtr = null;
            constantsPtr = null;
            current = default;
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
            ValueStackPointer = 0;
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