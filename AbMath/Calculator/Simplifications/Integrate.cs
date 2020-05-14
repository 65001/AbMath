using System;
using System.Collections.Generic;
using System.Text;

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
            RPN.Token addToken = new RPN.Token("+", 2, RPN.Type.Operator);

            RPN.Node integral = new RPN.Node(new RPN.Node[] { node[0].Clone(), node[1].Clone(), node[2].Clone(), node[3, 1].Clone() }, _integrate);
            node.Replace(node[3], node[3, 0]); //This saves a simplification step later
            RPN.Node addition = new RPN.Node(new RPN.Node[] { integral, node.Clone() }, addToken);

            return addition;
        }

        public static bool ConstantsRunnable(RPN.Node node)
        {
            return node[3].IsNumberOrConstant() || !node[3].Contains(node[2]);
        }

        public static RPN.Node Constants(RPN.Node node)
        {
            RPN.Node subtraction = new RPN.Node(new RPN.Node[] {node[1], node[0]}, new RPN.Token("-",2,RPN.Type.Operator));
            RPN.Node multiplication = new RPN.Node(new RPN.Node[] {subtraction, node[3]}, new RPN.Token("*",2, RPN.Type.Operator));
            return multiplication;
        }

        public static bool CoefficientRunnable(RPN.Node node)
        {
            return node[3].IsMultiplication() && (node[3, 1].IsNumberOrConstant() || !node[3, 1].Contains(node[2]));
        }

        public static RPN.Node Coefficient(RPN.Node node)
        {
            RPN.Node coefficient = node[3, 1].Clone();
            RPN.Node integral = new RPN.Node(new RPN.Node[] { node[0].Clone(), node[1].Clone(), node[2].Clone(), node[3, 0].Clone() }, _integrate);
            RPN.Node multiplication = new RPN.Node(new RPN.Node[] {integral, coefficient}, new RPN.Token("*", 2, RPN.Type.Operator));
            return multiplication;
        }

        public static bool SingleVariableRunnable(RPN.Node node)
        {
            return node[3].IsVariable() && node[3].IsVariable(node[2]);
        }

        public static RPN.Node SingleVariable(RPN.Node node)
        {
            RPN.Node end = new RPN.Node(new RPN.Node[] { new RPN.Node(2), node[0] }, new RPN.Token("^", 2, RPN.Type.Operator));
            RPN.Node start = new RPN.Node(new RPN.Node[] {  new RPN.Node(2), node[1] }, new RPN.Token("^", 2, RPN.Type.Operator));
            RPN.Node subtraction = new RPN.Node(new RPN.Node[] {start, end}, new RPN.Token("-", 2, RPN.Type.Operator));
            RPN.Node division = new RPN.Node(new RPN.Node[] {new RPN.Node(2), subtraction}, new RPN.Token("/", 2, RPN.Type.Operator));
            return division;
        }
    }
}
