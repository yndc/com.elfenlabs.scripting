using System.Runtime.CompilerServices;
using TreeEditor;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine
    {
        public NativeList<int> Values;
        public NativeList<Frame> Frames;
        public int* stackHeadPtr;
        int* constantsPtr;

        /// <summary>
        /// Get the values stack size in words
        /// </summary>
        /// <returns></returns>
        public int GetStackWordLength()
        {
            return (int)(stackHeadPtr - Values.GetUnsafePtr());
        }

        /// <summary>
        /// Asserts that the values stack has at least the certain length. The value stack will be extended if needed.
        /// </summary>
        /// <param name="words"></param>
        public void EnsureStackCapacity(int additionalWords)
        {
            Values.ResizeUninitialized(GetStackWordLength() + additionalWords);
        }

        /// <summary>
        /// Removes a value from the stack without returning it
        /// </summary>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void Remove(int wordLen = 1)
        {
            stackHeadPtr -= wordLen;
        }

        /// <summary>
        /// Push a value directly to the stack.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void Push<T>(T value) where T : unmanaged
        {
            *(T*)stackHeadPtr = value;
            stackHeadPtr += CompilerUtility.GetWordLength<T>();
        }

        /// <summary>
        /// Pops a value from the stack.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe T Pop<T>(byte wordLen = 1) where T : unmanaged
        {
            stackHeadPtr -= wordLen;
            var value = *(T*)stackHeadPtr;
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
            var ptr = (T*)(stackHeadPtr - wordLen);
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
        unsafe void LoadConstant(ushort offset, byte wordLen)
        {
            EnsureStackCapacity(wordLen);
            UnsafeUtility.MemCpy(stackHeadPtr, constantsPtr + offset, wordLen * CompilerUtility.WordSize);
            stackHeadPtr += wordLen;
        }

        /// <summary>
        /// Pushes a variable value onto the stack.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void LoadStack(short offset, byte wordLen)
        {
            EnsureStackCapacity(wordLen);
            UnsafeUtility.MemCpy(stackHeadPtr, frameValuesPtr + offset, wordLen * CompilerUtility.WordSize);
            stackHeadPtr += wordLen;
        }

        /// <summary>
        /// Pushes a variable element value onto the stack using index on the top of the stack.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void LoadStackWithOffset(short offset, byte wordLen)
        {
            EnsureStackCapacity(wordLen);
            var index = Pop<int>();
            UnsafeUtility.MemCpy(stackHeadPtr, frameValuesPtr + offset + index, wordLen * CompilerUtility.WordSize);
            stackHeadPtr += wordLen;
        }

        /// <summary>
        /// Store the stack value onto a variable
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void WriteStack(short offset, byte wordLen)
        {
            UnsafeUtility.MemCpy(frameValuesPtr + offset, stackHeadPtr - wordLen, wordLen * CompilerUtility.WordSize);
            stackHeadPtr -= wordLen;
        }

        /// <summary>
        /// Fills the stack with zero values and moves the stack head
        /// </summary>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void FillZero(ushort wordLen)
        {
            EnsureStackCapacity(wordLen);
            UnsafeUtility.MemClear(stackHeadPtr, wordLen * CompilerUtility.WordSize);
            stackHeadPtr += wordLen;
        }

        /// <summary>
        /// Pop the stack value of the given word length and write it to the previous stack value with the given offset
        /// Overlapping is undefined behavior
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        unsafe void WritePrevious(ushort offset, byte wordLen)
        {
            UnsafeUtility.MemCpy(stackHeadPtr - offset - wordLen, stackHeadPtr - wordLen, wordLen * CompilerUtility.WordSize);
            stackHeadPtr -= wordLen;
        }
    }
}