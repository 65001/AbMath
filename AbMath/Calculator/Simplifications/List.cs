using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator.Simplifications
{
    public static class List
    {
        public static bool setRule(RPN.Node node)
        {
            return node.IsFunction("list") || node.IsFunction("matrix") || node.IsOperator();
        }

        public static bool singleElementRunnable(RPN.Node node)
        {
            return !node.IsOperator() && node.Children.Count == 1;
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

        //{a,b,c,d}-f
        /**
         * AST Simplified Infix : {a,b,c,d}-f
           - [7 | 2 | Operator | True | ea8e4abd68164eeb2055ac51b1fe75c6]
           ├─f [6 | 0 | Variable | False | 8fa14cdd754f91cc6554c9e71929cce7]
           └─list [5 | 4 | Function | False | 6e428e7310356df8d8351466442eb6d1]
           ├─d [4 | 0 | Variable | False | 8277e0910d750195b448797616e091ad]
           ├─c [3 | 0 | Variable | False | 4a8a08f09d37b73795649038408b5f33]
           ├─b [2 | 0 | Variable | False | 92eb5ffee6ae2fec3ad71c777531578f]
           └─a [1 | 0 | Variable | False | 0cc175b9c0f1b6a831c399e269772661]
         */
        public static bool VectorFrontScalarBackRunnable(RPN.Node node)
        {
            return node.IsOperator() && node.Token.Arguments == 2 &&
                   node[0].IsScalar() &&
                   node[1].IsFunction("list");
        }

        //This is what we want our end result to look like
        /**
         * AST Simplified Infix : {a-f,b-f,c-f,d-f}
           list [13 | 4 | Function | True | 7c563ab9893e876c49fcc316a78e6b6d]
           ├─- [12 | 2 | Operator | False | 281861c9d3dfdecb3ddffdc8930cb63e]
           │  ├─f [11 | 0 | Variable | False | 8fa14cdd754f91cc6554c9e71929cce7]
           │  └─d [10 | 0 | Variable | False | 8277e0910d750195b448797616e091ad]
           ├─- [9 | 2 | Operator | False | 4ad3c11a7d979f70332d8dbc8ab33f43]
           │  ├─f [8 | 0 | Variable | False | 8fa14cdd754f91cc6554c9e71929cce7]
           │  └─c [7 | 0 | Variable | False | 4a8a08f09d37b73795649038408b5f33]
           ├─- [6 | 2 | Operator | False | df41b29ce151a927ae80c6df545518c1]
           │  ├─f [5 | 0 | Variable | False | 8fa14cdd754f91cc6554c9e71929cce7]
           │  └─b [4 | 0 | Variable | False | 92eb5ffee6ae2fec3ad71c777531578f]
           └─- [3 | 2 | Operator | False | d5c11d5ccf6dc77d16eaf012db8813fa]
              ├─f [2 | 0 | Variable | False | 8fa14cdd754f91cc6554c9e71929cce7]
              └─a [1 | 0 | Variable | False | 0cc175b9c0f1b6a831c399e269772661]
         */
        public static RPN.Node VectorFrontScalarBack(RPN.Node node)
        {
            RPN.Token operatorToken = node.Token;
            


            return node;
        }

    }
}
