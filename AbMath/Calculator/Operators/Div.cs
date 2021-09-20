using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Operators
{
    public class Div : RPN.Node
    {
        public Div(RPN.Node numerator, RPN.Node denominator) : base(new RPN.Node[] { denominator, numerator}, new RPN.Token("/", 2, RPN.Type.Operator))
        {

        }
    }
}
