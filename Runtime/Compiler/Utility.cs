using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public static partial class CompilerUtility
    {
        public static int[] ToIntArray<T>(T value) where T : struct
        {
            var wordLength = UnsafeUtility.SizeOf<T>();
            var array = new int[wordLength];
            unsafe
            {
                UnsafeUtility.CopyStructureToPtr(ref value, UnsafeUtility.AddressOf(ref array[0]));
            }
            return array;
        }

        public static int[] ToIntArray(string value)
        {
            var wordLength = GetWordLength(value.Length * sizeof(char));
            var array = new int[wordLength];
            unsafe
            {
                fixed (char* strPtr = value)
                {
                    fixed (int* arrayPtr = array)
                    {
                        UnsafeUtility.MemCpy(arrayPtr, strPtr, value.Length * sizeof(char));
                    }
                }
            }
            return array;
        }

        public static int GetWordLength<T>() where T : struct
        {
            return GetWordLength(UnsafeUtility.SizeOf<T>());
        }

        public static int GetWordLength(int byteLength)
        {
            return (byteLength + WordSize - 1) / WordSize;
        }
    }
}