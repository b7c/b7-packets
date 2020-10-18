using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TanjiWPF
{
    public class HashResolvingException : Exception
    {
        public IReadOnlyList<Identifier> Unresolved { get; }

        internal HashResolvingException(IEnumerable<Identifier> identifiers)
            : base(BuildMessage(identifiers))
        {
            Unresolved = identifiers.ToList().AsReadOnly();
        }

        private static string BuildMessage(IEnumerable<Identifier> identifiers)
        {
            var sb = new StringBuilder();

            sb.Append($"Unable to resolve required message hashes.");

            if (identifiers.Any(x => !x.IsOutgoing))
            {
                sb.Append(" Incoming: ");
                sb.Append(string.Join(", ", identifiers.Where(x => !x.IsOutgoing)));
                if (identifiers.Any(x => x.IsOutgoing)) sb.Append(";");
            }

            if (identifiers.Any(x => x.IsOutgoing))
            {
                sb.Append(" Outgoing: ");
                sb.Append(string.Join(", ", identifiers.Where(x => x.IsOutgoing)));
            }

            return sb.ToString();
        }
    }
}
