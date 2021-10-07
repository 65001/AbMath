using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AbMath.Calculator.RPN;

namespace AbMath.Calculator
{
    /// <summary>
    /// This class generates a valid DataStore to use in our code. 
    /// I'm moving the responsibility from DataStore to better reflect coding norms
    /// </summary>
    public class DataFactory
    {
        private static DataFactory singleton; 

        private DataFactory() {}

        public static DataFactory getInstance()
        {
            if (singleton == null)
            {
                singleton = new DataFactory();
            }
            return singleton;
        }


        public DataStore generate(string equation)
        {
            DataStore data = new DataStore(equation);
            DefaultLeftBracket(data);
            DefaultRightBracket(data);
            DefaultFunctions(data);
            DefaultOperators(data);
            DefaultAliases(data);
            DefaultFormat(data);
            return data;
        }

        private void DefaultLeftBracket(DataStore data)
        {
            data.AddLeftBracket("(")
                .AddLeftBracket("{")
                .AddLeftBracket("[");
        }

        private void DefaultRightBracket(DataStore data)
        {
            data.AddRightBracket(")")
                .AddRightBracket("}")
                .AddRightBracket("]")
                .AddRightBracket(",");
        }

        private void DefaultFunctions(DataStore data)
        {
            DefaultTrigFunctions(data);
            DefaultConstants(data);
            DefaultMetaCommands(data);

            Description max = new Description("max(a,b,...)", "Returns the highest value of all the passed in parameters.");
            Description min = new Description("min(a,b,...)", "Returns the lowest value of all the passed in parameters.");
            Description sqrt = new Description("sqrt(f(x))", "Returns the square root of f(x).");
            Description round = new Description();
            round.Add("round(a)", "Rounds 'a' to the nearest integer");
            round.Add("round(a,b)", "Rounds 'a' to the 'b' position.");
            round.Add("round(2.3) = 2");
            round.Add("round(2.6) = 3");
            round.Add("round(2.555,0) = 3");
            round.Add("round(2.555,1) = 2.6");
            round.Add("round(2.555,2) = 2.56");

            Description gcd = new Description("gcd(a,b)", "The greatest common denominator of 'a' and 'b'");
            Description lcm = new Description("lcm(a,b)", "The least common multiple of 'a' and 'b'");
            Description ln = new Description("ln(a)", "Takes the natural log of 'a'. Equivalent to log(e,a).");
            Description log = new Description("log(b,x)", "Takes the log of 'x' with a base of 'b'.\nx = b^y <-> log(b,x) = y");
            log.Add("log(x)", "Returns the natural log of a specified number");
            Description bounded = new Description("bounded(low,x,high)", "Returns low if (x < low)\nReturns high if (x > high)\nReturns x otherwise.");
            Description total = new Description("total(a_0,...,a_n)", "Totals up and returns the sum of all parameters.");
            Description sum = new Description("sum(f(x),x,a,b)", "Computes or returns the sum of f(x) from 'a' to 'b'.\n'x' shall represent the index variable.");
            Description avg = new Description("avg(a,...,b)", "Returns the average of all the passed in parameters.");

            Description random = new Description("random()", "Returns a non-negative random integer number.");
            random.Add("random(ceiling)", "Returns a non-negative integer number that is below the ceiling");
            random.Add("random(min,max)", "Returns a random integer that is between the min and maximum.");

            Description rand = new Description("rand()", "Returns a non-negative random integer number.");
            Description seed = new Description("seed(a)", "Sets the seed for the random number generator.");
            Description abs = new Description("abs(x)", "Returns the absolute value of 'x'.");
            Description binomial = new Description("binomial(n,k)", "Returns the value of (n!)/[k!(n - k)!].\nThis is the equivalent of (n choose k).\nRestrictions:0 <= k <= n");
            Description gamma = new Description("Γ(x)", "The gamma function is related to factorials as: Γ(x) = (x - 1)!.\nSince the gamma function is really hard to compute we are using Gergő Nemes Approximation.");

            data.AddFunction("max", new Function(2, 2, int.MaxValue, DoFunctions.Max, max))
                .AddFunction("min", new Function(2, 2, int.MaxValue, DoFunctions.Min, min))
                .AddFunction("sqrt", new Function(1, 1, 1, DoFunctions.Sqrt, sqrt))
                .AddFunction("round", new Function(1, 2, 2, DoFunctions.Round, round))
                .AddFunction("gcd", new Function(2, 2, 2, DoFunctions.Gcd, gcd))
                .AddFunction("lcm", new Function(2, 2, 2, DoFunctions.Lcm, lcm))
                .AddFunction("ln", new Function(1, 1, 1, DoFunctions.ln, ln))
                .AddFunction("log", new Function(1, 2, 2, DoFunctions.Log, log))
                .AddFunction("bounded", new Function(3, 3, 3, DoFunctions.Bounded, bounded))
                .AddFunction("total", new Function(1, 1, int.MaxValue, DoFunctions.Sum, total))
                .AddFunction("sum", new Function(4, 4, 4, sum))
                .AddFunction("avg", new Function(1, 1, int.MaxValue, DoFunctions.Avg, avg))
                .AddFunction("random", new Function(0, 0, 2, DoFunctions.Random, random))
                .AddFunction("rand", new Function(0, 0, 0, DoFunctions.Random, rand))
                .AddFunction("seed", new Function(1, 1, 1, DoFunctions.Seed, seed))
                .AddFunction("abs", new Function(1, 1, 1, DoFunctions.Abs, abs))
                .AddFunction("binomial", new Function(2, 2, 2, DoFunctions.Binomial, binomial))
                .AddFunction("Γ", new Function(1, 1, 1, DoFunctions.Gamma, gamma));
        }

        private void DefaultTrigFunctions(DataStore data)
        {
            data.AddFunction("sin", new Function(1, 1, 1, DoFunctions.Sin))
                .AddFunction("cos", new Function(1, 1, 1, DoFunctions.Cos))
                .AddFunction("tan", new Function(1, 1, 1, DoFunctions.Tan))
                .AddFunction("sec", new Function(1, 1, 1, DoFunctions.Sec))
                .AddFunction("csc", new Function(1, 1, 1, DoFunctions.Csc))
                .AddFunction("cot", new Function(1, 1, 1, DoFunctions.Cot))
                .AddFunction("arcsin", new Function(1, 1, 1, DoFunctions.Arcsin))
                .AddFunction("arccos", new Function(1, 1, 1, DoFunctions.Arccos))
                .AddFunction("arctan", new Function(1, 1, 1, DoFunctions.Arctan))
                .AddFunction("arcsec", new Function(1, 1, 1, DoFunctions.Arcsec))
                .AddFunction("arccsc", new Function(1, 1, 1, DoFunctions.Arccsc))
                .AddFunction("arccot", new Function(1, 1, 1, DoFunctions.Arccot))
                .AddFunction("rad", new Function(1, 1, 1, DoFunctions.rad))
                .AddFunction("deg", new Function(1, 1, 1, DoFunctions.deg));
        }

        private void DefaultConstants(DataStore data)
        {
            Description pi = new Description("π", "Returns the value of π.");
            Description euler = new Description("e", "Returns the euler number");

            data.AddFunction("π", new Function(0, 0, 0, DoFunctions.Pi, pi))
                .AddFunction("e", new Function(0, 0, 0, DoFunctions.EContstant, euler));
        }

        private void DefaultOperators(DataStore data)
        {
            data.AddOperator("^", new Operator(Assoc.Right, 5, 2, DoOperators.Power))
                .AddOperator("E", new Operator(Assoc.Right, 5, 2, DoOperators.E))
                .AddOperator("!", new Operator(Assoc.Left, 5, 1, DoOperators.Factorial))
                .AddOperator("%", new Operator(Assoc.Left, 4, 2, DoOperators.Mod))
                .AddOperator("/", new Operator(Assoc.Left, 4, 2, DoOperators.Divide))
                .AddOperator("*", new Operator(Assoc.Left, 4, 2, DoOperators.Multiply))
                .AddOperator("+", new Operator(Assoc.Left, 3, 2, DoOperators.Add))
                .AddOperator("++", new Operator(Assoc.Left, 3, 1, DoOperators.AddSelf))
                .AddOperator("−", new Operator(Assoc.Left, 3, 2, DoOperators.Subtract))
                .AddOperator("-", new Operator(Assoc.Left, 3, 2, DoOperators.Subtract));

            data.AddOperator(">", new Operator(Assoc.Left, 2, 2, DoOperators.GreaterThan))
                .AddOperator("<", new Operator(Assoc.Left, 2, 2, DoOperators.LessThan))
                .AddOperator("=", new Operator(Assoc.Left, 2, 2, DoOperators.Equals))
                .AddOperator("==", new Operator(Assoc.Left, 2, 2, DoOperators.Equals))
                .AddOperator(">=", new Operator(Assoc.Left, 2, 2, DoOperators.GreaterThanOrEquals))
                .AddOperator("<=", new Operator(Assoc.Left, 2, 2, DoOperators.LessThanOrEquals))
                .AddOperator("!=", new Operator(Assoc.Left, 1, 2, DoOperators.NotEquals))
                .AddOperator("&&", new Operator(Assoc.Left, 1, 2, DoOperators.And))
                .AddOperator("||", new Operator(Assoc.Left, 1, 2, DoOperators.Or));
        }

        private void DefaultAliases(DataStore data)
        {
            data.AddAlias("÷", "/")
                .AddAlias("gamma", "Γ")
                .AddAlias("pi", "π")
                .AddAlias("≠", "!=")
                .AddAlias("≥", ">=")
                .AddAlias("≤", "<=")
                .AddAlias("ne", "!=")
                .AddAlias("ge", ">=")
                .AddAlias("le", "<=")
                .AddAlias("and", "&&")
                .AddAlias("or", "||")
                .AddAlias("Σ", "sum")
                .AddAlias("infinity", "∞")
                .AddAlias("-infinity", "-∞");
        }

        private void DefaultFormat(DataStore data)
        {
            data.AddFormat(Math.Sqrt(2) / 2, "√2 / 2")
                .AddFormat(-Math.Sqrt(2) / 2, "-√2 / 2")
                .AddFormat(Math.Sqrt(3) / 2, "√3 / 2")
                .AddFormat(-Math.Sqrt(3) / 2, "-√3 / 2")
                .AddFormat(Math.PI / 2, "π/2")
                .AddFormat(Math.PI / 3, "π/3")
                .AddFormat(Math.PI / 4, "π/4");
        }

        private void DefaultMetaCommands(DataStore data)
        {
            Description derivative = new Description("derivative(f(x),x)", "Takes the derivative of f(x) in respect to x.");
            derivative.Add("derivative(f(x),x,n)", "");
            derivative.Add("derivative(f(x),x,2) = derivative(derivative(f(x),x),x)");
            
            
            data.AddMetaFunction("derivative", new Function(2, 2, 3, derivative));

            data.AddMetaFunction("integrate", new Function(4, 4, 5))
                .AddMetaFunction("table", new Function(4, 4, 5))
                .AddMetaFunction("solve", new Function(2, 2, 3))
                .AddMetaFunction("list", new Function(1, 2, int.MaxValue))
                .AddMetaFunction("plot", new Function(4, 4, 4));

            data.AddMetaFunction("derive")
                .AddMetaFunction("sum");
               
        }
    }
}
