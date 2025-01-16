using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    /// <summary>
    /// Lists are a type of pointer that points to a ListDescriptor
    /// </summary>
    public class ListType : PointerType
    {
        public ListType(Type element) : base(element, new Path($"{element.Identifier.Name}[]"))
        {
            Element = element;
        }
    }

    /// <summary>
    /// Holds information about a list
    /// </summary>
    public class ListDescriptor
    {
        public Type ElementType;
        public int Capacity;
        public int Length;
    }

    public partial class Compiler
    {
        
    }
}