using static UnityEngine.UI.Image;

namespace Elfenlabs.Scripting
{
    /// <summary>
    /// Represents a reference to a specific memory location in the virtual machine.
    /// Used for resolving variable accesses, fields, and other memory operations.
    /// </summary>
    public class MemoryReference
    {
        /// <summary>
        /// The type of the value
        /// </summary>
        public Type Type;

        /// <summary>
        /// A flag indicating if the value is stored on the heap
        /// </summary>
        public bool IsHeap;

        /// <summary>
        /// A flag indicating if a deref operation is needed to access the value
        /// </summary>
        public bool IsUnderRef;

        /// <summary>
        /// A flag indicating if this is an R-Value
        /// </summary>
        public bool IsRValue;

        /// <summary>
        /// The position of the value, from the nearest frame pointer if this is a stack value 
        /// or from the heap pointer if this is a heap value
        /// </summary>
        public short Offset;

        public MemoryReference() { }

        /// <summary>
        /// Create a memory reference for a struct field
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="field"></param>
        public MemoryReference(MemoryReference parent, StructureValueType.Field field)
        {
            Type = field.Type;
            IsHeap = parent.IsHeap;
            Offset = (short)(parent.Offset + field.Offset);
        }
    }

    /// <summary>
    /// Memory reference with a name and scope
    /// </summary>
    public class Variable : MemoryReference
    {
        /// <summary>
        /// The name of the variable
        /// </summary>
        public string Name;

        /// <summary>
        /// The scope the variable is declared in
        /// </summary>
        public Scope Scope;

        /// <summary>
        /// Create a new variable
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="scope"></param>
        /// <param name="position"></param>
        public Variable(Type type, string name, Scope scope, short offset) : base()
        {
            Type = type;
            Name = name;
            Scope = scope;
            Offset = offset;
        }

        /// <summary>
        /// Create a variable for a struct field
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="field"></param>
        public Variable(Variable parent, StructureValueType.Field field) : base()
        {
            Type = field.Type;
            Name = $"{parent.Name}.{field.Name}";
            Scope = parent.Scope;
            Offset = (short)(parent.Offset + field.Offset);
        }
    }

    public partial class Compiler
    {
        void ConsumeStatementVariableDeclaration()
        {
            Skip();
            Consume(TokenType.Identifier, "Expected variable name");
            var variableName = previous.Value.Value;

            Consume(TokenType.Equal, "Expected '=' after variable name");

            var valueType = ConsumeExpression();
            if (valueType == Type.Void)
                throw CreateException(previous.Value, "Cannot declare variable of type void");

            currentScope.DeclareVariable(variableName, valueType);
            Consume(TokenType.StatementTerminator, "Expected new-line after declaration");
        }

        void ConsumeStatementVariable(Variable variable)
        {
            var resolvedValue = ConsumeVariable(variable);

            switch (current.Value.Type)
            {
                case TokenType.Equal:

                    // Ensure that the LHS is R-Value
                    if (resolvedValue.IsRValue)
                        throw CreateException(current.Value, "Invalid target for assignment");

                    Skip();

                    var equalToken = previous.Value;
                    var rhsType = ConsumeExpression();
                    if (resolvedValue.Type != rhsType)
                        throw CreateException(equalToken, $"Cannot assign {rhsType.Identifier} to {resolvedValue.Type.Identifier}");

                    if (resolvedValue.IsHeap)
                        CodeBuilder.Add(new Instruction(InstructionType.StoreToHeap, resolvedValue.Offset, resolvedValue.Type.WordLength));
                    if (resolvedValue.IsUnderRef)
                        CodeBuilder.Add(new Instruction(InstructionType.StoreToAddress, resolvedValue.Offset, resolvedValue.Type.WordLength));
                    else
                        CodeBuilder.Add(new Instruction(InstructionType.Store, resolvedValue.Offset, resolvedValue.Type.WordLength));

                    break;
                case TokenType.Increment:
                    Skip();
                    ConsumeIncrement(resolvedValue);
                    break;
                case TokenType.Decrement:
                    Skip();
                    ConsumeDecrement(resolvedValue);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Fully resolve a variable expression
        /// </summary>
        /// <param name="variable"></param>
        MemoryReference ConsumeVariable(Variable variable)
        {
            var resolvedValue = ConsumeValueAccessor(variable);

            return resolvedValue;
        }

        /// <summary>
        /// Consume value accessor operators (e.g. .member, [index])
        /// If the end of the accessor chain lives in the heap, the heap address will be on the top of the stack
        /// Otherwise the returned value's offset will be the resolved offset
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        MemoryReference ConsumeValueAccessor(MemoryReference value)
        {
            switch (current.Value.Type)
            {
                case TokenType.Dot:
                    Skip();
                    return ConsumeMemberAccessor(value);
                case TokenType.LeftBracket:
                    Skip();
                    return ConsumeArrayAccessor(value);
                default:
                    return value;
            }
        }

        /// <summary>
        /// Consume member accessor operator (.) on a value
        /// </summary>
        /// <param name="type"></param>
        /// <param name="currentOffset"></param>
        /// <returns></returns>
        MemoryReference ConsumeMemberAccessor(MemoryReference parent)
        {
            var underlyingType = parent.Type is ReferenceType refType ? refType.Element : parent.Type;
            switch (underlyingType)
            {
                case StructureValueType structureValueType:
                    var member = Consume(TokenType.Identifier, "Expected identifier after '.'");
                    if (structureValueType.TryGetField(member.Value, out var field))
                    {
                        var fieldValue = new MemoryReference 
                        { 
                            Type = field.Type,
                            IsHeap = parent.IsHeap,
                        };

                        // If the parent is a ref type, the parent is guaranteed to be a variable since ref types cannot be a field member
                        if (parent.Type is ReferenceType)
                        {
                            CodeBuilder.Add(new Instruction(InstructionType.LoadStack, parent.Offset, 1));
                            fieldValue.IsUnderRef = true;
                            fieldValue.Offset = field.Offset;
                        }
                        else
                        {
                            fieldValue.Offset = (short)(parent.Offset + field.Offset);
                        }

                        // If the parent value is a ptr, that means the field member resides in the heap
                        // So we need to push the heap address to the stack
                        // TODO: recheck this for ptr 
                        if (parent.Type is PointerType)
                        {
                            // If the parent is a heap value then we can safely assume that the top of the stack is its heap address
                            // so we simply add the field offset to the top of the stack
                            if (parent.IsHeap)
                            {
                                CodeBuilder.AddConstant(field.Offset);
                                CodeBuilder.Add(new Instruction(InstructionType.IntAdd));
                            }

                            // If the parent is a stack value then we need to load the parent's heap address to the stack
                            else
                            {
                                CodeBuilder.Add(new Instruction(InstructionType.LoadStack, (short)(parent.Offset + field.Offset)));
                            }

                            fieldValue.IsHeap = true;
                        }

                        return ConsumeValueAccessor(fieldValue);
                    }
                    if (structureValueType.TryGetMethod(member.Value, out var method))
                    {
                        // TODO: handle method invocation for stack struct vs heap struct
                        // Inject the struct address as the first argument
                        CodeBuilder.Add(new Instruction(InstructionType.LoadStackAddress, parent.Offset));

                        var methodReturnType = ConsumeFunctionCall(method, 1);
                        var returnValue = new MemoryReference { Type = methodReturnType, IsRValue = true };
                        return ConsumeValueAccessor(returnValue);
                    }
                    throw CreateException(current.Value, $"Unknown member {member.Value} of type {structureValueType}");
                case SpanValueType spanValueType:
                    var indexToken = Consume(TokenType.Integer, "Expected integer after '.'");
                    var index = int.Parse(indexToken.Value);
                    if (index >= spanValueType.Length)
                        throw CreateException(indexToken, $"Index {index} is out of bounds for span {spanValueType}");
                    return ConsumeValueAccessor(new MemoryReference { Type = spanValueType, Offset = (short)(parent.Offset + index) });
                default:
                    throw CreateException(
                        previous.Value,
                        $"The member accessor operator '.' can only be used for spans, structs, or module. {parent.Type} is not one of them.");
            }
        }

        /// <summary>
        /// Consume variable array accessor ([])
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        MemoryReference ConsumeArrayAccessor(MemoryReference parent)
        {
            switch (parent.Type)
            {
                case ListValueType listValueType:
                    var indexType = ConsumeExpression();
                    if (indexType != Type.Int)
                        throw CreateException(current.Value, "Index must be an integer");

                    Consume(TokenType.RightBracket, "Expected ']' after index");

                    // If the parent is a stack value then we need to load the parent's heap address to the stack first
                    if (!parent.IsHeap)
                    {
                        CodeBuilder.Add(new Instruction(InstructionType.LoadStack, parent.Offset));
                    }
                    CodeBuilder.Add(new Instruction(InstructionType.IntAdd));

                    return ConsumeValueAccessor(new MemoryReference { Type = listValueType.Element, IsHeap = true });
                default:
                    throw CreateException(
                        previous.Value,
                        $"The array accessor operator '[]' can only be used for lists. {parent.Type} is not a list.");
            }
        }

        void ConsumeIncrement(MemoryReference value)
        {
            // TODO: Handle reference types
            AssertValueType(value.Type, Type.Int);
            CodeBuilder.Add(new Instruction(
                InstructionType.VariableIncrement,
                value.Offset));
        }

        void ConsumeDecrement(MemoryReference value)
        {
            // TODO: Handle reference types
            AssertValueType(value.Type, Type.Int);
            CodeBuilder.Add(new Instruction(
                InstructionType.VariableDecrement,
                value.Offset));
        }
    }
}