using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System.Runtime.CompilerServices;

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
            var instruction = Code.Instructions[InstructionPointer];
            InstructionPointer++;
            return instruction;
        }

        /// <summary>
        /// Prepare the virtual machine with the given code
        /// </summary>
        /// <param name="code"></param>
        public unsafe void Insert(ByteCode code)
        {
            Code = code;
            StackPointer = 0;
            InstructionPointer = 0;
            State = ExecutionState.Running;
        }

        public unsafe bool Run(EnvironmentState state = default)
        {
            switch (State)
            {
                case ExecutionState.Halt:
                    return true;
                case ExecutionState.Yield:
                    if (state.Time - YieldStartTime > YieldDuration)
                    {
                        State = ExecutionState.Running;
                        goto case ExecutionState.Running;
                    }
                    return false;
                case ExecutionState.Running:
                    return Execute(state);
            }

            return true;
        }

        unsafe bool Execute(EnvironmentState env)
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
                    case InstructionType.Yield:
                        State = ExecutionState.Yield;
                        YieldStartTime = env.Time;
                        YieldDuration = instruction.ArgShort;
                        return false;
                    case InstructionType.Jump:
                        InstructionPointer += instruction.ArgShort;
                        break;
                    case InstructionType.JumpIfFalse:
                        {
                            var value = Pop<bool>();
                            if (value == false)
                                InstructionPointer += instruction.ArgShort;
                            break;
                        }

                    // Stack operations
                    case InstructionType.LoadConstant:
                        PushConstant(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.LoadVariable:
                        PushVariable(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.StoreVariable:
                        StoreVariable(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.Pop:
                        Remove(instruction.ArgShort);
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