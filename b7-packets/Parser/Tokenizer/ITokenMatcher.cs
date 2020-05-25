﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace b7.Packets
{
    interface ITokenMatcher
    {
        IEnumerable<TokenMatch> FindMatches(string input);
    }
}
