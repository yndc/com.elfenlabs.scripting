using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    public class StructureType : Type
    {
        public class Field
        {
            public string Name;
            public Type Type;
            public short Offset;
        }

        public List<Field> Fields = new();

        public List<PlaceholderType> TypePlaceholders = new();

        public TypeArguments ResolvedTypes;

        public StructureType(string name) : base(name, 0) { }

        public override bool HasUnresolved()
        {
            if (TypePlaceholders.Count == 0)
                return false;

            foreach (var field in Fields)
            {
                if (field.Type is PlaceholderType)
                    return true;
            }

            return false;
        }

        public override Type Clone()
        {
            var clone = new StructureType(Identifier.Name)
            {
                Fields = new List<Field>(),
                TypePlaceholders = TypePlaceholders,
                WordLength = WordLength,
                Methods = Methods,
            };

            foreach (var field in Fields)
            {
                clone.Fields.Add(new Field { Name = field.Name, Type = field.Type.Clone(), Offset = field.Offset });
            }

            return clone;
        }

        public override void Resolve(TypeArguments typeArguments)
        {
            base.Resolve(typeArguments);

            ResolvedTypes = typeArguments;

            if (TypePlaceholders.Count != typeArguments.Length)
                throw new System.Exception($"Expected {TypePlaceholders.Count} generics but received {typeArguments.Length}");

            foreach (var field in Fields)
            {
                if (field.Type is PlaceholderType placeholder)
                {
                    field.Type = typeArguments[placeholder.Index];
                }
            }

            if (HasUnresolved())
                throw new System.Exception("Failed to resolve all field generics");

            RecalculateOffsets();
        }

        public bool TryGetGeneric(string name, out PlaceholderType placeholder)
        {
            foreach (var generic in TypePlaceholders)
            {
                if (generic.Identifier.Name == name)
                {
                    placeholder = generic;
                    return true;
                }
            }
            placeholder = null;
            return false;
        }

        /// <summary>
        /// Try getting field type info from a field name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool TryGetField(string name, out Field result)
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
        public void AddField(string name, Type type)
        {
            Fields.Add(new Field { Name = name, Type = type, Offset = WordLength });
            WordLength += type.WordLength;
        }

        /// <summary>
        /// Recalculates field offsets and word length
        /// </summary>
        public void RecalculateOffsets()
        {
            short offset = 0;
            WordLength = 0;
            foreach (var field in Fields)
            {
                field.Offset = offset;
                offset += field.Type.WordLength;
                WordLength += field.Type.WordLength;
            }
        }
    }

    public partial class Compiler
    {
        void ConsumeStructureDeclaration()
        {   
            Consume(TokenType.Structure);

            var name = Consume(TokenType.Identifier).Value;

            if (current.Value.Type == TokenType.Less)
            {
                ConsumeStructureTemplateDeclaration(name);
                return;
            }

            var type = new StructureType(name);
            RegisterType(type);

            Consume(TokenType.StatementTerminator, "Expected new-line after structure name");

            currentScope = currentScope.CreateChild();

            ConsumeStructureMembers(type);

            currentScope = currentScope.Parent;
        }

        void ConsumeStructureMembers(StructureType type)
        {
            while (true)
            {
                if (!TryConsumeIndents(currentScope.Depth))
                    break;

                ConsumeStructureMemberDeclaration(type);
            }
        }

        void ConsumeStructureMemberDeclaration(StructureType type)
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

        void ConsumeStructureFieldDeclaration(StructureType type)
        {
            Consume(TokenType.Field);
            var fieldNname = Consume(TokenType.Identifier).Value;
            var fieldType = ConsumeType();
            type.AddField(fieldNname, fieldType);
            Consume(TokenType.StatementTerminator, "Expected new-line after structure field declaration");
        }

        void ConsumeStructureFunctionDeclaration(StructureType type)
        {
            var function = ConsumeFunctionDeclarationHeader();

            // Add 'self' variable as a reference to this structure
            function.Parameters.Insert(0, new FunctionHeader.Parameter("self", new ReferenceType(type)));

            type.Methods.Add(function);

            var subProgram = new SubProgram(function);
            RegisterSubProgram(subProgram);
            ConsumeFunctionBody(subProgram);
        }

        void CompileMethod(StructureType structureType, FunctionHeader function, TypeArguments methodTypeArguments)
        {
            if (structureType.HasUnresolved())
                throw new CompilerException(null, $"Structure {structureType.Identifier} has unresolved types");

            var typeArgs = structureType.ResolvedTypes.Append(methodTypeArguments);
            var currentToken = current.Value;
            var subProgram = new SubProgram(function);

            current.Value = function.Body;
            ConsumeFunctionBody(subProgram);
            current.Value = currentToken;
        }

        void ConsumeStructLiteral(StructureType type)
        {
            Consume(TokenType.LeftBrace);
            Skip(TokenType.StatementTerminator);

            // Add the default value for the structure first for the layout
            // Then we compute each field value and only then we write the fields one by one
            CodeBuilder.Add(new Instruction(InstructionType.FillZero, (ushort)type.WordLength));

            var literalFields = new List<StructureType.Field>();
            var assignedFields = new HashSet<StructureType.Field>();
            while (current.Value.Type != TokenType.RightBrace)
            {
                Skip(TokenType.Indent);
                var field = ConsumeStructLiteralField(type, assignedFields);
                literalFields.Add(field);
                Skip(TokenType.StatementTerminator);
                Skip(TokenType.Comma);
            }

            Consume(TokenType.RightBrace);
        }

        StructureType.Field ConsumeStructLiteralField(StructureType type, HashSet<StructureType.Field> assignedFields)
        {
            var fieldName = Consume(TokenType.Identifier).Value;
            if (!type.TryGetField(fieldName, out var field))
                throw CreateException(previous.Value, $"Field {fieldName} does not exist in structure {type.Identifier}");
            if (assignedFields.Contains(field))
                throw CreateException(previous.Value, $"Field {fieldName} already assigned in structure literal");

            Consume(TokenType.Equal);

            var expressionType = ConsumeExpression();
            if (expressionType != field.Type)
                throw CreateException(previous.Value, $"Cannot assign {expressionType.Identifier} to {field.Type.Identifier}");

            CodeBuilder.Add(new Instruction(InstructionType.StoreToOffset, (ushort)(type.WordLength - field.Offset), field.Type.WordLength));

            return field;
        }
    }
}