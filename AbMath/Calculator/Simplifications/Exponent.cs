using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class Exponent
    {

        public static bool setRule(RPN.Node node)
        {
            return node.IsExponent();
        }

        public static bool functionRaisedToOneRunnable(RPN.Node node)
        {
            return node[0].IsNumber(1);
        }

        public static RPN.Node functionRaisedToOne(RPN.Node node)
        {
            return node[1];
        }

        public static bool functionRaisedToZeroRunnable(RPN.Node node)
        {
            return node[0].IsNumber(0);
        }

        public static RPN.Node functionRaisedToZero(RPN.Node node)
        {
            return new RPN.Node(1);
        }

        public static bool zeroRaisedToConstantRunnable(RPN.Node node)
        {
            return node[1].IsNumber(0) && node[0].IsGreaterThanNumber(0);
        }

        public static RPN.Node zeroRaisedToConstant(RPN.Node node)
        {
            return new RPN.Node(0);
        }

        public static bool oneRaisedToFunctionRunnable(RPN.Node node)
        {
            return node[1].IsNumber(1);
        }

        public static RPN.Node oneRaisedToFunction(RPN.Node node)
        {
            return new RPN.Node(1);
        }

        public static bool toDivisionRunnable(RPN.Node node)
        {
            return node[0].IsLessThanNumber(0);
        }

        public static RPN.Node toDivision(RPN.Node node)
        {
            RPN.Node powerClone = new RPN.Node(new[] { new RPN.Node(-1), node[0].Clone() },
                new RPN.Token("*", 2, RPN.Type.Operator));
            RPN.Node exponent = new RPN.Node(new[] { powerClone, node[1].Clone() },
                new RPN.Token("^", 2, RPN.Type.Operator));
            RPN.Node division = new RPN.Node(new[] { exponent, new RPN.Node(1) },
                new RPN.Token("/", 2, RPN.Type.Operator));
            return division;
        }

        public static bool toSqrtRunnable(RPN.Node node)
        {
            return node[0].IsNumber(0.5) || (node[0].IsDivision() && node[0, 0].IsNumber(2) && node[0,1].IsNumber(1));
        }

        public static RPN.Node toSqrt(RPN.Node node)
        {
            return new RPN.Node(new[] { node[1] }, new RPN.Token("sqrt", 1, RPN.Type.Function));
        }

        public static bool ExponentToExponentRunnable(RPN.Node node)
        {
            return node[1].IsExponent();
        }

        public static RPN.Node ExponentToExponent(RPN.Node node)
        {
            RPN.Node multiply;

            if (node[0].IsNumber() && node[1, 0].IsNumber())
            {
                multiply = new RPN.Node(node[0].GetNumber() * node[1, 0].GetNumber());
            }
            else
            {
                multiply = new RPN.Node(new[] { node[0].Clone(), node[1, 0].Clone() },
                    new RPN.Token("*", 2, RPN.Type.Operator));
            }

            RPN.Node exponent = new RPN.Node(new[] { multiply, node[1, 1].Clone() },
                new RPN.Token("^", 2, RPN.Type.Operator));
            return exponent;
        }

        public static bool ConstantRaisedToConstantRunnable(RPN.Node node)
        {
            return node[0].IsInteger() && node[1].IsInteger();
        }

        public static RPN.Node ConstantRaisedToConstant(RPN.Node node)
        {
            return new RPN.Node( Math.Pow( node[1].GetNumber() , node[0].GetNumber() ) );
        }



        public static bool NegativeConstantRaisedToAPowerOfTwoRunnable(RPN.Node node)
        {
            return node[0].IsNumberOrConstant() && node[1].IsLessThanNumber(0) && node[0].GetNumber() % 2 == 0;
        }

        public static RPN.Node NegativeConstantRaisedToAPowerOfTwo(RPN.Node node)
        {
            node.Replace(node[1], new RPN.Node(-1 * node[1].GetNumber()));
            return node;
        }

    }
}
