using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public unsafe partial struct Machine
    {
        /// <summary>
        /// Removes a value from the stack without returning it
        /// </summary>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void Remove(int wordLen = 1)
        {
            ValueStackPointer -= wordLen;
        }

        /// <summary>
        /// Pops a value from the stack.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe T Pop<T>(byte wordLen = 1) where T : unmanaged
        {
            ValueStackPointer -= wordLen;
            var value = *(T*)(stackPtr + ValueStackPointer);
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
            var ptr = (T*)(stackPtr + ValueStackPointer - wordLen);
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
            Values.ResizeUninitialized(ValueStackPointer + wordLen);
            UnsafeUtility.MemCpy(stackPtr + ValueStackPointer, constantsPtr + offset, wordLen * CompilerUtility.WordSize);
            ValueStackPointer += wordLen;
        }

        /// <summary>
        /// Pushes a variable value onto the stack.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void LoadVariable(ushort offset, byte wordLen)
        {
            Values.ResizeUninitialized(ValueStackPointer + wordLen);
            UnsafeUtility.MemCpy(stackPtr + ValueStackPointer, stackPtr + offset, wordLen * CompilerUtility.WordSize);
            ValueStackPointer += wordLen;
        }

        /// <summary>
        /// Store the stack value onto a variable
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="wordLen"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void StoreVariable(ushort offset, byte wordLen)
        {
            UnsafeUtility.MemCpy(stackPtr + offset, stackPtr + ValueStackPointer - wordLen, wordLen * CompilerUtility.WordSize);
            ValueStackPointer -= wordLen;
        }
    }
}