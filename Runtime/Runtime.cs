using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System.Runtime.CompilerServices;

namespace Yonderlabs.Scripting
{
    public enum ExecutionState
    {
        Running,
        Yield,
        Halt,
    }

    public struct EnvironmentState
    {
        public float DeltaTime;
        public float Time;
    }

    public unsafe partial struct ScriptExecutionState
    {
        public int InstructionPointer;
        public int StackPointer;
        public float YieldDuration;
        public float YieldStartTime;
        public NativeList<int> Stack;
        public ExecutionState State;
        public Code Code;

        int* stackPtr;

        public ScriptExecutionState(Code code, int stackCapacity, Allocator allocator)
        {
            Stack = new NativeList<int>(stackCapacity, allocator);
            InstructionPointer = 0;
            StackPointer = 0;
            YieldDuration = 0f;
            YieldStartTime = 0f;
            State = ExecutionState.Running;
            Code = code;

            stackPtr = Stack.GetUnsafePtr();
        }

        public unsafe void Execute(EnvironmentState state)
        {
            switch (State)
            {
                case ExecutionState.Halt:
                    return;
                case ExecutionState.Yield:
                    if (state.Time - YieldStartTime > YieldDuration)
                    {
                        State = ExecutionState.Running;
                        goto case ExecutionState.Running;
                    }
                    return;
                case ExecutionState.Running:
                    Run(state);
                    return;
            }
        }

        public NativeArray<int>.ReadOnly GetStack()
        {
            return Stack.AsArray().AsReadOnly();
        }

        public T ReadStackAs<T>() where T : unmanaged
        {
            return *(T*)stackPtr;
        }

        public NativeArray<int> GetStackSnapshot(Allocator allocator)
        {
            var stack = new NativeArray<int>(StackPointer, allocator);
            UnsafeUtility.MemCpy(stack.GetUnsafePtr(), stackPtr, StackPointer * sizeof(int));
            return stack;
        }

        unsafe void Run(EnvironmentState env)
        {
            while (true)
            {
                var instruction = NextInstruction();
                switch (instruction.Type)
                {
                    // Control flow
                    case InstructionType.Halt:
                        State = ExecutionState.Halt;
                        return;
                    case InstructionType.Yield:
                        State = ExecutionState.Yield;
                        YieldStartTime = env.Time;
                        YieldDuration = instruction.ArgShort;
                        return;

                    // Stack operations
                    case InstructionType.LoadConstant:
                        PushConstant(instruction.ArgShort, instruction.ArgByte1);
                        break;
                    case InstructionType.LoadVariable:
                        PushVariable(instruction.ArgShort, instruction.ArgByte1);
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
        /// Removes a value from the stack without returning it
        /// </summary>
        /// <param name="wordLen"></param>
        unsafe void Remove(int wordLen = 1)
        {
            StackPointer -= wordLen;
        }

        /// <summary>
        /// Pops a value from the stack.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe T Pop<T>(byte wordLen = 1) where T : unmanaged
        {
            StackPointer -= wordLen;
            var value = *(T*)(stackPtr + StackPointer);
            return value;
        }

        /// <summary>
        /// Returns a reference to the top of the stack.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe ref T Peek<T>(int wordLen = 1) where T : unmanaged
        {
            var ptr = (T*)(stackPtr + StackPointer - wordLen);
            return ref *ptr;
        }

        /// <summary>
        /// Pops a value from the stack and returns a reference to the top of the stack. 
        /// Used to implement binary operations.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="other"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe ref T Binary<T>(out T other) where T : unmanaged
        {
            other = Pop<T>();
            return ref Peek<T>();
        }

        /// <summary>
        /// Returns a reference to the top of the stack.
        /// Used to implement unary operations.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe ref T Unary<T>() where T : unmanaged
        {
            return ref Peek<T>();
        }

        /// <summary>
        /// Pushes a constant value onto the stack.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void PushConstant(ushort offset, byte wordLen)
        {
            Stack.ResizeUninitialized(StackPointer + wordLen);
            UnsafeUtility.MemCpy(stackPtr + StackPointer, (int*)Code.Constants.GetUnsafeReadOnlyPtr() + offset, wordLen);
            StackPointer += wordLen;
        }

        /// <summary>
        /// Pushes a variable value onto the stack.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void PushVariable(ushort offset, byte wordLen)
        {
            Stack.ResizeUninitialized(StackPointer + wordLen);
            UnsafeUtility.MemCpy(stackPtr + StackPointer, stackPtr + offset, wordLen);
            StackPointer += wordLen;
        }
    }
}