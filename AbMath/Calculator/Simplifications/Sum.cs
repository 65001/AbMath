using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using AbMath.Calculator.Operators;

namespace AbMath.Calculator.Simplifications
{
    public static class Sum
    {
        private static readonly RPN.Token _sum = new RPN.Token("sum", 4, RPN.Type.Function);

        public static bool setUp(RPN.Node node)
        {
            return node.IsFunction("sum");
        }

        /// <summary>
        /// Code from: https://rosettacode.org/wiki/Bernoulli_numbers
        /// </summary>
        /// <param name="nth"></param>
        /// <returns></returns>
        public static RPN.Node getBernoulliNumber(int n)
        {
            if (n < 0)
            {
                throw new ArgumentOutOfRangeException("The Bernoulli number is not defined for values below zero.");
            }

            BigInteger f;
            BigInteger[] nu = new BigInteger[n + 1],
                de = new BigInteger[n + 1];
            for (int m = 0; m <= n; m++)
            {
                nu[m] = 1; de[m] = m + 1;
                for (int j = m; j > 0; j--)
                    if ((f = BigInteger.GreatestCommonDivisor(
                            nu[j - 1] = j * (de[j] * nu[j - 1] - de[j - 1] * nu[j]),
                            de[j - 1] *= de[j])) != BigInteger.One)
                    { nu[j - 1] /= f; de[j - 1] /= f; }
            }

            if (n == 1)
            {
                nu[0] = -1;
            }

            return new Div(new RPN.Node((double)nu[0]), new RPN.Node((double)de[0]));
        }

        public static bool PropagationRunnable(RPN.Node node)
        {
            return node[3].IsAddition() || node[3].IsSubtraction();
        }

        public static RPN.Node Propagation(RPN.Node node)
        {
            RPN.Node sum = new RPN.Node(new RPN.Node[] { node[0].Clone(), node[1].Clone(), node[2].Clone(), node[3, 1].Clone() }, _sum);
            bool isSubtraction = node[3].IsSubtraction();
            node.Replace(node[3], node[3,0]); //This saves a simplification step later

            if (isSubtraction)
            {
                return new Sub(sum, node.Clone());
            }

            RPN.Node addition = new Add(node.Clone(), sum);

            return addition;
        }


        public static bool VariableRunnable(RPN.Node node)
        {
            return (node[1].IsNumber(0) || node[1].IsNumber(1)) && node[3].IsVariable() && node[3].Matches(node[2]);
        }

        public static RPN.Node Variable(RPN.Node node)
        {
            return new Div( new Mul(node[0], new Add(node[0].Clone(), new RPN.Node(1)) ), new RPN.Node(2) );
        }

        public static bool ConstantComplexRunnable(RPN.Node node)
        {
            return node[3].IsNumberOrConstant() || (node[3].IsVariable() && !node[3].Matches(node[2])) ;
        }

        public static RPN.Node ConstantComplex(RPN.Node node)
        {
            RPN.Node subtraction = new RPN.Node(new RPN.Node[] {node[1], node[0]}, new RPN.Token("-", 2, RPN.Type.Operator));
            RPN.Node addition = new RPN.Node(new RPN.Node[] {subtraction, new RPN.Node(1)}, new RPN.Token("+", 2, RPN.Type.Operator));
            RPN.Node multiplication = new RPN.Node(new RPN.Node[] {node[3], addition}, new RPN.Token("*", 2, RPN.Type.Operator));
            return multiplication;
        }

        public static bool CoefficientRunnable(RPN.Node node)
        {
            return node[3].IsMultiplication() && node[3, 1].IsNumberOrConstant();
        }

        public static RPN.Node Coefficient(RPN.Node node)
        {
            RPN.Node sum = new RPN.Node(new RPN.Node[] {node[0].Clone(), node[1].Clone(), node[2].Clone(), node[3, 0]}, _sum);            
            return new Mul(node[3,1], sum);
        }

        public static bool CoefficientDivisionRunnable(RPN.Node node)
        {
            return node[3].IsDivision() && node[3, 0].IsNumberOrConstant();
        }

        public static RPN.Node CoefficientDivision(RPN.Node node)
        {
            RPN.Node sum = new RPN.Node(new RPN.Node[] { node[0].Clone(), node[1].Clone(), node[2].Clone(), node[3, 1] }, _sum);
            return new Div(sum ,node[3,0]);

        }

        public static bool PowerRunnable(RPN.Node node)
        {
            return ( node[1].IsInteger(1) || node[1].IsNumber(0) ) //start point must be 0 or 1 since 0^(c) = 0 when c > 0.
                   && node[3].IsExponent() 
                   && node[3, 0].IsInteger() && 
                   node[3, 0].IsGreaterThanNumber(0) && //ensures power is positive
                   node[3, 1].Matches(node[2]);
        }

        public static RPN.Node Power(RPN.Node node)
        {
            RPN.Node power = node[3, 0].Clone();
            RPN.Node end = node[0].Clone();

            RPN.Token _fac = new RPN.Token("!", 1, RPN.Type.Operator);
            RPN.Token _total = new RPN.Token("total", (int)power.GetNumber(), RPN.Type.Function);

            RPN.Node total = new RPN.Node(_total);

            double max = power.GetNumber();

            RPN.Node numeratorAddition = new RPN.Node(power.GetNumber() + 1); //(p + 1)

            for (int i = 0; i <= max; i++)
            {
                RPN.Node j = new RPN.Node(i);
                RPN.Node subtraction = new RPN.Node(power.GetNumber() - j.GetNumber()); //(p - j)
                RPN.Node addition = new RPN.Node(subtraction.GetNumber() + 1); //(p - j + 1)

                RPN.Node exponent = new Pow(end.Clone(), addition.Clone()); //n^(p - j + 1)
                RPN.Node bernoulli = Sum.getBernoulliNumber(i); //B(j)

                RPN.Node numerator = new RPN.Node(new RPN.Node[] { numeratorAddition.Clone() }, _fac); //(p + 1)!

                RPN.Node denominatorFactorial = new RPN.Node(new RPN.Node[] { addition.Clone() }, _fac); //(p - j + 1)!
                RPN.Node jFactorial = new RPN.Node(new RPN.Node[] { j.Clone() }, _fac); //j! 
                RPN.Node denominator = new Mul(jFactorial, denominatorFactorial);  // j! * (p - j + 1)!

                RPN.Node fraction = new Div(numerator, denominator);

                RPN.Node negativeOneExponent = new Pow(new RPN.Node(-1), j.Clone()); //(-1)^j
                RPN.Node multiplication = new Mul(new Mul(negativeOneExponent, fraction), new Mul(bernoulli, exponent));
                total.AddChild(multiplication);
            }
            return new Div(total, numeratorAddition.Clone());
        }
    }
}
