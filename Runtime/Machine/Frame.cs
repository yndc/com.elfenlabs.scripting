using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public unsafe struct Frame
    {
        public Instruction* InstructionPtr;
        public int* ConstantsPtr;
        public int* FrameValuesPtr;
        public int* ValuesPtr;
    }

    public unsafe partial struct Machine
    {
        int* frameValuesPtr;

        /// <summary>
        /// Call a function in another chunk 
        /// </summary>
        /// <param name="chunkIndex"></param>
        /// <param name="parametersWordLength"></param>
        void Call(ushort chunkIndex, byte parametersWordLength)
        {
            // Include the parameters in the new frame
            var newFrameValuesPtr = valuesPtr - parametersWordLength;

            // Save the current frame
            Frames.Add(new Frame
            {
                ConstantsPtr = constantsPtr,
                InstructionPtr = instructionPtr,
                FrameValuesPtr = frameValuesPtr,
                ValuesPtr = newFrameValuesPtr
            });

            frameValuesPtr = newFrameValuesPtr;

            var chunk = Program.Chunks[chunkIndex];
            instructionPtr = (Instruction*)chunk.Instructions.GetUnsafePtr();
            constantsPtr = (int*)chunk.Constants.GetUnsafePtr();
        }

        void Return(byte returnWordLength)
        {
            // Get the last call frame
            var frame = Frames[^1];
            Frames.RemoveAtSwapBack(Frames.Length - 1);

            // Copy the return value to the caller stack
            UnsafeUtility.MemCpy(
                frame.ValuesPtr,
                valuesPtr - returnWordLength,
                returnWordLength * CompilerUtility.WordSize);

            // Restore the frame state
            instructionPtr = frame.InstructionPtr;
            constantsPtr = frame.ConstantsPtr;
            valuesPtr = frame.ValuesPtr + returnWordLength;
            frameValuesPtr = frame.FrameValuesPtr;
        }

        Frame CaptureFrame()
        {
            return new Frame
            {
                ConstantsPtr = constantsPtr,
                InstructionPtr = instructionPtr,
                FrameValuesPtr = frameValuesPtr,
                ValuesPtr = valuesPtr
            };
        }

        void RestoreFrame(Frame frame)
        {

        }
    }
}