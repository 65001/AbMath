using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Operators
{
    public class Mul : RPN.Node
    {
        private static RPN.Token _mul = new RPN.Token("*", 2, RPN.Type.Operator);
        public Mul(RPN.Node left, RPN.Node right) : base(new RPN.Node[] { right, left }, _mul) {}
    }
}
