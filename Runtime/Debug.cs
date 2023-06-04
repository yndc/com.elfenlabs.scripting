using System.Collections.Generic;
using System.Text;
using Unity.Collections;

namespace Elfenlabs.Scripting
{
    public static partial class CompilerUtility
    {
        public static int[] Debug(string sourceCode)
        {
            var tokens = Tokenizer.Tokenize(sourceCode);
            UnityEngine.Debug.Log(Debug(tokens));
            var code = Compiler.Compile(tokens);
            UnityEngine.Debug.Log(Debug(code));
            var machine = new Machine(1024, Allocator.Temp);
            machine.Insert(code);
            machine.Run();
            var stack = machine.GetStackSnapshot(Allocator.Temp);
            var snapshot = stack.ToArray();
            UnityEngine.Debug.Log("-- Stack:");
            for (var i = 0; i < snapshot.Length; i++)
            {
                UnityEngine.Debug.Log(snapshot[i]);
            }

            machine.Dispose();
            stack.Dispose();

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
        public static string Debug(Code code)
        {
            var text = new StringBuilder();
            var constants = code.Constants;
            text.Append("-- Constants:\n");
            for (var i = 0; i < constants.Length; i++)
            {
                text.Append(constants[i]);
                text.Append("\t");
                if ((i + 1) % 4 == 0) text.Append("\n");
            }
            text.Append("\n-- Instructions:\n");
            for (var ip = 0; ip < code.Instructions.Length; ip++)
            {
                var instruction = code.Instructions[ip];
                switch (instruction.Type)
                {
                    case InstructionType.LoadConstant:
                        var index = instruction.ArgShort;
                        var size = instruction.ArgByte1;
                        text.Append("Load");
                        text.Append("\t");
                        text.Append(index);
                        text.Append("\t");
                        text.Append(size);
                        text.Append("\n");
                        break;
                    default: text.Append(instruction.Type.ToString()); text.Append("\n"); break;
                }
            }

            return text.ToString();
        }
    }
}