using static Unity.Entities.SystemBaseDelegates;
using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    public partial class Compiler
    {
        ValueType ConsumeExpressionComposite()
        {
            var opening = previous.Value.Type;
            switch (opening)
            {
                case TokenType.LeftBrace:
                    return ConsumeSpanElements();
                default:
                    throw CreateException(previous.Value, $"Unknown composite expression starting with {opening}");
            }
        }

        ValueType ConsumeSpanElements()
        {
            var parseElements = true;
            var valueType = ValueType.Void;
            var count = 0;
            while (parseElements)
            {
                var elementValueType = ConsumeExpression();
                if (elementValueType == ValueType.Void)
                    throw CreateException(previous.Value, "Expected expression in span element");

                if (valueType == ValueType.Void)
                    valueType = elementValueType;
                else if (valueType != elementValueType)
                    throw CreateException(
                        previous.Value,
                        $"All elements in a span must be of the same type. Expected type is {valueType.Identifier} but get {valueType.Identifier}");

                count++;

                switch (current.Value.Type)
                {
                    case TokenType.Comma:
                        Skip();
                        continue;
                    case TokenType.RightBrace:
                        Skip();
                        parseElements = false;
                        break;
                    default:
                        throw CreateException(current.Value, "Expected ',' or '}' after span element expression");
                }
            }

            return new SpanValueType(valueType, count);
        }
    }
}