namespace Elfenlabs.Scripting
{
    public class SpanValueType : Type
    {
        public Type Element;

        public int Length;

        public SpanValueType(Type element, int length) : base(new Path($"{element.Identifier.Name}<{length}>"), (byte)(element.WordLength * length))
        {
            Element = element;
            Length = length;
        }

        public override string ToString()
        {
            return $"{Element}<{Length}>";
        }
    }
}