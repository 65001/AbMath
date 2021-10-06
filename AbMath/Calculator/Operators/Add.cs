using System;
using System.Collections.Generic;
using System.Text;
using AbMath;

namespace AbMath.Calculator.Operators
{
    public class Add : RPN.Node
    {
        private static RPN.Token _add = new RPN.Token("+", 2, RPN.Type.Operator);

        public Add(RPN.Node left, RPN.Node right) : 
            base(new RPN.Node[] { right, left }, _add)
        {

        }
    }
}
