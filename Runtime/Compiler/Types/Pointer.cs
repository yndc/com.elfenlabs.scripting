namespace Elfenlabs.Scripting
{

    /// <summary>
    /// Pointer type holds a reference to a heap value
    /// </summary>
    public class PointerType : Type
    {
        public Type Element;

        public PointerType(Type element) : base(new Path($"ptr {element.Identifier.Name}"), 1)
        {
            Element = element;
        }

        public PointerType(Type element, Path identifierOverride) : base(identifierOverride, 1)
        {
            Element = element;
        }
    }
}