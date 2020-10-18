using System;

namespace TanjiWPF
{
    public class UnknownIdentifierException : Exception
    {
        internal UnknownIdentifierException(string message)
            : base(message)
        { }
    }
}
