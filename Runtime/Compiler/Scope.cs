using System.Collections.Generic;
using System;

namespace Elfenlabs.Scripting
{
    public class Scope
    {
        public Scope Parent;
        public Dictionary<string, Variable> Variables = new();
        public Dictionary<string, FunctionHeader> Functions = new();
        public int Depth;
        public ushort WordLength;
        public bool IsFunction;

        /// <summary>
        /// Declares a new variable in this scope
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public ushort DeclareVariable(string name, ValueType type)
        {
            var variable = new Variable(name, type, WordLength);

            if (!Variables.TryAdd(name, variable))
                throw new Exception($"Variable {name} already declared in this scope");

            WordLength += type.WordLength;

            return variable.Position;
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
        /// Try getting a variable by name, will out the variable with relative offset from the this scope
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
                //if (IsFunction)
                    //variable.Position -= Parent.WordLength;
                return true;
            }

            return false;
        }

        public bool TryGetFunction(string name, out FunctionHeader function)
        {
            if (Functions.TryGetValue(name, out function))
                return true;

            if (Parent != null)
                return Parent.TryGetFunction(name, out function);

            return false;
        }

        public Scope CreateChild(bool isFunction = false)
        {
            return new Scope { Parent = this, Depth = Depth + 1, IsFunction = isFunction };
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