using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Elfenlabs.Scripting
{
    public struct Code
    {
        public NativeArray<Instruction> Instructions;
        public NativeArray<uint> Constants;
        public bool IsEmpty => !Instructions.IsCreated && !Constants.IsCreated;
        public Code(NativeList<Instruction> instructions, NativeList<uint> constants)
        {
            Instructions = instructions.AsArray();
            Constants = constants.AsArray();
        }
    }

    public unsafe struct CodeBuilder
    {
        NativeList<Instruction> m_Instructions;
        NativeList<uint> m_Constants;

        public int Length => m_Instructions.Length;

        public CodeBuilder(Allocator allocator)
        {
            m_Instructions = new NativeList<Instruction>(allocator);
            m_Constants = new NativeList<uint>(allocator);
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
            m_Instructions.Add(instruction);
            return m_Instructions.Length - 1;
        }

        public ref Instruction Patch(int index)
        {
            return ref m_Instructions.ElementAt(index);
        }

        public ushort AddConstant(int[] value)
        {
            fixed (int* ptr = value)
            {
                var offset = (ushort)m_Constants.Length;
                var wordLength = value.Length;
                m_Constants.ResizeUninitialized(m_Constants.Length + wordLength);
                UnsafeUtility.MemCpy(m_Constants.GetUnsafePtr() + offset, ptr, wordLength * sizeof(int));
                Add(new Instruction(InstructionType.LoadConstant, offset, (byte)wordLength));
                return offset;
            }
        }

        public ushort AddConstant<T>(T value) where T : unmanaged
        {
            var offset = (ushort)m_Constants.Length;
            var wordLength = CompilerUtility.GetWordLength<T>();
            m_Constants.ResizeUninitialized(m_Constants.Length + wordLength);
            UnsafeUtility.CopyStructureToPtr(ref value, m_Constants.GetUnsafePtr() + offset);
            Add(new Instruction(InstructionType.LoadConstant, offset, (byte)wordLength));
            return offset;
        }

        public Code Build()
        {
            AssertEndWithHalt();
            return new Code(m_Instructions, m_Constants);
        }

        public BlobBuilderArray<byte> Build(BlobBuilder blobBuilder, ref BlobArray<byte> dst)
        {
            AssertEndWithHalt();
            var codeBuilder = blobBuilder.Allocate(ref dst, m_Instructions.Length);
            unsafe
            {
                UnsafeUtility.MemCpy(codeBuilder.GetUnsafePtr(), m_Instructions.GetUnsafePtr(), m_Instructions.Length);
            }
            return codeBuilder;
        }

        void AssertEndWithHalt()
        {
            if (m_Instructions[^1].Type != InstructionType.Halt)
            {
                m_Instructions.Add(new Instruction(InstructionType.Halt));
            }
        }
    }
}