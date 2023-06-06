using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Elfenlabs.Scripting
{
    public unsafe struct Frame
    {
        public Instruction* InstructionPointer;
        public int* ConstantsPointer;
        public int ValueStackPointer;
    }

    public unsafe partial struct Machine
    {
        public void Call(ushort chunkIndex, byte parametersWordLength)
        {
            Frames.Add(new Frame
            {
                InstructionPointer = instructionPtr,
                ConstantsPointer = constantsPtr,
                ValueStackPointer = ValueStackPointer - parametersWordLength,
            });
            var chunk = Program.Chunks[chunkIndex];
            instructionPtr = (Instruction*)chunk.Instructions.GetUnsafePtr();
            constantsPtr = (int*)chunk.Constants.GetUnsafePtr();
        }

        public void Return(byte returnWordLength)
        {
            var frame = Frames[^1];
            Frames.RemoveAtSwapBack(Frames.Length - 1);
            UnsafeUtility.MemCpy(
                stackPtr + frame.ValueStackPointer,
                stackPtr + ValueStackPointer - returnWordLength,
                returnWordLength * CompilerUtility.WordSize);
            instructionPtr = frame.InstructionPointer;
            constantsPtr = frame.ConstantsPointer;
            ValueStackPointer = frame.ValueStackPointer + returnWordLength;
        }
    }
}