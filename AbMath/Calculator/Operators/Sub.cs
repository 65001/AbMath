using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Operators
{
    public class Sub : RPN.Node
    {
        public Sub(RPN.Node left, RPN.Node right) :
            base(new RPN.Node[] { right, left }, new RPN.Token("-", 2, RPN.Type.Operator))
        {

        }
    }
}
