using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator.Functions;
using AbMath.Calculator.Operators;

namespace AbMath.Calculator.Simplifications
{
    public static class Log
    {
        public static bool LogOneRunnable(RPN.Node node)
        {
            return node.IsLog() && node[0].IsNumber(1);
        }

        public static RPN.Node LogOne(RPN.Node node)
        {
            return new RPN.Node(0);
        }

        public static bool LogIdentitcalRunnable(RPN.Node node)
        {
            return node.IsLog() && node.ChildrenAreIdentical();
        }

        public static RPN.Node LogIdentitcal(RPN.Node node)
        {
            return new RPN.Node(1);
        }

        public static bool LogPowerRunnable(RPN.Node node)
        {
            return node.IsExponent() && node[0].IsLog() && node[0, 1].Matches(node[1]);
        }

        public static RPN.Node LogPower(RPN.Node node)
        {
            return node[0, 0];
        }

        public static bool LnPowerRunnable(RPN.Node node)
        {
            return node.IsExponent() && node[0].IsLn() && node[1].IsConstant("e");
        }

        public static RPN.Node LnPower(RPN.Node node)
        {
            return node[0, 0];
        }

        public static bool LogExponentExpansionRunnable(RPN.Node node)
        {
            return node.IsLog() && node[0].IsExponent();
        }

        public static RPN.Node LogExponentExpansion(RPN.Node node)
        {
            RPN.Node exponent = node[0];
            RPN.Node baseNode = exponent[1];
            RPN.Node power = exponent[0];

            RPN.Node log = new RPN.Node(new[] { baseNode.Clone(), node.Children[1] }, new RPN.Token("log", 2, RPN.Type.Function));
            return new Mul(power, log);
        }

        public static bool LogToLnRunnable(RPN.Node node)
        {
            return node.IsLog() && node[1].IsConstant("e");
        }

        public static RPN.Node LogToLn(RPN.Node node)
        {
            return new RPN.Node(new RPN.Node[] { node[0] }, new RPN.Token("ln", 1, RPN.Type.Function));
        }

        public static bool LnToLogRunnable(RPN.Node node)
        {
            return node.IsLn();
        }

        public static RPN.Node LnToLog(RPN.Node node)
        {
            RPN.Node e = new RPN.Node(new RPN.Token("e", 0, RPN.Type.Function));
            RPN.Node log = new RPN.Node(new RPN.Node[] { node[0].Clone(), e }, new RPN.Token("log", 2, RPN.Type.Function));
            return log;
        }

        public static bool LogSummationRunnable(RPN.Node node)
        {
            return node.IsAddition() && node[0].IsLog() &&
                   node[1].IsLog() &&
                   node[0, 1].Matches(node[1, 1]);
        }

        public static RPN.Node LogSummation(RPN.Node node)
        {
            RPN.Node log = new RPN.Node(new[] { new Mul(node[1,0], node[0,0]) , node[0, 1] },
                new RPN.Token("log", 2, RPN.Type.Function));
            return log;
        }

        public static bool LogSubtractionRunnable(RPN.Node node)
        {
            return node.IsSubtraction() && node[0].IsLog() &&
                   node[1].IsLog() &&
                   node[0, 1].Matches(node[1, 1]);
        }

        public static RPN.Node LogSubtraction(RPN.Node node)
        {
            RPN.Node log = new RPN.Node(new[] { new Div(node[1,0], node[0,0]), node[0, 1] },
                new RPN.Token("log", 2, RPN.Type.Function));
            return log;
        }

        public static bool LnSummationRunnable(RPN.Node node)
        {
            return node.IsAddition() && node[0].IsLn() && node[1].IsLn();
        }

        public static RPN.Node LnSummation(RPN.Node node)
        {
            RPN.Node ln = new RPN.Node(new[] { new Mul(node[1,0], node[0,0]) },
                new RPN.Token("ln", 1, RPN.Type.Function));
            return ln;
        }

        public static bool LnSubtractionRunnable(RPN.Node node)
        {
            return node.IsSubtraction() && node[0].IsLn() && node[1].IsLn();
        }

        public static RPN.Node LnSubtraction(RPN.Node node)
        {
            RPN.Node ln = new RPN.Node(new[] { new Div(node[1,0], node[0,0]) },
                new RPN.Token("ln", 1, RPN.Type.Function));
            return ln;
        }

        public static bool LnPowerRuleRunnable(RPN.Node node)
        {
            return node.IsLn() && node[0].IsExponent() && !node[0,0].IsVariable();
        }

        public static RPN.Node LnPowerRule(RPN.Node node)
        {
            return new Mul(node[0, 0], new Ln(node[0, 1]) );
        } 
    }
}
