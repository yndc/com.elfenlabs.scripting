namespace Elfenlabs.Scripting
{
    public class StructureValueType : ValueType
    {
        public class Field
        {
            public string Name;
            public ValueType Type;
        }

        public Field[] Fields;
        
        public StructureValueType(string name, Field[] fields) : base(name, CalculateWordLength(fields))
        {
            Fields = fields;
        }

        static byte CalculateWordLength(Field[] fields)
        {
            byte length = 0;
            foreach (var field in fields)
                length += field.Type.WordLength;
            return length;
        }
    }
    
    public partial class Compiler
    {
        void ConsumeStructureDeclaration()
        {
            //Consume(TokenType.Structure);

            //var name = Consume(TokenType.Identifier).Value;

            //var structure = new Structure(name);

            //while (current.Type != TokenType.CloseBrace)
            //{
            //    var type = Consume(TokenType.Identifier).Value;
            //    var field = Consume(TokenType.Identifier).Value;

            //    structure.Fields.Add(new Field(type, field));

            //    Consume(TokenType.SemiColon);
            //}
        }
    }
}