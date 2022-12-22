using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator.Operators;

namespace AbMath.Calculator.Simplifications
{
    public static class Subtraction
    {
        public static bool setRule(RPN.Node node)
        {
            return node.IsSubtraction();
        }

        public static bool SameFunctionRunnable(RPN.Node node)
        {
            return node.ChildrenAreIdentical() && !node.ContainsDomainViolation();
        }

        public static RPN.Node SameFunction(RPN.Node node)
        {
            return new RPN.Node(0);
        }

        public static bool SameFunctionObstructedRunnable(RPN.Node node)
        {
            return node[1].IsAddition() && node[1, 0].Matches(node[0]);
        }

        public static RPN.Node SameFunctionObstructed(RPN.Node node)
        {
            return node[1, 1];
        }

        public static bool CoefficientOneReductionRunnable(RPN.Node node)
        {
            return node[1].IsMultiplication() && node[1, 1].IsNumber() && node[1, 0].Matches(node[0]);
        }

        public static RPN.Node CoefficientOneReduction(RPN.Node node)
        {
            node.Replace(node[0], new RPN.Node(0));
            node[1].Replace(node[1,1], new RPN.Node(node[1, 1].GetNumber() - 1));
            return node;
        }

        public static bool SubtractionByZeroRunnable(RPN.Node node)
        {
            return node[0].IsNumber(0);
        }

        public static RPN.Node SubtractionByZero(RPN.Node node)
        {
            return node[1];
        }

        public static bool ZeroSubtractedByFunctionRunnable(RPN.Node node)
        {
            return !(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[1].IsNumber(0);
        }

        public static RPN.Node ZeroSubtractedByFunction(RPN.Node node)
        {
            return new Mul(node[0], new RPN.Node(-1));
        }

        public static bool SubtractionDivisionCommonDenominatorRunnable(RPN.Node node)
        {
            return node[0].IsDivision() && node[1].IsDivision() && node[0, 0].Matches(node[1, 0]);
        }


        public static RPN.Node SubtractionDivisionCommonDenominator(RPN.Node node)
        {
            return new Div(new Sub(node[1,1], node[0,1]), node[0,0]);
        }

        public static bool CoefficientReductionRunnable(RPN.Node node)
        {
            return (node[0].IsMultiplication() && node[1].IsMultiplication()) &&
                   node[0,1].IsNumber() && node[1,1].IsNumber() &&
                   node[0,0].Matches(node[1,0]);
        }

        public static RPN.Node CoefficientReduction(RPN.Node node)
        {
            double coefficient = node[1, 1].GetNumber() - node[0, 1].GetNumber();
            node[0].Replace(node[0, 1], new RPN.Node(0));
            node[1].Replace(node[1, 1], new RPN.Node(coefficient));
            return node;
        }

        public static bool ConstantToAdditionRunnable(RPN.Node node)
        {
            return node[0].IsNumber() && node[0].IsLessThanNumber(0);
        }

        public static RPN.Node ConstantToAddition(RPN.Node node)
        {
            return new Add(node[1], new RPN.Node(node[0].GetNumber() * -1));
        }

        public static bool FunctionToAdditionRunnable(RPN.Node node)
        {
            //(cos(x)^2)-(-1*(sin(x)^2)) 
            //(cos(x)^2)-(-2*(sin(x)^2)) 
            //((-2*(cos(x)^2))+(2*(sin(x)^2)))
            return !(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[0].IsMultiplication() &&
                   node[0, 1].IsLessThanNumber(0);
        }

        public static RPN.Node FunctionToAddition(RPN.Node node)
        {
            node[0, 1].Replace(node[0, 1].GetNumber() * -1);
            node.Replace(new RPN.Token("+", 2, RPN.Type.Operator));
            return node;
        }

        public static bool DistributiveSimpleRunnable(RPN.Node node)
        {
            return node[0].IsSubtraction();
        }

        public static RPN.Node DistributiveSimple(RPN.Node node)
        {
            //f(x) - (g(x) - h(x)) -> f(x) - g(x) + h(x) -> (f(x) + h(x)) - g(x)
            //We want to do this automatically
            return new Sub(new Add(node[1], node[0,0]) ,  node[0,1]);
        }
    }
}
