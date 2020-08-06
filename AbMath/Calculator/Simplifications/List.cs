using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class List
    {
        public static bool setRule(RPN.Node node)
        {
            return node.IsFunction("list") || node.IsFunction("matrix");
        }

        public static bool singleElementRunnable(RPN.Node node)
        {
            return node.Children.Count == 1;
        }

        public static RPN.Node singleElement(RPN.Node node)
        {
            return node[0];
        }

        public static bool convertVectorToMatrixRunnable(RPN.Node node)
        {
            return node.IsFunction("list") && node.Children.TrueForAll(n => n.IsFunction("list"));
        }

        public static RPN.Node convertVectorToMatrix(RPN.Node node)
        {
            node.Token.Value = "matrix";
            return node;
        }

    }
}
