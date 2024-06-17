using System.Runtime.CompilerServices;
using TreeEditor;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine
    {
        /// <summary>
        /// Get the values stack size in words
        /// </summary>
        /// <returns></returns>
        public int GetStackWordLength()
        {
            return (int)(valuesPtr - Values.GetUnsafePtr());
        }

        /// <summary>
        /// Asserts that the values stack has at least the certain length. The value stack will be extended if needed.
        /// </summary>
        /// <param name="words"></param>
        public void AssertValueStackWordLength(int additionalWords)
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
            valuesPtr -= wordLen;
        }

        /// <summary>
        /// Pops a value from the stack.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe T Pop<T>(byte wordLen = 1) where T : unmanaged
        {
            valuesPtr -= wordLen;
            var value = *(T*)valuesPtr;
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
            var ptr = (T*)(valuesPtr - wordLen);
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
            AssertValueStackWordLength(wordLen);
            UnsafeUtility.MemCpy(valuesPtr, constantsPtr + offset, wordLen * CompilerUtility.WordSize);
            valuesPtr += wordLen;
        }

        /// <summary>
        /// Pushes a variable value onto the stack.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void LoadVariable(ushort offset, byte wordLen)
        {
            AssertValueStackWordLength(wordLen);
            UnsafeUtility.MemCpy(valuesPtr, frameValuesPtr + offset, wordLen * CompilerUtility.WordSize);
            valuesPtr += wordLen;
        }

        /// <summary>
        /// Store the stack value onto a variable
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void StoreVariable(ushort offset, byte wordLen)
        {
            UnsafeUtility.MemCpy(frameValuesPtr + offset, valuesPtr - wordLen, wordLen * CompilerUtility.WordSize);
            valuesPtr -= wordLen;
        }
    }
}