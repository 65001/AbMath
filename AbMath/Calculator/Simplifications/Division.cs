using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class Division
    {
        public static bool setRule(RPN.Node node)
        {
            return node.IsDivision();
        }

        public static bool DivisionByOneRunnable(RPN.Node node)
        {
            return node[0].IsNumber(1);
        }

        public static RPN.Node DivisionByOne(RPN.Node node)
        {
            return node[1];
        }
    }
}
