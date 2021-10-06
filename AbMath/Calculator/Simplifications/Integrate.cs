using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator.Operators;

namespace AbMath.Calculator.Simplifications
{
    public static class Integrate
    {
        private static readonly RPN.Token _integrate = new RPN.Token("integrate", 4, RPN.Type.Function);
        public static bool setUp(RPN.Node node)
        {
            return node.IsFunction("integrate");
        }

        public static bool PropagationRunnable(RPN.Node node)
        {
            return node.Children.Count == 4 && (node[3].IsAddition() || node[3].IsSubtraction());
        }

        public static RPN.Node Propagation(RPN.Node node)
        {
            RPN.Node integral = new RPN.Node(new RPN.Node[] { node[0].Clone(), node[1].Clone(), node[2].Clone(), node[3, 1].Clone() }, _integrate);
            node.Replace(node[3], node[3, 0]); //This saves a simplification step later
            return new Add(node.Clone(), integral);
        }

        public static bool ConstantsRunnable(RPN.Node node)
        {
            return node[3].IsNumberOrConstant() || !node[3].Contains(node[2]);
        }

        public static RPN.Node Constants(RPN.Node node)
        {
            return new Mul(node[3], new Sub(node[0], node[1]));
        }

        public static bool CoefficientRunnable(RPN.Node node)
        {
            return node[3].IsMultiplication() && (node[3, 1].IsNumberOrConstant() || !node[3, 1].Contains(node[2]));
        }

        public static RPN.Node Coefficient(RPN.Node node)
        {
            RPN.Node coefficient = node[3, 1].Clone();
            RPN.Node integral = new RPN.Node(new RPN.Node[] { node[0].Clone(), node[1].Clone(), node[2].Clone(), node[3, 0].Clone() }, _integrate);
            return new Mul(coefficient, integral);
        }

        public static bool SingleVariableRunnable(RPN.Node node)
        {
            return node[3].IsVariable() && node[3].IsVariable(node[2]);
        }

        public static RPN.Node SingleVariable(RPN.Node node)
        {
            RPN.Node subtraction = new Sub(new Pow(node[0], new RPN.Node(2)), new Pow(node[1], new RPN.Node(2))); 
            return new Div(subtraction, new RPN.Node(2));
        }
    }
}
