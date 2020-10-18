using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class Matrix
    {
        public static bool setRule(RPN.Node node)
        {
            return (node.IsAddition() || node.IsSubtraction() || node.IsMultiplication() || node.IsExponent() || node.IsDivision()) && 
                   node[0].

        }
    }
}
