using System;

namespace Elfenlabs.Scripting
{
    /// <summary>
    /// Path is a class that represents location in a namespace
    /// </summary>
    public class Path : IEquatable<Path>
    {
        public string[] Steps;

        public string Name
        {
            get => Steps[Steps.Length - 1];
            set => Steps[Steps.Length - 1] = value;
        }

        public Path(string fullyQualifiedPath)
        {
            Steps = fullyQualifiedPath.Split('.');
        }

        public bool Equals(Path other)
        {
            for (int i = 0; i < Steps.Length; i++)
            {
                var path = Steps[i];
                if (path != other.Steps[i])
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return Steps.GetHashCode();
        }

        public override string ToString()
        {
            return string.Join(".", Steps);
        }

        public static implicit operator string(Path path) { return path.ToString(); }
    }
}