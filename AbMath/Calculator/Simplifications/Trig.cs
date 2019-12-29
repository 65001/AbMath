using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{ 
    public static class Trig
    {
        public static bool TrigIdentitySinToCosRunnable(RPN.Node node)
        {
            return node.IsSubtraction() && node[0].IsExponent() && node[1].IsNumber(1) && node[0, 0].IsNumber(2) &&
                   node[0, 1].IsFunction("sin");
        }

        public static RPN.Node TrigIdentitySinToCos(RPN.Node node)
        {
            RPN.Node cos = new RPN.Node(new[] { node[0, 1, 0] }, new RPN.Token("cos", 1, RPN.Type.Function));
            RPN.Node exponent = new RPN.Node(new[] { node[0, 0], cos }, new RPN.Token("^", 2, RPN.Type.Operator));
            return exponent;
        }

        public static bool TrigIdentityCosToSinRunnable(RPN.Node node)
        {
            return node.IsSubtraction() && node[0].IsExponent() && node[1].IsNumber(1) && node[0, 0].IsNumber(2) &&
                   node[0, 1].IsFunction("cos");
        }

        public static RPN.Node TrigIdentityCosToSin(RPN.Node node)
        {
            RPN.Node sin = new RPN.Node(new[] { node[0, 1, 0] }, new RPN.Token("sin", 1, RPN.Type.Function));
            RPN.Node exponent = new RPN.Node(new[] { node[0, 0], sin }, new RPN.Token("^", 2, RPN.Type.Operator));
            return exponent;
        }

        public static bool CosOverSinToCotComplexRunnable(RPN.Node node)
        {
            return node.IsDivision() && node[0].IsMultiplication() && node[1].IsFunction("cos") &&
                   node[0, 0].IsFunction("sin") && node[0, 0, 0].Matches(node[1, 0]);
        }

        public static RPN.Node CosOverSinToCotComplex(RPN.Node node)
        {
            //cos(x)/[sin(x) * f(x)] -> cot(x)/f(x) is also implemented due to swapping rules. 
            RPN.Node cot = new RPN.Node(new[] { node[1, 0] }, new RPN.Token("cot", 1, RPN.Type.Function));
            RPN.Node division = new RPN.Node(new[] { node[0, 1], cot }, new RPN.Token("/", 2, RPN.Type.Operator));
            return division;
        }
    }
}
