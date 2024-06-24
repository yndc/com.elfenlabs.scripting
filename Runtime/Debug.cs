using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;

namespace Elfenlabs.Scripting
{
    public static partial class CompilerUtility
    {
        public static int[] Debug(string sourceCode)
        {
            var log = new StringBuilder();
            var module = new Module(sourceCode);
            var compiler = new Compiler();

            new Tokenizer().Tokenize(module);
            UnityEngine.Debug.Log(Debug(module.Tokens));
            compiler.AddModule(module);
            var program = compiler.Build();
            for (var i = 0; i < program.Chunks.Length; i++)
            {
                log.AppendLine("-- Chunk " + i);
                log.AppendLine(Debug(program.Chunks[i]));
            }

            var machine = new Machine(1024, Allocator.Temp);
            machine.Boot(program);
            machine.Execute();

            // Snapshot the stack
            var stack = machine.GetStackSnapshot(Allocator.Temp);
            var snapshot = stack.ToArray();
            log.AppendLine("Stack");
            for (var i = 0; i < snapshot.Length; i++)
            {
                log.AppendLine(snapshot[i].ToString());
            }
            log.AppendLine();

            // Snapshot the heap
            var heap = machine.GetHeapSnapshot(Allocator.Temp);
            var heapSnapshot = heap.ToArray();
            log.AppendLine("Heap");
            log.AppendLine(GenerateHexString(heapSnapshot));

            machine.Dispose();
            stack.Dispose();

            UnityEngine.Debug.Log(log.ToString());

            return snapshot;
        }

        /// <summary>
        /// Debug tokens
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="ignoreFormatting"></param>
        /// <returns></returns>
        public static string Debug(LinkedList<Token> tokens, bool ignoreFormatting = false)
        {
            var text = new StringBuilder();
            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.NewLine: goto formatting;
                    case TokenType.Indent: goto formatting;
                    default:
                        text.Append(token.Value); break;
                    formatting: if (ignoreFormatting) continue; else break;
                }
                text.Append("\t");
                text.Append(token.Type.ToString());
                text.Append("\n");
            }

            return text.ToString();
        }

        /// <summary>
        /// Debug compiled bytecode
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string Debug(ByteCode code)
        {
            var text = new StringBuilder();
            var constants = code.Constants;
            text.Append("Constants\n");
            for (var i = 0; i < constants.Length; i++)
            {
                text.Append(constants[i]);
                text.Append("\t");
                if ((i + 1) % 4 == 0) text.Append("\n");
            }
            text.Append("\nInstructions\n");
            for (var ip = 0; ip < code.Instructions.Length; ip++)
            {
                var instruction = code.Instructions[ip];
                var format = InstructionUtility.InstructionFormats[instruction.Type];
                text.Append($"{ip:D4}\t{Debug(instruction, format)}");
            }

            return text.ToString();
        }

        public static string Debug(Instruction instruction, Format format)
        {
            var text = new StringBuilder();
            text.Append(instruction.Type.ToString());
            text.Append("\t");
            switch (format)
            {
                case Format.O:
                    break;
                case Format.OS:
                    text.Append(instruction.ArgShort);
                    break;
                case Format.OB:
                    text.Append(instruction.ArgByte);
                    break;
                case Format.OBS:
                    text.Append(instruction.ArgShort);
                    text.Append("\t");
                    text.Append(instruction.ArgByte1);
                    text.Append("\t");
                    break;
                case Format.OBBB:
                    text.Append(instruction.ArgByte1);
                    text.Append("\t");
                    text.Append(instruction.ArgByte2);
                    text.Append("\t");
                    text.Append(instruction.ArgByte3);
                    text.Append("\t");
                    break;
                case Format.I:
                    text.Append(instruction.DataInt);
                    break;
                case Format.SS:
                    text.Append(instruction.DataShort1);
                    text.Append("\t");
                    text.Append(instruction.DataShort2);
                    break;
            }
            text.Append("\n");

            return text.ToString();
        }

        public static unsafe string ToString(void* ptr)
        {
            return Encoding.UTF8.GetString((byte*)ptr + sizeof(int), *(int*)ptr);
        }

        public static unsafe string GenerateHexString(byte[] bytes, int wrap = 8)
        {
            var text = new StringBuilder();
            for (var i = 0; i < bytes.Length; i++)
            {
                text.Append(bytes[i].ToString("X2"));
                if ((i + 1) % wrap == 0)
                    text.Append("\n");
                else
                    text.Append(" ");
            }

            return text.ToString();
        }
    }
}