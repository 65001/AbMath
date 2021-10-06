using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Operators
{
    public class Sub : RPN.Node
    {
        private static RPN.Token _sub = new RPN.Token("-", 2, RPN.Type.Operator);
        public Sub(RPN.Node left, RPN.Node right) :
            base(new RPN.Node[] { right, left },_sub)
        {

        }
    }
}
