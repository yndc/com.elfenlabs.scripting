using System.Collections.Generic;
using static Unity.Entities.SystemBaseDelegates;

namespace Elfenlabs.Scripting
{
    public class StructureValueType : ValueType
    {
        public class Field
        {
            public string Name;
            public ValueType Type;
            public byte Offset;
        }

        public List<Field> Fields = new();

        public StructureValueType(string name) : base(name, 0) { }

        /// <summary>
        /// Try getting field type info from a field name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetFieldByName(string name, out Field result)
        {
            foreach (var field in Fields)
            {
                if (field.Name == name)
                {
                    result = field;
                    return true;
                }
            }
            result = null;
            return false;
        }

        /// <summary>
        /// Adds a new field to the structure
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public void AddField(string name, ValueType type)
        {
            Fields.Add(new Field { Name = name, Type = type, Offset = WordLength });
            WordLength += type.WordLength;
        }

        /// <summary>
        /// Adds a new method to the structure
        /// </summary>
        /// <param name="function"></param>
        public void AddMethod(FunctionHeader function)
        {
            Methods.Add(function);
        }
    }

    public partial class Compiler
    {
        void ConsumeStructureDeclaration()
        {
            Consume(TokenType.Structure);

            var name = Consume(TokenType.Identifier).Value;
            var type = new StructureValueType(name);

            Consume(TokenType.StatementTerminator, "Expected new-line after structure name");

            currentScope = currentScope.CreateChild();

            while (true)
            {
                if (!TryConsumeIndents(currentScope.Depth))
                    break;

                ConsumeStructureMemberDeclaration(type);
            }

            currentScope = currentScope.Parent;

            RegisterType(type);
        }

        void ConsumeStructureMemberDeclaration(StructureValueType type)
        {
            switch (current.Value.Type)
            {
                case TokenType.Field:
                    ConsumeStructureFieldDeclaration(type);
                    break;
                case TokenType.Function:
                    ConsumeStructureFunctionDeclaration(type);
                    break;
                default:
                    throw new CompilerException(current.Value, $"Expected structure member declaration. Received: {current.Value}");
            }
        }

        void ConsumeStructureFieldDeclaration(StructureValueType type)
        {
            Consume(TokenType.Field);
            var fieldNname = Consume(TokenType.Identifier).Value;
            var fieldType = ConsumeType();
            type.AddField(fieldNname, fieldType);
            Consume(TokenType.StatementTerminator, "Expected new-line after structure field declaration");
        }

        void ConsumeStructureFunctionDeclaration(StructureValueType type)
        {
            var function = ConsumeFunctionDeclarationHeader();

            // Add 'self' variable as a reference to this structure
            function.Parameters.Insert(0, new FunctionHeader.Parameter("self", new ReferenceType(type)));

            var subProgram = new SubProgram(function);
            RegisterSubProgram(subProgram);
            ConsumeFunctionBody(subProgram);
        }

        void ConsumeStructLiteral(StructureValueType type)
        {
            Consume(TokenType.LeftBrace);
            Skip(TokenType.StatementTerminator);

            // Add the default value for the structure first for the layout
            // Then we compute each field value and only then we write the fields one by one
            CodeBuilder.Add(new Instruction(InstructionType.FillZero, (ushort)type.WordLength));

            var literalFields = new List<StructureValueType.Field>();
            var assignedFields = new HashSet<StructureValueType.Field>();
            while (current.Value.Type != TokenType.RightBrace)
            {
                Skip(TokenType.Indent);
                var field = ConsumeStructLiteralField(type, assignedFields);
                literalFields.Add(field);
                Skip(TokenType.StatementTerminator);
            }

            Consume(TokenType.RightBrace);
        }

        StructureValueType.Field ConsumeStructLiteralField(StructureValueType type, HashSet<StructureValueType.Field> assignedFields)
        {
            var fieldName = Consume(TokenType.Identifier).Value;
            if (!type.TryGetFieldByName(fieldName, out var field))
                throw CreateException(previous.Value, $"Field {fieldName} does not exist in structure {type.Identifier}");
            if (assignedFields.Contains(field))
                throw CreateException(previous.Value, $"Field {fieldName} already assigned in structure literal");

            Consume(TokenType.Equal);

            var expressionType = ConsumeExpression();
            if (expressionType != field.Type)
                throw CreateException(previous.Value, $"Cannot assign {expressionType.Identifier} to {field.Type.Identifier}");

            CodeBuilder.Add(new Instruction(InstructionType.WritePrevious, (ushort)(type.WordLength - field.Offset), field.Type.WordLength));

            return field;
        }
    }
}