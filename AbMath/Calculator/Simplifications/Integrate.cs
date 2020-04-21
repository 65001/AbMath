using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class Integrate
    {
        private static readonly RPN.Token _integrate = new RPN.Token("integrate", 5, RPN.Type.Function);
        public static bool setUp(RPN.Node node)
        {
            return node.IsFunction("integrate");
        }

        public static bool PropagationRunnable(RPN.Node node)
        {
            return node[3].IsAddition() || node[3].IsSubtraction();
        }

        public static RPN.Node Propagation(RPN.Node node)
        {
            RPN.Token addToken = new RPN.Token("+", 2, RPN.Type.Operator);

            RPN.Node integral = new RPN.Node(new RPN.Node[] { node[0].Clone(), node[1].Clone(), node[2].Clone(), node[3, 1].Clone() }, _integrate);
            node.Replace(node[3], node[3, 0]); //This saves a simplification step later
            RPN.Node addition = new RPN.Node(new RPN.Node[] { integral, node.Clone() }, addToken);

            return addition;
        }
    }
}
