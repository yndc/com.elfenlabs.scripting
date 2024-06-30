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
                        instructionPtr += instruction.ArgSignedShort;
                        break;
                    case InstructionType.JumpCondition:
                        {
                            var value = Pop<bool>();
                            if (value == *(bool*)&instruction.Data[1])
                                instructionPtr += instruction.ArgSignedShort;
                            break;
                        }
                    case InstructionType.Call:
                        {
                            Call(instruction.ArgShort, instruction.ArgByte1);
                            break;
                        }
                    case InstructionType.CallExternal:
                        {
                            CallExternal(instruction.ArgShort);
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
                    case InstructionType.VariableIncrement:
                        {
                            frameValuesPtr[instruction.ArgShort]++;
                            break;
                        }
                    case InstructionType.VariableDecrement:
                        {
                            frameValuesPtr[instruction.ArgShort]--;
                            break;
                        }

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
                    case InstructionType.IntModulo:
                        {
                            ref var value = ref Binary<int>(out var other);
                            value %= other;
                            break;
                        }

                    // String operations
                    case InstructionType.StringConcatenate:
                        {
                            var rhsHeapIndex = Pop<int>();
                            var lhsHeapIndex = *(stackHeadPtr - 1);
                            var lhsLen = *(heapPtr + lhsHeapIndex);
                            var rhsLen = *(heapPtr + rhsHeapIndex);
                            var newLen = lhsLen + rhsLen;
                            var newWordLen = 1 + CompilerUtility.GetWordLength(newLen);
                            if (!heap.TryExpand(lhsHeapIndex, newWordLen, out var newHeapIndex))
                            {
                                *(heapPtr + newHeapIndex) = newLen;
                                UnsafeUtility.MemCpy(
                                    heapPtr + newHeapIndex + 1,
                                    heapPtr + lhsHeapIndex + 1,
                                    lhsLen);
                                UnsafeUtility.MemCpy(
                                    ((byte*)(heapPtr + newHeapIndex + 1)) + lhsLen,
                                    heapPtr + rhsHeapIndex + 1,
                                    rhsLen);
                            }
                            *(stackHeadPtr - 1) = newHeapIndex;
                            //else
                            //{
                            //    var newDestPtr = heapPtr + newHeapIndex;
                            //    *newDestPtr = newLen;
                            //    UnsafeUtility.MemCpy(
                            //        ((byte*)(newDestPtr + 1)) + lhsLen,
                            //        heapPtr + rhsHeapIndex + 1,
                            //        rhsLen + 5);
                            //}
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

                    // --------------------------------
                    // Conversion operations
                    // --------------------------------
                    case InstructionType.ConvertIntToString:
                        {
                            var heapIndex = heap.Allocate(2);
                            *(heapPtr + heapIndex) = 1;
                            *(heapPtr + heapIndex + 1) = *(stackHeadPtr - 1);
                            *(stackHeadPtr - 1) = heapIndex;
                            break;
                        }
                }
            }
        }
    }
}