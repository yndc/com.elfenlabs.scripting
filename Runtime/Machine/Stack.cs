using System.Runtime.CompilerServices;
using TreeEditor;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine
    {
        public NativeList<int> Stack;
        public NativeList<Frame> Frames;
        public int* stackHeadPtr;
        int* constantsPtr;

        /// <summary>
        /// Get the values stack size in words
        /// </summary>
        /// <returns></returns>
        public int GetStackWordLength()
        {
            return (int)(stackHeadPtr - Stack.GetUnsafePtr());
        }

        /// <summary>
        /// Asserts that the values stack has at least the certain length. The value stack will be extended if needed.
        /// </summary>
        /// <param name="words"></param>
        public void EnsureStackCapacity(int additionalWords)
        {
            Stack.ResizeUninitialized(GetStackWordLength() + additionalWords);
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
            UnsafeUtility.MemCpy(stackHeadPtr, stackFramePtr + offset, wordLen * CompilerUtility.WordSize);
            stackHeadPtr += wordLen;
        }

        /// <summary>
        /// Push the stack address for the current frame plus offset argument
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void LoadStackAddress(short offset)
        {
            *stackHeadPtr = (int)(stackFramePtr + offset - Stack.GetUnsafePtr());
            stackHeadPtr += 1;
        }

        /// <summary>
        /// Pushes a stack value from an address onto the stack.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void LoadFromStackAddress(short offset, byte wordLen)
        {
            EnsureStackCapacity(wordLen);
            var addressPtr = stackHeadPtr - 1;
            var address = *(addressPtr) + offset;
            UnsafeUtility.MemCpy(addressPtr, Stack.GetUnsafePtr() + address, wordLen * CompilerUtility.WordSize);
            stackHeadPtr += wordLen - 1;
        }

        /// <summary>
        /// Store the stack value onto a variable
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void Store(short offset, byte wordLen)
        {
            UnsafeUtility.MemCpy(stackFramePtr + offset, stackHeadPtr - wordLen, wordLen * CompilerUtility.WordSize);
            stackHeadPtr -= wordLen;
        }

        /// <summary>
        /// Store the stack value onto a ref
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void StoreRef(short offset, byte wordLen)
        {
            var address = *(stackHeadPtr - wordLen - 1);
            UnsafeUtility.MemCpy(Stack.GetUnsafePtr() + address + offset, stackHeadPtr - wordLen, wordLen * CompilerUtility.WordSize);
            stackHeadPtr -= (wordLen + 1);
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
    }
}