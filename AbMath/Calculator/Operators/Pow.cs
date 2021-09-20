using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Operators
{
    public class Pow : RPN.Node
    {
        public Pow(RPN.Node bottom, RPN.Node power) : base(new RPN.Node[] {power, bottom}, new RPN.Token("^", 2, RPN.Type.Operator) )
        {

        }
    }
}
