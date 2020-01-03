using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class Sum
    {
        private static readonly RPN.Token _sum = new RPN.Token("sum", 4, RPN.Type.Function);

        public static bool setUp(RPN.Node node)
        {
            return node.IsFunction("sum");
        }

        public static bool PropagationRunnable(RPN.Node node)
        {
            return node[3].IsAddition() || node[3].IsSubtraction();
        }

        public static RPN.Node Propagation(RPN.Node node)
        {
            RPN.Token addToken = new RPN.Token("+", 2, RPN.Type.Operator);

            RPN.Node sum = new RPN.Node(new RPN.Node[] { node[0].Clone(), node[1].Clone(), node[2].Clone(), node[3, 1].Clone() }, _sum);
            node[3, 1].Remove(new RPN.Node(0));
            RPN.Node addition = new RPN.Node(new RPN.Node[] { sum, node.Clone() }, addToken);

            return addition;
        }

        public static bool VariableRunnable(RPN.Node node)
        {
            return (node[1].IsNumber(0) || node[1].IsNumber(1)) && node[3].IsVariable() && node[3].Matches(node[2]);
        }

        public static RPN.Node Variable(RPN.Node node)
        {
            RPN.Node one = new RPN.Node(1);
            RPN.Node two = new RPN.Node(2);

            RPN.Node addition = new RPN.Node(new RPN.Node[] {node[0].Clone(), one}, new RPN.Token("+", 2, RPN.Type.Operator));
            RPN.Node multiplication = new RPN.Node(new RPN.Node[] {addition, node[0]}, new RPN.Token("*", 2, RPN.Type.Operator));
            RPN.Node division = new RPN.Node(new RPN.Node[] {two, multiplication}, new RPN.Token("/", 2, RPN.Type.Operator));
            return division;
        }

        public static bool ConstantRunnable(RPN.Node node)
        {
            return (node[1].IsNumber(0) || node[1].IsNumber(1)) && node[3].IsNumberOrConstant();
        }

        public static RPN.Node Constant(RPN.Node node)
        {
            RPN.Node multiplication = new RPN.Node(new RPN.Node[] {node[3], node[0]}, new RPN.Token("*", 2, RPN.Type.Operator));
            return multiplication;
        }

        public static bool CoefficientRunnable(RPN.Node node)
        {
            return node[3].IsMultiplication() && node[3, 1].IsNumberOrConstant();
        }

        public static RPN.Node Coefficient(RPN.Node node)
        {
            RPN.Node sum = new RPN.Node(new RPN.Node[] {node[0].Clone(), node[1].Clone(), node[2].Clone(), node[3, 0]}, _sum);
            RPN.Node multiplication = new RPN.Node(new RPN.Node[] {sum, node[3, 1] }, new RPN.Token("*", 2, RPN.Type.Operator));
            
            return multiplication;
        }
    }
}
