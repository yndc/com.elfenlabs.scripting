using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Elfenlabs.Scripting
{
    public enum ExecutionState
    {
        Running,
        Yield,
        Halt,
    }

    public unsafe partial struct Machine
    {
        /// <summary>
        /// Returns the next instruction and increments the instruction pointer.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Instruction NextInstruction()
        {
            var instruction = *instructionPtr;
            instructionPtr++;
            return instruction;
        }

        /// <summary>
        /// Executes the program until it halts
        /// </summary>
        /// <returns></returns>
        public unsafe bool Execute()
        {
            while (true)
            {
                var instruction = NextInstruction();
                switch (instruction.Type)
                {
                    // Control flow
                    case InstructionType.Halt:
                        State = ExecutionState.Halt;
                        return true;
                    case InstructionType.Jump:
                        instructionPtr += instruction.ArgShort;
                        break;
                    case InstructionType.JumpIfFalse:
                        {
                            var value = Pop<bool>();
                            if (value == false)
                                instructionPtr += instruction.ArgShort;
                            break;
                        }
                    case InstructionType.Call:
                        {
                            Call(instruction.ArgShort, instruction.ArgByte1);
                            break;
                        }
                    case InstructionType.Return:
                        {
                            Return(instruction.ArgByte);
                            break;
                        }

                    // Stack operations
                    case InstructionType.LoadConstant:
                        LoadConstant(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.LoadVariable:
                        LoadVariable(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.LoadVariableElement:
                        LoadVariableElement(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.LoadHeap:
                        LoadHeap(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.StoreVariable:
                        StoreVariable(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.StoreHeap:
                        StoreHeap(instruction.ArgByte1);
                        break;
                    case InstructionType.WriteHeap:
                        WriteHeap(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.HeapLoadConstant:
                        HeapLoadConstant(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.Pop:
                        Remove(instruction.ArgShort);
                        break;
                    case InstructionType.FillZero:
                        FillZero(instruction.ArgShort);
                        break;
                    case InstructionType.WritePrevious:
                        WritePrevious(instruction.ArgShort, instruction.ArgByte1);
                        break;

                    // Integer arithmetic operations
                    case InstructionType.IntNegate:
                        {
                            ref var value = ref Unary<int>();
                            value = -value;
                            break;
                        }
                    case InstructionType.IntAdd:
                        {
                            ref var value = ref Binary<int>(out var other);
                            value += other;
                            break;
                        }
                    case InstructionType.IntSubstract:
                        {
                            ref var value = ref Binary<int>(out var other);
                            value -= other;
                            break;
                        }
                    case InstructionType.IntMultiply:
                        {
                            ref var value = ref Binary<int>(out var other);
                            value *= other;
                            break;
                        }
                    case InstructionType.IntDivide:
                        {
                            ref var value = ref Binary<int>(out var other);
                            value /= other;
                            break;
                        }

                    // Float arithmetic operations
                    case InstructionType.FloatNegate:
                        {
                            ref var value = ref Unary<float>();
                            value = -value;
                            break;
                        }
                    case InstructionType.FloatAdd:
                        {
                            ref var value = ref Binary<float>(out var other);
                            value += other;
                            break;
                        }
                    case InstructionType.FloatSubstract:
                        {
                            ref var value = ref Binary<float>(out var other);
                            value -= other;
                            break;
                        }
                    case InstructionType.FloatMultiply:
                        {
                            ref var value = ref Binary<float>(out var other);
                            value *= other;
                            break;
                        }

                    // Boolean operations
                    case InstructionType.BoolNegate:
                        {
                            ref var value = ref Unary<bool>();
                            value = !value;
                            break;
                        }

                    // Comparison operations (any up to 4 bytes)
                    case InstructionType.Equal:
                        {
                            ref var value = ref Binary<int>(out var other);
                            var temp = (value == other);
                            value = *(int*)&temp;
                            break;
                        }
                    case InstructionType.NotEqual:
                        {
                            ref var value = ref Binary<int>(out var other);
                            var temp = (value != other);
                            value = *(int*)&temp;
                            break;
                        }

                    // Comparison operations (int)
                    case InstructionType.IntGreaterThan:
                        {
                            ref var value = ref Binary<int>(out var other);
                            var temp = (value > other);
                            value = *(int*)&temp;
                            break;
                        }
                    case InstructionType.IntLessThan:
                        {
                            ref var value = ref Binary<int>(out var other);
                            var temp = (value < other);
                            value = *(int*)&temp;
                            break;
                        }
                    case InstructionType.IntGreaterThanEqual:
                        {
                            ref var value = ref Binary<int>(out var other);
                            var temp = (value >= other);
                            value = *(int*)&temp;
                            break;
                        }
                    case InstructionType.IntLessThanEqual:
                        {
                            ref var value = ref Binary<int>(out var other);
                            var temp = (value <= other);
                            value = *(int*)&temp;
                            break;
                        }

                    // Comparison operations (float)
                    case InstructionType.FloatGreaterThan:
                        {
                            ref var value = ref Binary<float>(out var other);
                            var temp = (value > other);
                            value = *(int*)&temp;
                            break;
                        }
                    case InstructionType.FloatLessThan:
                        {
                            ref var value = ref Binary<float>(out var other);
                            var temp = (value < other);
                            value = *(int*)&temp;
                            break;
                        }
                    case InstructionType.FloatGreaterThanEqual:
                        {
                            ref var value = ref Binary<float>(out var other);
                            var temp = (value >= other);
                            value = *(int*)&temp;
                            break;
                        }
                    case InstructionType.FloatLessThanEqual:
                        {
                            ref var value = ref Binary<float>(out var other);
                            var temp = (value <= other);
                            value = *(int*)&temp;
                            break;
                        }

                }
            }
        }
    }
}