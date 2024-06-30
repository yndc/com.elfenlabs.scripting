using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using System.Linq;

namespace Elfenlabs.Scripting
{
    public class Snapshot
    {
        public int[] Stack;
        public int[] Heap;
        public Snapshot(Machine.Snapshot snapshot)
        {
            Stack = snapshot.Stack.ToArray();
            Heap = snapshot.Heap.ToArray();
            snapshot.Dispose();
        }
    }

    public static partial class CompilerUtility
    {
        public static Snapshot Debug(params string[] sources)
        {
            var sb = new StringBuilder();
            var module = new Module(sources[0]);
            var compiler = new Compiler();

            new Tokenizer().Tokenize(module);
            UnityEngine.Debug.Log(Debug(module.Tokens));
            compiler.AddModule(module);
            var program = compiler.Build();
            for (var i = 0; i < program.Chunks.Length; i++)
            {
                sb.AppendLine("-- Chunk " + i);
                sb.AppendLine(Debug(program.Chunks[i]));
            }

            var machine = new Machine(1024, Allocator.Temp);
            machine.Boot(program);
            machine.Execute();

            var snapshot = machine.GetSnapshot(Allocator.Temp);
            sb.AppendLine("Stack");
            for (var i = 0; i < snapshot.Stack.Length; i++)
            {
                sb.AppendLine(snapshot.Stack[i].ToString());
            }
            sb.AppendLine();
            sb.AppendLine("Heap");
            sb.AppendLine(GenerateHexString(snapshot.Heap.Select(i => (byte)i).ToArray()));

            UnityEngine.Debug.Log(sb.ToString());

            machine.Dispose();

            return new Snapshot(snapshot);
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
                case Format.OSs:
                    text.Append(instruction.ArgSignedShort);
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
                case Format.OBSs:
                    text.Append(instruction.ArgSignedShort);
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

        public static unsafe string ToString(int[] data)
        {
            fixed (int* ptr = data)
            {
                return Encoding.UTF8.GetString((byte*)ptr, data.Length * sizeof(int));
            }
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