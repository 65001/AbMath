using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public class Token
        {
            public string Value;
            public int Arguments;
            public Type Type;

            public override string ToString()
            {
                return Value;
            }
        }
    }
}
