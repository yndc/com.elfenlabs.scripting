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
            Advance();

            var prefixRule = GetRule(previous.Value.Type).Prefix;
            if (prefixRule == Handling.None)
                throw CreateException(
                    previous.Value,
                    $"Expected expression, received {previous.Value.Value} ({previous.Value.Type})");

            // This is the only place where infix operation is compiled, therefore we need to store the last value type here 
            lastValueType = ConsumeExpression(prefixRule);

            while (GetRule(current.Value.Type).Precedence >= minimumPrecedence)
            {
                Advance();
                var infixRule = GetRule(previous.Value.Type).Infix;
                lastValueType = ConsumeExpression(infixRule);
            }

            return lastValueType;
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
                    switch ((PrimitiveType)valueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntNegate)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatNegate)); break;
                        default: throw CreateException(previous.Value, $"Invalid type {valueType} for symbol {TokenType.Minus}");
                    }
                    break;
                case TokenType.Bang:
                    AssertValueType(valueType, ValueType.Bool);
                    builder.Add(new Instruction(InstructionType.BoolNegate)); break;
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
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntAdd)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatAdd)); break;
                    }
                    break;
                case TokenType.Minus:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntSubstract)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatSubstract)); break;
                    }
                    break;
                case TokenType.Slash:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntDivide)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatDivide)); break;
                    }
                    break;
                case TokenType.Asterisk:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntMultiply)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatMultiply)); break;
                    }
                    break;

                // Comparison
                case TokenType.BangEqual:
                    builder.Add(new Instruction(InstructionType.NotEqual));
                    return ValueType.Bool;
                case TokenType.EqualEqual:
                    builder.Add(new Instruction(InstructionType.Equal));
                    return ValueType.Bool;
                case TokenType.Greater:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntGreaterThan)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatGreaterThan)); break;
                    }
                    return ValueType.Bool;
                case TokenType.GreaterEqual:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntGreaterThanEqual)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatGreaterThanEqual)); break;
                    }
                    return ValueType.Bool;
                case TokenType.Less:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntLessThan)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatLessThan)); break;
                    }
                    return ValueType.Bool;
                case TokenType.LessEqual:
                    switch ((PrimitiveType)lhsValueType.Index)
                    {
                        case PrimitiveType.Int: builder.Add(new Instruction(InstructionType.IntLessThanEqual)); break;
                        case PrimitiveType.Float: builder.Add(new Instruction(InstructionType.FloatLessThanEqual)); break;
                    }
                    return ValueType.Bool;

            }

            return rhsValueType;
        }

        ValueType ConsumeExpressionLiteral()
        {
            var str = previous.Value.Value;

            switch (previous.Value.Type)
            {
                case TokenType.Integer:
                    builder.AddConstant(int.Parse(str));
                    return ValueType.Int;
                case TokenType.Float:
                    builder.AddConstant(float.Parse(str));
                    return ValueType.Float;
                case TokenType.False:
                    builder.AddConstant(0);
                    return ValueType.Bool;
                case TokenType.True:
                    builder.AddConstant(1);
                    return ValueType.Bool;
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
                builder.AddConstant(0);
                return valueType;
            }

            // Check if it refers to a variable
            if (currentScope.TryGetVariable(identifier, out var variable))
            {
                builder.Add(new Instruction(InstructionType.LoadVariable, variable.Position, variable.Type.WordLength));
                return variable.Type;
            }

            // Check if it refers to a function
            if (currentScope.TryGetFunction(identifier, out var function))
            {
                if (MatchAdvance(TokenType.LeftParentheses))
                {
                    ConsumeFunctionCallParameters(function);
                    builder.Add(new Instruction(InstructionType.Call, function.Index, function.ParameterWordLength));
                    return function.ReturnType;
                }
                else
                {
                    // TODO: Add function pointer support
                }
            }

            throw CreateException(previous.Value, $"Unknown expression identifier {identifier}");
        }

        ValueType ConsumeExpression(Handling handling)
        {
            return handling switch
            {
                Handling.Group => ConsumeExpressionGroup(),
                Handling.Unary => ConsumeExpressionUnary(),
                Handling.Binary => ConsumeExpressionBinary(),
                Handling.Literal => ConsumeExpressionLiteral(),
                Handling.Identifier => ConsumeExpressionIdentifier(),
                _ => ValueType.Void,
            };
        }
    }
}