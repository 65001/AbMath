using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public class Misc
    {
        public static bool ZeroFactorialRunnable(RPN.Node node)
        {
            return node.IsOperator("!") && ( node[0].IsNumber(0) || node[0].IsNumber(1) );
        }

        public static RPN.Node ZeroFactorial(RPN.Node node)
        {
            return new RPN.Node(1);
        }
    }
}
