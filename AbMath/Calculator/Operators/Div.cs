using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Operators
{
    public class Div : RPN.Node
    {
        private static RPN.Token _div = new RPN.Token("/", 2, RPN.Type.Operator);
        public Div(RPN.Node numerator, RPN.Node denominator) : base(new RPN.Node[] { denominator, numerator}, _div)
        {

        }
    }
}
