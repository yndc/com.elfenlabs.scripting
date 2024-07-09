using System.Collections.Generic;
using Unity.Entities;

namespace Elfenlabs.Scripting
{
    public enum InstructionType : byte
    {
        // --------------------------------
        // Control flow operations 
        // --------------------------------

        /// <summary>
        /// Halts the execution of the script
        /// </summary>
        Halt,
        Yield,

        /// <summary>
        /// Jump the instruction pointer to the given offset
        /// <br/>
        /// <br/>arg short  : Instruction offset to jump into
        /// </summary>
        Jump,

        /// <summary>
        /// Jump the instruction pointer to the given offset if the top of the stack 
        /// is equal with the given byte argument
        /// <br/>
        /// <br/>arg short  : Instruction offset to jump into
        /// <br/>arg byte   : The boolean value to check with the top of the stack
        /// </summary>
        JumpCondition,

        /// <summary>
        /// Calls a function with the given index
        /// <br/>
        /// <br/>arg ushort  : Index of the function to call
        /// <br/>arg byte    : Length in words from the top of the stack to pass as arguments
        /// </summary>
        Call,

        /// <summary>
        /// Returns to the previous frame 
        /// <br/>
        /// <br/>arg byte    : Length in words from the top of the stack to return 
        /// </summary>
        Return,

        // --------------------------------
        // Stack operations
        // --------------------------------

        /// <summary>
        /// Copies constant values from the given position to the top of the stack.
        /// <br/>
        /// <br/>arg short  : Starting word offset from the base constant pointer
        /// <br/>arg byte   : Length in words to copy
        /// </summary>
        LoadConstant,

        /// <summary>
        /// Copies the stack values from the given position to the top of the stack.
        /// <br/>
        /// <br/>arg short  : Starting word offset from the current frame pointer
        /// <br/>arg byte   : Length in words to copy from the stack
        /// </summary>
        LoadStack,

        /// <summary>
        /// Pops a word from the stack, then copies the stack values from the 
        /// given offset plus the popped value to the top of the stack.
        /// <br/>
        /// <br/>arg short  : Starting word offset from the current frame pointer
        /// <br/>arg byte   : Length in words to copy from the stack
        /// </summary>
        LoadStackWithOffset,

        LoadHeap,           // <short>    - The index of the heap to load
                            // <byte>     - The number of words to load from the heap   

        WriteStack,      // <short>    - The index of the variable to store
                            // <byte>     - The number of words to store from the stack

        WriteHeap,          // <short>    - The index of the heap to store
                            // <byte>     - The number of words to store from the stack

        Pop,                // <short>    - The number of words to pop from the stack

        FillZero,           // <short>    - The number of words to fill with zeros

        WritePrevious,      // <short>    - The offset in words
                            // <byte>     - The number of words to copy from the stack

        // --------------------------------
        // Heap operations
        // --------------------------------

        HeapLoadConstant,   // <short>    - The index of the constant to store
                            // <byte>     - The number of words to store from the constant

        // --------------------------------
        // External operations
        // --------------------------------

        CallExternal,       // <short>    - The index of the external function to call

        // --------------------------------
        // Value operations
        // --------------------------------

        IntAdd,
        IntSubstract,
        IntMultiply,
        IntPower,
        IntDivide,
        IntModulo,
        IntNegate,
        VariableIncrement,
        VariableDecrement,
        FloatAdd,
        FloatSubstract,
        FloatMultiply,
        FloatPower,
        FloatDivide,
        FloatModulo,
        FloatNegate,
        BoolNegate,

        // --------------------------------
        // Conversion operations
        // --------------------------------
        ConvertIntToFloat,
        ConvertFloatToInt,
        ConvertIntToString,
        ConvertFloatToString,

        // --------------------------------
        // String operations
        // --------------------------------
        StringConcatenate,

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
        OSs,
        OB,
        OBS,
        OBSs,
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
        public short ArgSignedShort
        {
            get
            {
                fixed (byte* ptr = Data) return *(short*)(ptr + 2);
            }
            set
            {
                fixed (byte* ptr = Data) *(short*)(ptr + 2) = value;
            }
        }
        public byte ArgByte => Data[2];


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

        public Instruction(InstructionType type, short signedShortArg)
        {
            fixed (byte* ptr = Data)
            {
                *ptr = (byte)type;
                *(short*)(ptr + 2) = signedShortArg;
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

        public Instruction(InstructionType type, short signedShortArg, byte byteArg)
        {
            fixed (byte* ptr = Data)
            {
                *ptr = (byte)type;
                *(ptr + 1) = byteArg;
                *(short*)(ptr + 2) = signedShortArg;
            }
        }

        public Instruction(InstructionType type, byte arg)
        {
            Data[0] = (byte)type;
            Data[2] = arg;
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
            { InstructionType.Jump, Format.OSs },
            { InstructionType.JumpCondition, Format.OBSs },
            { InstructionType.Call, Format.OBS },
            { InstructionType.Return, Format.OB },
            { InstructionType.LoadConstant, Format.OBS },
            { InstructionType.LoadStack, Format.OBSs },
            { InstructionType.LoadStackWithOffset, Format.OBSs },
            { InstructionType.WriteStack, Format.OBSs },
            { InstructionType.Pop, Format.OS },
            { InstructionType.FillZero, Format.OS },
            { InstructionType.WritePrevious, Format.OBS },
            { InstructionType.LoadHeap, Format.OBS },
            { InstructionType.WriteHeap, Format.OBS },
            { InstructionType.HeapLoadConstant, Format.OBS},
            { InstructionType.CallExternal, Format.OS},
            { InstructionType.IntAdd, Format.O },
            { InstructionType.IntSubstract, Format.O },
            { InstructionType.IntMultiply, Format.O },
            { InstructionType.IntPower, Format.O },
            { InstructionType.IntDivide, Format.O },
            { InstructionType.IntModulo, Format.O },
            { InstructionType.IntNegate, Format.O },
            { InstructionType.ConvertIntToFloat, Format.O },
            { InstructionType.ConvertFloatToInt, Format.O },
            { InstructionType.ConvertIntToString, Format.O },
            { InstructionType.ConvertFloatToString, Format.O },
            { InstructionType.StringConcatenate, Format.O },
            { InstructionType.VariableIncrement, Format.O },
            { InstructionType.VariableDecrement, Format.O },
            { InstructionType.FloatAdd, Format.O },
            { InstructionType.FloatSubstract, Format.O },
            { InstructionType.FloatMultiply, Format.O },
            { InstructionType.FloatPower, Format.O },
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