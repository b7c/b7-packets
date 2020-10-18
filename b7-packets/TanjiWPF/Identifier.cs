using System;

namespace TanjiWPF
{
    public sealed class Identifier
    {
        public bool IsOutgoing { get; }
        public string Name { get; }

        internal Identifier(bool isOutgoing, string name)
        {
            IsOutgoing = isOutgoing;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
