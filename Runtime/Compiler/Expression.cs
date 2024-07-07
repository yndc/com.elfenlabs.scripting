using UnityEngine;

namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        ValueType ConsumeExpression()
        {
            return ConsumeExpressionForward(Precedence.Assignment);
        }

        ValueType ConsumeExpressionForward(Precedence minimumPrecedence)
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

        ValueType ConsumeExpression(Handling handling)
        {
            return handling switch
            {
                Handling.Group => ConsumeExpressionGroup(),
                Handling.Unary => ConsumeExpressionUnary(),
                Handling.Binary => ConsumeExpressionBinary(),
                Handling.Literal => ConsumeExpressionLiteral(),
                Handling.Composite => ConsumeExpressionComposite(),
                Handling.Identifier => ConsumeExpressionIdentifier(),
                _ => ValueType.Void,
            };
        }

        ValueType ConsumeExpressionGroup()
        {
            var valueType = ConsumeExpression();
            Consume(TokenType.RightParentheses, "Expected ')' after expression.");
            return valueType;
        }

        ValueType ConsumeExpressionUnary()
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
                    AssertValueType(valueType, ValueType.Bool);
                    CodeBuilder.Add(new Instruction(InstructionType.BoolNegate)); break;
                default: throw CreateException(previous.Value, $"Invalid unary symbol {op}");
            }

            return valueType;
        }

        ValueType ConsumeExpressionBinary()
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
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntSubstract)); break;
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
                    return ValueType.Bool;
                case TokenType.EqualEqual:
                    CodeBuilder.Add(new Instruction(InstructionType.Equal));
                    return ValueType.Bool;
                case TokenType.Greater:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntGreaterThan)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatGreaterThan)); break;
                    }
                    return ValueType.Bool;
                case TokenType.GreaterEqual:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntGreaterThanEqual)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatGreaterThanEqual)); break;
                    }
                    return ValueType.Bool;
                case TokenType.Less:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntLessThan)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatLessThan)); break;
                    }
                    return ValueType.Bool;
                case TokenType.LessEqual:
                    switch (lhsValueType.Identifier)
                    {
                        case "Int": CodeBuilder.Add(new Instruction(InstructionType.IntLessThanEqual)); break;
                        case "Float": CodeBuilder.Add(new Instruction(InstructionType.FloatLessThanEqual)); break;
                    }
                    return ValueType.Bool;

                // String concatenation
                case TokenType.StringInterpolationTerminator:
                    AssertValueType(lhsValueType, ValueType.String);
                    AssertValueType(rhsValueType, ValueType.String);
                    CodeBuilder.Add(new Instruction(InstructionType.StringConcatenate));
                    return ValueType.String;
            }

            return rhsValueType;
        }

        ValueType ConsumeExpressionLiteral()
        {
            var str = previous.Value.Value;

            switch (previous.Value.Type)
            {
                case TokenType.Integer:
                    CodeBuilder.AddConstant(int.Parse(str));
                    return ValueType.Int;
                case TokenType.Float:
                    CodeBuilder.AddConstant(float.Parse(str));
                    return ValueType.Float;
                case TokenType.False:
                    CodeBuilder.AddConstant(0);
                    return ValueType.Bool;
                case TokenType.True:
                    CodeBuilder.AddConstant(1);
                    return ValueType.Bool;
                case TokenType.String:
                    CodeBuilder.AddConstant(str);
                    return ValueType.String;
                default:
                    throw CreateException(previous.Value, $"Unknown literal {str} of type {previous.Value.Type}");
            };
        }

        ValueType ConsumeExpressionIdentifier()
        {
            var identifier = previous.Value.Value;

            // Check if it refers to a type, replace it as the default value for that type
            if (types.TryGetValue(identifier, out ValueType valueType))
            {
                // If it is a struct type and the next token is a left brace, it is a struct literal
                if (valueType is StructureValueType && current.Value.Type == TokenType.LeftBrace)
                {
                    ConsumeStructLiteral(valueType as StructureValueType);
                }

                // Otherwise it is a default value
                else
                {
                    Rewind();

                    valueType = ConsumeType();
                    CodeBuilder.AddConstant(valueType.GenerateDefaultValue());
                }

                return valueType;
            }

            // Check if it refers to a variable
            if (currentScope.TryGetVariable(identifier, out var variable))
            {
                // Check if it uses the array access operator
                //if (MatchAdvance(TokenType.LeftBracket))
                //{
                //    if (variable.Type.Span == 0) throw CreateException(previous.Value, $"Variable {identifier} is not an array, you can't use the array accessor operator here '[]'");
                //    var indexValueType = ConsumeExpression();
                //    AssertValueTypeEqual(indexValueType, ValueType.Int);
                //    Consume(TokenType.RightBracket, "Expected ']' to close the array accessor operator");
                //    builder.Add(new Instruction(InstructionType.LoadVariableElement, variable.Position, variable.Type.WordLength));
                //    return variable.Type.ToElement();
                //}

                // Check if it uses the member access operator
                if (MatchAdvance(TokenType.Dot))
                {
                    switch (variable.Type)
                    {
                        case SpanValueType spanValueType:
                            var indexToken = Consume(TokenType.Integer, "Expected integer after '.'");
                            var index = int.Parse(indexToken.Value);
                            if (index >= spanValueType.Length)
                                throw CreateException(indexToken, $"Index {index} is out of bounds for span {spanValueType}");
                            CodeBuilder.Add(new Instruction(InstructionType.LoadVariable, (ushort)(variable.Position + index), spanValueType.Element.WordLength));
                            return spanValueType.Element;
                        case StructureValueType structureValueType:
                            var member = Consume(TokenType.Identifier, "Expected identifier after '.'");
                            if (!structureValueType.TryGetFieldByName(member.Value, out var field))
                            {
                                throw CreateException(current.Value, $"Unknown member {member} in variable {identifier} of type {structureValueType}");
                            }
                            CodeBuilder.Add(new Instruction(InstructionType.LoadVariable, (ushort)(variable.Position + field.Offset), field.Type.WordLength));
                            return field.Type;
                        default:
                            throw CreateException(previous.Value, $"The member accessor operator '.' can only be used for spans, structs, or module. {identifier} is not one of them.");
                    }
                }

                CodeBuilder.Add(new Instruction(InstructionType.LoadVariable, variable.Position, variable.Type.WordLength));

                // Handles increment and decrement operators
                ConsumeVariableIncrement(variable);
                ConsumeVariableDecrement(variable);

                return variable.Type;
            }

            // Check if it refers to a function
            if (currentScope.TryGetFunction(identifier, out var function))
            {
                if (MatchAdvance(TokenType.LeftParentheses))
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
    }
}