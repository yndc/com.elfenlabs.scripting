using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public unsafe struct Frame
    {
        public Instruction* InstructionPtr;
        public int* ConstantsPtr;
        public int* StackFramePtr;
        public int* StackHeadPtr;
    }

    public unsafe partial struct Machine
    {
        int* stackFramePtr;

        /// <summary>
        /// Call a function in another chunk 
        /// </summary>
        /// <param name="chunkIndex"></param>
        /// <param name="parametersWordLength"></param>
        void Call(ushort chunkIndex, byte parametersWordLength)
        {
            // Include the parameters in the new frame
            var newFrameValuesPtr = stackHeadPtr - parametersWordLength;

            // Save the current frame
            Frames.Add(new Frame
            {
                ConstantsPtr = constantsPtr,
                InstructionPtr = instructionPtr,
                StackFramePtr = stackFramePtr,
                StackHeadPtr = newFrameValuesPtr
            });

            stackFramePtr = newFrameValuesPtr;

            var chunk = Program.Chunks[chunkIndex];
            instructionPtr = (Instruction*)chunk.Instructions.GetUnsafePtr();
            constantsPtr = (int*)chunk.Constants.GetUnsafePtr();
        }

        /// <summary>
        /// Return from a function call
        /// </summary>
        /// <param name="returnWordLength"></param>
        void Return(byte returnWordLength)
        {
            // Get the last call frame
            var callerFrame = Frames[^1];
            Frames.RemoveAtSwapBack(Frames.Length - 1);

            // Copy the return value to the caller stack
            if (returnWordLength > 0)
            {
                UnsafeUtility.MemCpy(
                    callerFrame.StackHeadPtr,
                    stackHeadPtr - returnWordLength,
                    returnWordLength * CompilerUtility.WordSize);
            }

            // Restore the frame state
            instructionPtr = callerFrame.InstructionPtr;
            constantsPtr = callerFrame.ConstantsPtr;
            stackHeadPtr = callerFrame.StackHeadPtr + returnWordLength;
            stackFramePtr = callerFrame.StackFramePtr;
        }

        Frame CaptureFrame()
        {
            return new Frame
            {
                ConstantsPtr = constantsPtr,
                InstructionPtr = instructionPtr,
                StackFramePtr = stackFramePtr,
                StackHeadPtr = stackHeadPtr
            };
        }

        void RestoreFrame(Frame frame)
        {

        }
    }
}