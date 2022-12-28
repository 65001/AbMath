using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator.Functions;
using AbMath.Calculator.Operators;

namespace AbMath.Calculator.Simplifications
{ 
    public static class Trig
    {
        public static bool CosOverSinToCotRunnable(RPN.Node node)
        {
            return node.IsDivision() && node[0].IsFunction("sin") &&
                   node[1].IsFunction("cos") &&
                   node[0,0].Matches(node[1,0]);
        }

        public static RPN.Node CosOverSinToCot(RPN.Node node)
        {
            RPN.Node cot = new RPN.Node(new[] { node[0,0] },
                new RPN.Token("cot", 1, RPN.Type.Function));
            return cot;
        }

        public static bool SinOverCosRunnable(RPN.Node node)
        {
            return node.IsDivision() && node[0].IsFunction("cos") &&
                   node[1].IsFunction("sin") &&
                   node[0,0].Matches(node[1,0]);
        }

        public static RPN.Node SinOverCos(RPN.Node node)
        {
            RPN.Node tan = new RPN.Node(new[] { node[0,0] },
                new RPN.Token("tan", 1, RPN.Type.Function));
            return tan;
        }

        public static bool CosOverSinComplexRunnable(RPN.Node node)
        {
            return node.IsDivision() && node[1].IsMultiplication() &&
                   node[0].IsFunction("sin") && node[1,0].IsFunction("cos") &&
                   node[0,0].Matches(node[1,0,0]);
        }

        public static RPN.Node CosOverSinComplex(RPN.Node node)
        {
            return new Mul(node[1,1], new Cot(node[0, 0]) );
        }

        public static bool SecUnderToCosRunnable(RPN.Node node)
        {
            return node.IsDivision() && node[0].IsFunction("sec");
        }

        public static RPN.Node SecUnderToCos(RPN.Node node)
        {
            return new Mul(node[1], new Cos(node[0,0]));
        }

        public static bool CscUnderToSinRunnable(RPN.Node node)
        {
            return node.IsDivision() && node[0].IsFunction("csc");
        }

        public static RPN.Node CscUnderToSin(RPN.Node node)
        {
            return new Mul(node[1], new Sin(node[0, 1]) );
        }

        public static bool CotUnderToTanRunnable(RPN.Node node)
        {
            return node.IsDivision() && node[0].IsFunction("cot");
        }

        public static RPN.Node CotUnderToTan(RPN.Node node)
        {
            return new Mul(node[1], new Tan(node[0, 0]) );
        }


        public static bool CosUnderToSecRunnable(RPN.Node node)
        {
            return node.IsDivision() && node[0].IsFunction("cos");
        }

        public static RPN.Node CosUnderToSec(RPN.Node node)
        {
            return new Mul(node[1], new Sec(node[0,0]) );
        }

        public static bool SinUnderToCscRunnable(RPN.Node node)
        {
            return node.IsDivision() && node[0].IsFunction("sin");
        }

        public static RPN.Node SinUnderToCsc(RPN.Node node)
        {
            RPN.Node csc = new RPN.Node(new[] { node.Children[0].Children[0] },
                new RPN.Token("csc", 1, RPN.Type.Function));
            return new Mul(node[1], csc);
        }

        public static bool TanUnderToCotRunnable(RPN.Node node)
        {
            return node.IsDivision() && node.Children[0].IsFunction("tan");
        }

        public static RPN.Node TanUnderToCot(RPN.Node node)
        {
            return new Mul(node[1], new Cot(node[0, 0]));
        }

        public static bool CosEvenIdentityRunnable(RPN.Node node)
        {
            return node.IsFunction("cos") && node[0].IsMultiplication() && node[0,1].IsNumber(-1);
        }

        public static RPN.Node CosEvenIdentity(RPN.Node node)
        {
            node[0].Replace(node[0,1], new RPN.Node(1));
            return node;
        }

        public static bool SecEvenIdentityRunnable(RPN.Node node)
        {
            return node.IsFunction("sec") && node[0].IsMultiplication() && node[0,1].IsNumber(-1);
        }

        public static RPN.Node SecEvenIdentity(RPN.Node node)
        {
            node[0].Replace(node[0,1], new RPN.Node(1));
            return node;
        }

        public static bool SinOddIdentityRunnable(RPN.Node node)
        {
            return node.IsFunction("sin") && node[0].IsMultiplication() && node[0, 1].IsNumber(-1);
        }

        public static RPN.Node SinOddIdentity(RPN.Node node)
        {
            return new Mul(node[0,1], new Sin(node[0, 0]) );
        }

        public static bool TanOddIdentityRunnable(RPN.Node node)
        {
            return node.IsFunction("tan") && node[0].IsMultiplication() && node[0, 1].IsNumber(-1);
        }

        public static RPN.Node TanOddIdentity(RPN.Node node)
        {
            return new Mul(node[0,1], new Tan(node[0, 0]) );
        }

        public static bool CotOddIdentityRunnable(RPN.Node node)
        {
            return node.IsFunction("cot") && node[0].IsMultiplication() && node[0, 1].IsNumber(-1);
        }

        public static RPN.Node CotOddIdentity(RPN.Node node)
        {
            return new Mul(node[0,1], new Cot(node[0,0]) );
        }

        public static bool CscOddIdentityRunnable(RPN.Node node)
        {
            return node.IsFunction("csc") && node[0].IsMultiplication() && node[0, 1].IsNumber(-1);
        }

        public static RPN.Node CscOddIdentity(RPN.Node node)
        {
            RPN.Node csc = new RPN.Node(new[] { node.Children[0].Children[0] },
                new RPN.Token("csc", 1, RPN.Type.Function));
            return new Mul(node[0,1], csc);
        }
        public static bool TrigIdentitySinToCosRunnable(RPN.Node node)
        {
            return node.IsSubtraction() && node[0].IsExponent() && node[1].IsNumber(1) && node[0, 0].IsNumber(2) &&
                   node[0, 1].IsFunction("sin");
        }

        public static RPN.Node TrigIdentitySinToCos(RPN.Node node)
        {
            return new Pow(new Cos(node[0,1,0]), node[0,0]);
        }

        public static bool TrigIdentityCosToSinRunnable(RPN.Node node)
        {
            return node.IsSubtraction() && node[0].IsExponent() && node[1].IsNumber(1) && node[0, 0].IsNumber(2) &&
                   node[0, 1].IsFunction("cos");
        }

        public static RPN.Node TrigIdentityCosToSin(RPN.Node node)
        {
            return new Pow(new Sin(node[0, 1, 0]), node[0,0]);
        }

        public static bool TrigIdentitySinPlusCosRunnable(RPN.Node node)
        {
            return node.IsAddition() &&
                   node[0].IsExponent() &&
                   node[1].IsExponent() &&
                   node[0,0].IsNumber(2) &&
                   node[1,0].IsNumber(2) &&
                   (node[0,1].IsFunction("cos") || node[0,1].IsFunction("sin")) &&
                   (node[1,1].IsFunction("sin") || node[1,1].IsFunction("cos")) &&
                   !node.ChildrenAreIdentical() &&
                   !node.ContainsDomainViolation() &&
                   node[0,1,0].Matches(node[1,1,0]);
        }

        public static RPN.Node TrigIdentitySinPlusCos(RPN.Node node)
        {
            return new RPN.Node(1);
        }

        public static bool CosOverSinToCotComplexRunnable(RPN.Node node)
        {
            return node.IsDivision() && node[0].IsMultiplication() && node[1].IsFunction("cos") &&
                   node[0, 0].IsFunction("sin") && node[0, 0, 0].Matches(node[1, 0]);
        }

        public static RPN.Node CosOverSinToCotComplex(RPN.Node node)
        {
            //cos(x)/[sin(x) * f(x)] -> cot(x)/f(x) is also implemented due to swapping rules. 
            return new Div(new Cot(node[1, 0]), node[0,1]);
        }
    }
}
