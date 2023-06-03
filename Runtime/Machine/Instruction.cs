using Unity.Entities;

namespace Elfenlabs.Scripting
{
    public enum InstructionType : byte
    {
        Halt,
        Yield,

        // Load operations
        LoadConstant,       // <short>    - The index of the constant to load
                            // <byte>     - The number of words to load from the constant

        LoadVariable,       // <short>    - The index of the variable to load
                            // <byte>     - The number of words to load from the stack

        Pop,                // <short>    - The number of words to pop from the stack

        // Integer operations
        IntAdd,
        IntSubstract,
        IntMultiply,
        IntDivide,
        IntModulo,
        IntNegate,

        // Float operations
        FloatAdd,
        FloatSubstract,
        FloatMultiply,
        FloatDivide,
        FloatModulo,
        FloatNegate,

        // Boolean operations
        BoolNegate,

        // Comparison operations
        Equal,      // Equality can be used for any data type as we compare the raw bytes
        NotEqual,
        IntLessThan,
        IntLessThanEqual,
        IntGreaterThan,
        IntGreaterThanEqual,
        FloatLessThan,
        FloatLessThanEqual,
        FloatGreaterThan,
        FloatGreaterThanEqual,
    }

    public unsafe struct Instruction
    {
        public fixed byte Data[4];

        public InstructionType Type => (InstructionType)Data[0];

        public byte ArgByte1 => Data[1];
        public byte ArgByte2 => Data[2];
        public byte ArgByte3 => Data[3];
        public ushort ArgShort
        {
            get
            {
                fixed (byte* ptr = Data) return *(ushort*)(ptr + 2);
            }
        }


        public int DataInt
        {
            get
            {
                fixed (byte* ptr = Data) return *(int*)ptr;
            }
        }

        public int DataShort1
        {
            get
            {
                fixed (byte* ptr = Data) return *(ushort*)(ptr + 0);
            }
        }

        public int DataShort2
        {
            get
            {
                fixed (byte* ptr = Data) return *(ushort*)(ptr + 2);
            }
        }

        public Instruction(InstructionType type)
        {
            Data[0] = (byte)type;
        }

        public Instruction(InstructionType type, ushort arg)
        {
            fixed (byte* ptr = Data)
            {
                *ptr = (byte)type;
                *(ushort*)(ptr + 2) = arg;
            }
        }

        public Instruction(InstructionType type, ushort shortArg, byte byteArg)
        {
            fixed (byte* ptr = Data)
            {
                *ptr = (byte)type;
                *(ptr + 1) = byteArg;
                *(ushort*)(ptr + 2) = shortArg;
            }
        }

        public Instruction(InstructionType type, byte arg1, byte arg2 = 0, byte arg3 = 0)
        {
            Data[0] = (byte)type;
            Data[1] = arg1;
            Data[2] = arg2;
            Data[3] = arg3;
        }

        public Instruction NewData(int data)
        {
            var instruction = new Instruction();
            byte* ptr = instruction.Data;
            *(int*)ptr = data;
            return instruction;
        }
    }

    public unsafe struct A
    {
        public fixed byte Data[1024];

        // blah methods
    }

    public unsafe struct B
    {
        public fixed byte Data[1024];

        // blah methods 

        public A AsStructA()
        {
            return Utility.UnsafeCast<A, B>(this);
        }
    }

    public unsafe static class Utility
    {
        public static unsafe T UnsafeCast<T, R>(R input) where T : unmanaged where R : unmanaged
        {
            return *(T*)&input;
        }
    }

    //public unsafe struct InstructionData
    //{
    //    public fixed byte Data[4];
    //    public int ArgInt => *(int*)Data[0];
    //    public short ArgShort1 => *(short*)Data[0];
    //    public short ArgShort2 => *(short*)Data[2];
    //    public byte ArgByte1 => Data[1];
    //    public byte ArgByte2 => Data[2];
    //    public byte ArgByte3 => Data[3];
    //    public byte ArgByte4 => Data[4];
    //}

    public struct Script
    {
        public BlobArray<byte> Code;
    }
}