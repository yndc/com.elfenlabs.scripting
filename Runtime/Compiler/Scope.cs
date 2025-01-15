using System.Collections.Generic;
using System;

namespace Elfenlabs.Scripting
{
    public class Scope
    {
        /// <summary>
        /// Parent of this scope
        /// </summary>
        public Scope Parent;

        /// <summary>
        /// Variables declared in this scope
        /// </summary>
        public Dictionary<string, Variable> Variables = new();

        /// <summary>
        /// Functions declared in this scope
        /// </summary>
        public Dictionary<string, FunctionHeader> Functions = new();

        /// <summary>
        /// Scope depth with 0 as the global scope
        /// </summary>
        public int Depth;

        /// <summary>
        /// Offset from a nearest frame
        /// </summary>
        public int FrameOffset;

        /// <summary>
        /// Length of all variables in this scope
        /// </summary>
        public short WordLength;

        /// <summary>
        /// Flags if this scope is a function frame
        /// </summary>
        public bool IsFrame;

        /// <summary>
        /// Declares a new variable in this scope
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public short DeclareVariable(string name, Type type)
        {
            var offset = (short)(FrameOffset + WordLength);
            var variable = new Variable(type, name, this, offset);

            if (!Variables.TryAdd(name, variable))
                throw new Exception($"Variable {name} already declared in this scope");

            WordLength += type.WordLength;

            return offset;
        }

        /// <summary>
        /// Declares a new function in this scope
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public FunctionHeader DeclareFunction(FunctionHeader function)
        {
            if (!Functions.TryAdd(function.Name, function))
                throw new Exception($"Function {function.Name} already declared in this scope");

            return function;
        }

        /// <summary>
        /// Try getting a variable by name, if a variable is not found in this scope, it will try to find it in the parent scope
        /// </summary>
        /// <param name="name"></param>
        /// <param name="variable"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public bool TryGetVariable(string name, out Variable variable)
        {
            if (Variables.TryGetValue(name, out variable))
                return true;

            if (Parent != null && Parent.TryGetVariable(name, out variable))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Try getting a function by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public bool TryGetFunction(string name, out FunctionHeader function)
        {
            if (Functions.TryGetValue(name, out function))
                return true;

            if (Parent != null)
                return Parent.TryGetFunction(name, out function);

            return false;
        }

        public Scope CreateChild(bool isFrame = false)
        {
            var child = new Scope { Parent = this, Depth = Depth + 1, IsFrame = isFrame };
            if (!isFrame)
            {
                child.FrameOffset = FrameOffset + WordLength;
            }
            return child;
        }
    }

    public partial class Compiler
    {
        void ConsumeBlock()
        {
            var depth = currentScope.Depth;
            while (true)
            {
                if (!TryConsumeIndents(depth))
                    return;

                ConsumeStatement();
            }
        }

        void BeginScope()
        {
            currentScope = currentScope.CreateChild();
        }

        void EndScope()
        {
            CodeBuilder.Add(new Instruction(InstructionType.Pop, currentScope.WordLength));
            currentScope = currentScope.Parent;
        }

        bool TryConsumeIndents(int count)
        {
            var cursor = current;
            for (int i = 0; i < count; i++)
            {
                if (LookAhead(i)?.Type != TokenType.Indent)
                    return false;
            }

            for (int i = 0; i < count; i++)
            {
                Consume(TokenType.Indent, "Expected indent");
            }

            return true;
        }

        Token LookAhead(int offset)
        {
            var token = current;
            for (int i = 0; i < offset; i++)
            {
                if (token.Next == null)
                    return null;
                token = token.Next;
            }
            return token.Value;
        }
    }
}