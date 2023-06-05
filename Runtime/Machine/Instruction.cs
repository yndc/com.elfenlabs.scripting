using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Elfenlabs.Scripting
{
    public enum InstructionType : byte
    {
        // --------------------------------
        // Control flow operations 
        // --------------------------------

        Halt,
        Yield,
        Jump,               // <short>    - The offset to jump to
        JumpIfFalse,        // <short>    - The offset to jump to if the condition is false
        JumpIfTrue,         // <short>    - The offset to jump to if the condition is true

        // --------------------------------
        // Stack operations
        // --------------------------------

        LoadConstant,       // <short>    - The index of the constant to load
                            // <byte>     - The number of words to load from the constant

        LoadVariable,       // <short>    - The index of the variable to load
                            // <byte>     - The number of words to load from the stack

        StoreVariable,      // <short>    - The index of the variable to store
                            // <byte>     - The number of words to store from the stack

        Pop,                // <short>    - The number of words to pop from the stack


        // --------------------------------
        // Value operations
        // --------------------------------

        IntAdd,
        IntSubstract,
        IntMultiply,
        IntDivide,
        IntModulo,
        IntNegate,
        FloatAdd,
        FloatSubstract,
        FloatMultiply,
        FloatDivide,
        FloatModulo,
        FloatNegate,
        BoolNegate,

        // --------------------------------
        // Comparison operations
        // --------------------------------

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

    // Instruction layout format (O = opcode, S = short, B = byte)
    public enum Format
    {
        O,
        OS,
        OBS,
        OBBB,
        I,
        SS,
        BBBB,
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
            set
            {
                fixed (byte* ptr = Data) *(ushort*)(ptr + 2) = value;
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

    public unsafe static class InstructionUtility
    {
        public static unsafe T UnsafeCast<T, R>(R input) where T : unmanaged where R : unmanaged
        {
            return *(T*)&input;
        }

        public static Dictionary<InstructionType, Format> InstructionFormats = new()
        {
            { InstructionType.Halt, Format.O },
            { InstructionType.Yield, Format.OS },
            { InstructionType.Jump, Format.OS },
            { InstructionType.JumpIfFalse, Format.OS },
            { InstructionType.JumpIfTrue, Format.OS },
            { InstructionType.LoadConstant, Format.OBS },
            { InstructionType.LoadVariable, Format.OBS },
            { InstructionType.StoreVariable, Format.OBS },
            { InstructionType.Pop, Format.OS },
            { InstructionType.IntAdd, Format.O },
            { InstructionType.IntSubstract, Format.O },
            { InstructionType.IntMultiply, Format.O },
            { InstructionType.IntDivide, Format.O },
            { InstructionType.IntModulo, Format.O },
            { InstructionType.IntNegate, Format.O },
            { InstructionType.FloatAdd, Format.O },
            { InstructionType.FloatSubstract, Format.O },
            { InstructionType.FloatMultiply, Format.O },
            { InstructionType.FloatDivide, Format.O },
            { InstructionType.FloatModulo, Format.O },
            { InstructionType.FloatNegate, Format.O },
            { InstructionType.BoolNegate, Format.O },
            { InstructionType.Equal, Format.O },
            { InstructionType.NotEqual, Format.O },
            { InstructionType.IntLessThan, Format.O },
            { InstructionType.IntLessThanEqual, Format.O },
            { InstructionType.IntGreaterThan, Format.O },
            { InstructionType.IntGreaterThanEqual, Format.O },
            { InstructionType.FloatLessThan, Format.O },
            { InstructionType.FloatLessThanEqual, Format.O },
            { InstructionType.FloatGreaterThan, Format.O },
            { InstructionType.FloatGreaterThanEqual, Format.O },
        };
    }

    public struct Script
    {
        public BlobArray<byte> Code;
    }
}