using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using System.Text;

namespace Elfenlabs.Scripting
{
    public struct ByteCode
    {
        public NativeArray<Instruction> Instructions;
        public NativeArray<int> Constants;
        public bool IsEmpty => !Instructions.IsCreated && !Constants.IsCreated;
        public ByteCode(NativeList<Instruction> instructions, NativeList<int> constants)
        {
            Instructions = instructions.AsArray();
            Constants = constants.AsArray();
        }
    }

    public unsafe struct ByteCodeBuilder
    {
        NativeList<Instruction> instructions;
        NativeList<int> constants;

        public int InstructionCount => instructions.Length;

        public ByteCodeBuilder(Allocator allocator)
        {
            instructions = new NativeList<Instruction>(allocator);
            constants = new NativeList<int>(allocator);
        }

        public void Halt()
        {
            Add(new Instruction(InstructionType.Halt));
        }

        public void Yield()
        {
            Yield(new half(0));
        }

        public void Yield(half time)
        {
            Add(new Instruction(InstructionType.Yield, time.value));
        }

        public int Add(Instruction instruction)
        {
            instructions.Add(instruction);
            return instructions.Length - 1;
        }

        public ref Instruction Patch(int index)
        {
            return ref instructions.ElementAt(index);
        }

        public ushort AddConstant(byte[] value)
        {
            fixed (byte* ptr = value)
            {
                var offset = (ushort)constants.Length;
                var wordLength = CompilerUtility.GetWordLength(value.Length);
                constants.ResizeUninitialized(constants.Length + wordLength);
                UnsafeUtility.MemCpy(constants.GetUnsafePtr() + offset, ptr, wordLength * sizeof(int));
                Add(new Instruction(InstructionType.PushConstant, offset, (byte)wordLength));
                return offset;
            }
        }

        public ushort AddConstant(string value)
        {
            var offset = (ushort)constants.Length;
            var wordLength = CompilerUtility.GetWordLength(value.Length) + 1;
            constants.ResizeUninitialized(constants.Length + wordLength);

            // Set the first word as the length of the string
            constants.GetUnsafePtr()[offset] = value.Length;

            // Copy the string to the constant buffer
            var bytes = Encoding.UTF8.GetBytes(value);
            fixed (byte* ptr = bytes)
            {
                // Offset by 1 to skip the length
                UnsafeUtility.MemCpy((byte*)constants.GetUnsafePtr() + offset * sizeof(int) + sizeof(int), ptr, bytes.Length);
                Add(new Instruction(InstructionType.HeapLoadConstant, offset, (byte)wordLength));
                return offset;
            }
        }

        public ushort AddConstant<T>(T value) where T : unmanaged
        {
            var offset = (ushort)constants.Length;
            var wordLength = CompilerUtility.GetWordLength<T>();
            constants.ResizeUninitialized(constants.Length + wordLength);
            UnsafeUtility.CopyStructureToPtr(ref value, constants.GetUnsafePtr() + offset);
            Add(new Instruction(InstructionType.PushConstant, offset, (byte)wordLength));
            return offset;
        }

        public ByteCode Build()
        {
            EnsureEndWithHalt();
            return new ByteCode(instructions, constants);
        }

        public BlobBuilderArray<byte> Build(BlobBuilder blobBuilder, ref BlobArray<byte> dst)
        {
            EnsureEndWithHalt();
            var codeBuilder = blobBuilder.Allocate(ref dst, instructions.Length);
            unsafe
            {
                UnsafeUtility.MemCpy(codeBuilder.GetUnsafePtr(), instructions.GetUnsafePtr(), instructions.Length);
            }
            return codeBuilder;
        }

        public void EnsureEndWithHalt()
        {
            if (instructions.Length == 0 || (instructions[^1].Type != InstructionType.Halt && instructions[^1].Type != InstructionType.Return))
            {
                instructions.Add(new Instruction(InstructionType.Halt));
            }
        }

        public void EnsureEndWithReturn()
        {
            if (instructions.Length == 0 || instructions[^1].Type != InstructionType.Return)
            {
                instructions.Add(new Instruction(InstructionType.Return, 0));
            }
        }

        public void AssertEndWithReturn()
        {
            if (instructions.Length == 0 || instructions[^1].Type != InstructionType.Return)
            {
                throw new System.Exception("Chunk must end with a return instruction");
            }
        }
    }
}