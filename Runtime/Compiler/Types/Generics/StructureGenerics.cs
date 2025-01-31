using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    public class StructureTypeTemplate
    {
        public class FieldTemplate
        {
            public string Name;
            public PlaceholderType Placeholder;
            public Type Type;

            public FieldTemplate(string name, PlaceholderType placeholder)
            {
                Name = name;
                Placeholder = placeholder;
            }

            public FieldTemplate(string name, Type type)
            {
                Name = name;
                Type = type;
            }
        }

        public string Identifier;

        public List<FieldTemplate> Fields = new();

        public List<PlaceholderType> TypePlaceholders = new();

        public List<FunctionHeader> Methods = new();

        public StructureType Instantiate(TypeArguments typeArguments)
        {
            if (TypePlaceholders.Count != typeArguments.Length)
                throw new System.Exception($"Expected {TypePlaceholders.Count} generics but received {typeArguments.Length}");

            var structure = new StructureType(Identifier + typeArguments.ToString());

            for (int i = 0; i < Fields.Count; i++)
            {
                var field = Fields[i];
                if (field.Placeholder != null)
                {
                    structure.AddField(field.Name, typeArguments[field.Placeholder.Index]);
                }
                else
                {
                    structure.AddField(field.Name, field.Type);
                }
            }

            structure.Methods.AddRange(Methods);

            return structure;
        }

        public bool TryGetGeneric(string name, out PlaceholderType placeholder)
        {
            foreach (var generic in TypePlaceholders)
            {
                if (generic.Identifier == name)
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
        // public bool TryGetField(string name, out Field result)
        // {
        //     foreach (var field in Fields)
        //     {
        //         if (field.Name == name)
        //         {
        //             result = field;
        //             return true;
        //         }
        //     }
        //     result = null;
        //     return false;
        // }
    }

    public partial class Compiler
    {
        void ConsumeStructureTemplateDeclaration(string name)
        {

            var template = new StructureTypeTemplate
            {
                Identifier = name,
                TypePlaceholders = ConsumeTypeParameters()
            };

            RegisterStructureTemplate(template);

            Consume(TokenType.StatementTerminator, "Expected new-line after structure name");

            currentScope = currentScope.CreateChild();

            ConsumeStructureTemplateMembers(template, typeParams);

            currentScope = currentScope.Parent;
        }

        List<PlaceholderType> ConsumeTypeParameters()
        {
            Consume(TokenType.Less);

            var result = new List<PlaceholderType>();

            while (true)
            {
                var name = Consume(TokenType.Identifier).Value;
                var type = new PlaceholderType(name, result.Count);

                if (GetType(name, result) != null)
                    throw CreateException(previous.Value, $"Type {name} already defined");

                // TODO: handle constraints

                result.Add(type);

                if (MatchAdvance(TokenType.Comma))
                    continue;

                Consume(TokenType.Greater);
                break;
            }

            return result;
        }

        void ConsumeStructureTemplateMembers(StructureTypeTemplate template, List<PlaceholderType> typePlaceholders)
        {
            while (true)
            {
                if (!TryConsumeIndents(currentScope.Depth))
                    break;

                ConsumeStructureMemberDeclaration(type, typePlaceholders);
            }
        }

        void ConsumeStructureMemberDeclaration(StructureType type, List<PlaceholderType> typePlaceholders)
        {
            switch (current.Value.Type)
            {
                case TokenType.Field:
                    ConsumeStructureFieldDeclaration(type, typePlaceholders);
                    break;
                case TokenType.Function:
                    ConsumeStructureFunctionDeclaration(type, typePlaceholders);
                    break;
                default:
                    throw new CompilerException(current.Value, $"Expected structure member declaration. Received: {current.Value}");
            }
        }

        void ConsumeStructureFieldDeclaration(StructureType type, List<PlaceholderType> typePlaceholders)
        {
            Consume(TokenType.Field);
            var fieldNname = Consume(TokenType.Identifier).Value;
            var fieldType = ConsumeType(typePlaceholders);
            type.AddField(fieldNname, fieldType);
            Consume(TokenType.StatementTerminator, "Expected new-line after structure field declaration");
        }

        void ConsumeStructureFunctionDeclaration(StructureType type)
        {
            var function = ConsumeFunctionDeclarationHeader();

            // Add 'self' variable as a reference to this structure
            function.Parameters.Insert(0, new FunctionHeader.Parameter("self", new ReferenceType(type)));

            type.AddMethod(function);

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