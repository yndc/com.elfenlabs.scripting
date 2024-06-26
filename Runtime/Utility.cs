using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;

namespace Elfenlabs.Scripting
{
    public static partial class CompilerUtility
    {
        public const int WordSize = 4;

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

        public static string GetLineString(Module module, int line)
        {
            var cursor = 0;
            for (var l = 1; l < line; l++)
            {
                while (cursor < module.Source.Length)
                {
                    if (module.Source[cursor] == '\n')
                    {
                        cursor++;
                        break;
                    }
                    cursor++;
                }
            }

            var lineStart = cursor;
            while (cursor < module.Source.Length)
            {
                if (module.Source[cursor] == '\n')
                    break;
                cursor++;
            }
            var lineEnd = cursor;

            if (lineEnd < 0)
                return string.Empty;
            var lineSource = module.Source[lineStart..lineEnd];
            return lineSource;
        }

        public static string GenerateLinePointer(int col, int length = 1)
        {
            return new string(' ', Math.Max(0, col - 1)) + new string('^', Math.Max(1, length));
        }

        public static string GenerateCodeTokenPointer(Token token, int previousLines = 0)
        {
            return GenerateCodeTokenPointer(token.Module, token.Line, token.Column, token.Length, previousLines);
        }

        public static string GenerateCodeTokenPointer(Module module, int line, int col, int length = 1, int previousLines = 0)
        {
            var sb = new StringBuilder();
            for (var i = Math.Max(1, line - previousLines); i <= line; i++)
            {
                sb.AppendLine($"{i,-4} | " + GetLineString(module, i));
            }
            sb.AppendLine(new string(' ', 7) + GenerateLinePointer(col, length));
            return sb.ToString();
        }
    }
}