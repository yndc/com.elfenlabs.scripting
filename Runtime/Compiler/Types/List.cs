using System.Collections.Generic;

namespace Elfenlabs.Scripting
{
    /// <summary>
    /// Lists are a type of pointer that points to a ListDescriptor
    /// </summary>
    public class ListType : Type
    {
        public Type Element;

        public ListType(Type element) : base(new Path($"{element.Identifier.Name}[]"), 1)
        {
            Element = element;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Add the methods
            // base.Methods.Add(new FunctionHeader("Add", ));
        }

        public override void Instantiate(ByteCodeBuilder builder)
        {
            var initialSize = (short)GetInitialSize();

            // Prepare for the reference counter
            builder.Add(InstructionType.Push, (short)1);

            // Allocate the initial capacity
            builder.Add(InstructionType.FillZero, initialSize);
            builder.Add(InstructionType.StoreToHeap, initialSize);

            // Length and capacity
            builder.Add(InstructionType.Push, 0);
            builder.Add(InstructionType.Push, initialSize);

            // Allocate the fat pointer, include the reference counter
            builder.Add(InstructionType.StoreToHeap, 4);
        }

        public int GetInitialSize()
        {
            return 4;
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