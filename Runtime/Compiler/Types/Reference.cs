namespace Elfenlabs.Scripting
{
    /// <summary>
    /// Reference type holds a reference to a stack value
    /// </summary>
    public class ReferenceType : Type
    {
        public Type Element;

        public ReferenceType(Type element) : base(new Path($"ref {element.Identifier.Name}"), 1)
        {
            Element = element;
        }

        public ReferenceType(Type element, Path identifierOverride) : base(identifierOverride, 1)
        {
            Element = element;
        }
    }
}