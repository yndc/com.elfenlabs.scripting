using UnityEngine;

namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        Type ConsumeExpression()
        {
            return ConsumeExpressionForward(Precedence.Assignment);
        }

        Type ConsumeExpressionForward(Precedence minimumPrecedence)
        {
            Skip();

            var prefixRule = GetRule(previous.Value.Type).Prefix;
            if (prefixRule == Handling.None)
                throw CreateException(
                    previous.Value,
                    $"Expected expression, received {previous.Value.Value} ({previous.Value.Type})");

            // This is the only place where infix operation is compiled, therefore we need to store the last value type here 
            lastValueType = ConsumeExpression(prefixRule);

            while (GetRule(current.Value.Type).Precedence >= minimumPrecedence)
            {
                Skip();
                var infixRule = GetRule(previous.Value.Type).Infix;
                lastValueType = ConsumeExpression(infixRule);
            }

            return lastValueType;
        }

        Type ConsumeExpression(Handling handling)
        {
            return handling switch
            {
                Handling.Group => ConsumeExpressionGroup(),
                Handling.Unary => ConsumeExpressionUnary(),
                Handling.Binary => ConsumeExpressionBinary(),
                Handling.Literal => ConsumeExpressionLiteral(),
                Handling.Composite => ConsumeExpressionComposite(),
                Handling.Identifier => ConsumeExpressionIdentifier(),
                _ => Type.Void,
            };
        }

        Type ConsumeExpressionGroup()
        {
            var valueType = ConsumeExpression();
            Consume(TokenType.RightParentheses, "Expected ')' after expression.");
            return valueType;
        }

        Type ConsumeExpressionUnary()
        {
            var op = previous.Value.Type;
            var valueType = ConsumeExpressionForward(Precedence.Unary);
            switch (op)
            {
                case TokenType.Minus:
                    switch (valueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntNegate)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatNegate)); break;
                        default: throw CreateException(previous.Value, $"Invalid type {valueType} for symbol {TokenType.Minus}");
                    }
                    break;
                case TokenType.Bang:
                    AssertValueType(valueType, Type.Bool);
                    CodeBuilder.Add(new Instruction(InstructionType.BoolNegate)); break;
                default: throw CreateException(previous.Value, $"Invalid unary symbol {op}");
            }

            return valueType;
        }

        Type ConsumeExpressionBinary()
        {
            var op = previous.Value.Type;
            var rule = GetRule(op);
            var lhsValueType = lastValueType;
            var rhsValueType = ConsumeExpressionForward(rule.Precedence + 1);
            AssertValueTypeEqual(lhsValueType, rhsValueType);
            switch (op)
            {
                // Arithmetic
                case TokenType.Plus:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntAdd)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatAdd)); break;
                    }
                    break;
                case TokenType.Minus:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntSubtract)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatSubstract)); break;
                    }
                    break;
                case TokenType.Slash:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntDivide)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatDivide)); break;
                    }
                    break;
                case TokenType.Asterisk:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntMultiply)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatMultiply)); break;
                    }
                    break;
                case TokenType.Power:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntPower)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatPower)); break;
                    }
                    break;
                case TokenType.Remainder:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntModulo)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatModulo)); break;
                    }
                    break;

                // Comparison
                case TokenType.BangEqual:
                    CodeBuilder.Add(new Instruction(InstructionType.NotEqual));
                    return Type.Bool;
                case TokenType.EqualEqual:
                    CodeBuilder.Add(new Instruction(InstructionType.Equal));
                    return Type.Bool;
                case TokenType.Greater:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntGreaterThan)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatGreaterThan)); break;
                    }
                    return Type.Bool;
                case TokenType.GreaterEqual:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntGreaterThanEqual)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatGreaterThanEqual)); break;
                    }
                    return Type.Bool;
                case TokenType.Less:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntLessThan)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatLessThan)); break;
                    }
                    return Type.Bool;
                case TokenType.LessEqual:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntLessThanEqual)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatLessThanEqual)); break;
                    }
                    return Type.Bool;

                // String concatenation
                case TokenType.StringInterpolationTerminator:
                    AssertValueType(lhsValueType, Type.String);
                    AssertValueType(rhsValueType, Type.String);
                    CodeBuilder.Add(new Instruction(InstructionType.StringConcatenate));
                    return Type.String;

                // Access operator
                // TODO: untested
                case TokenType.Dot:
                    var lhsValue = new MemoryReference { Type = lhsValueType, IsRValue = true };
                    lhsValue = ConsumeValueAccessor(lhsValue);
                    return lhsValue.Type;
            }

            return rhsValueType;
        }

        Type ConsumeExpressionLiteral()
        {
            var str = previous.Value.Value;

            switch (previous.Value.Type)
            {
                case TokenType.Integer:
                    CodeBuilder.AddConstant(int.Parse(str));
                    return Type.Int;
                case TokenType.Float:
                    CodeBuilder.AddConstant(float.Parse(str));
                    return Type.Float;
                case TokenType.False:
                    CodeBuilder.AddConstant(0);
                    return Type.Bool;
                case TokenType.True:
                    CodeBuilder.AddConstant(1);
                    return Type.Bool;
                case TokenType.String:
                    CodeBuilder.AddConstant(str);
                    return Type.String;
                default:
                    throw CreateException(previous.Value, $"Unknown literal {str} of type {previous.Value.Type}");
            }
            ;
        }

        Type ConsumeExpressionIdentifier()
        {
            var identifier = previous.Value.Value;

            // Check if a primitive type exists with the same name
            if (types.TryGetValue(identifier, out Type type))
            {
                return ConsumeExpressionType(type);
            }

            // Check if it refers to a variable
            if (currentScope.TryGetVariable(identifier, out var variable))
            {
                return ConsumeExpressionVariable(variable);
            }

            // Check if it refers to a function
            if (currentScope.TryGetFunction(identifier, out var function))
            {
                if (current.Value.Type == TokenType.LeftParentheses)
                {
                    return ConsumeFunctionCall(function);
                }
                else
                {
                    return ConsumeFunctionPointer(function);
                }
            }

            throw CreateException(previous.Value, $"Unknown identifier {identifier}");
        }

        Type ConsumeExpressionVariable(Variable variable)
        {
            var resolvedValue = ConsumeValueAccessor(variable);
            if (resolvedValue.IsHeap)
            {
                CodeBuilder.Add(new Instruction(InstructionType.LoadHeap, resolvedValue.Offset, resolvedValue.Type.WordLength));
            }
            else if (resolvedValue.IsUnderRef)
            {
                CodeBuilder.Add(new Instruction(InstructionType.PushFromStackAddress, resolvedValue.Offset, resolvedValue.Type.WordLength));
            }
            else if (!resolvedValue.IsRValue)
            {
                CodeBuilder.Add(new Instruction(InstructionType.PushFromFrame, resolvedValue.Offset, resolvedValue.Type.WordLength));
            }
            return resolvedValue.Type;
        }

        Type ConsumeExpressionType(Type type)
        {
            // If it is a struct type and the next token is a left brace, it is a struct literal
            if (type is StructureValueType && current.Value.Type == TokenType.LeftBrace)
            {
                ConsumeStructLiteral(type as StructureValueType);
            }

            // Otherwise it is a default value
            else
            {
                Rewind();

                type = ConsumeType();
                CodeBuilder.AddConstant(type.GenerateDefaultValue());
            }

            return type;
        }
    }
}