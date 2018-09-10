using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public partial class RPN
    {
        public class Data
        {
            Dictionary<string,Functions> functions;
            Dictionary<string,Operators> operators;
            Dictionary<string, string> aliases;

            Dictionary<double, string> autoFormat;

            List<string> leftbracket;
            List<string> rightbracket;
            List<string> variables;
            Dictionary<string, string> variableStore;

            public IReadOnlyDictionary<string,Functions> Functions { get { return functions; } }
            public IReadOnlyDictionary<string,Operators> Operators { get { return operators; } }
            public IReadOnlyDictionary<string, string> Aliases { get { return aliases; ; } }
            public IReadOnlyDictionary<double,string> Format { get { return autoFormat; }}
            public IReadOnlyList<string> LeftBracket { get { return leftbracket; } }
            public IReadOnlyList<string> RightBracket { get { return rightbracket; } }
            public IReadOnlyList<string> Variables { get { return variables; } }

            public string Equation;
            public Queue<Term> Polish { get; set; }
            public bool ContainsVariables { get; private set; }


            public Data(string equation)
            {
                Equation = equation;
                functions = new Dictionary<string, Functions>();
                operators = new Dictionary<string, Operators>();
                aliases = new Dictionary<string, string>();
                autoFormat = new Dictionary<double, string>();

                leftbracket = new List<string>();
                rightbracket = new List<string>();
                variables = new List<string>();
                variableStore = new Dictionary<string, string>();

                DefaultFunctions();
                DefaultOperators();
                DefaultAliases();
                DefaultBrackets();
                DefaultFormats();
            }

            public void AddLeftBracket(string value)
            {
                leftbracket.Add(value);
            }

            public void AddRightBracket(string value)
            {
                rightbracket.Add(value);
            }

            public void AddAlias(string key, string value)
            {
                aliases.Add(key, value);
            }

            public void AddVariable(string Token)
            {
                ContainsVariables = true;
                variables.Add(Token);
                variables = variables.Distinct().ToList();
            }

            public void AddStore(string Variable,string Value)
            {
                AddVariable(Variable);
                if (variableStore.ContainsKey(Variable))
                {
                    variableStore[Variable] = Value;
                    return;
                }
                variableStore.Add(Variable, Value);
            }

            public void AddFunction(string Key, Functions Func)
            {
                functions.Add(Key, Func);
            }

            public void AddOperator(string Key, Operators Ops)
            {
                operators.Add(Key, Ops);
            }

            public void AddFormat(double number, string format)
            {
                autoFormat.Add(number,format);
            }

            public bool IsOperator(string value)
            {
                return Operators.ContainsKey(value);
            }

            public bool IsOperator(Term term)
            {
                return term.Type == Type.Operator;
            }

            public bool IsUniary(string value)
            {
                return IsOperator(value) && (value == "-" || value == "−" || value == "+");
            }

            public bool IsNumber(string value)
            {
                return double.TryParse(value, out double data);
            }

            public bool IsFunction(string value)
            {
                return Functions.ContainsKey(value);
            }

            public bool IsVariable(string value)
            {
                return value != "." &&  !(IsNumber(value) || IsOperator(value) || IsFunction(value) || IsLeftBracket(value) || IsRightBracket(value));
            }

            public bool IsLeftBracket(string value)
            {
                return LeftBracket.Contains(value);
            }

            public bool IsRightBracket(string value)
            {
                return RightBracket.Contains(value);
            }

            public Type Resolve(string value)
            {
                if (IsNumber(value)) { return Type.Number; }
                if (IsOperator(value)) { return Type.Operator; }
                if (IsFunction(value)) { return Type.Function; }
                if (IsLeftBracket(value)) { return Type.LParen; }
                if (IsRightBracket(value)) { return Type.RParen; }
                return Type.Variable;
            }

            void DefaultAliases()
            {
                AddAlias("÷", "/");
                AddAlias("π", "pi");
                AddAlias("≠", "!=");
                AddAlias("≥", ">=");
                AddAlias("≤", "<=");
                AddAlias("ne","!=");
                AddAlias("ge",">=");
                AddAlias("le","<=");
                AddAlias("and","&&");
                AddAlias("or","||");
            }

            void DefaultBrackets()
            {
                AddLeftBracket("(");
                AddLeftBracket("{");
                AddLeftBracket("[");

                AddRightBracket(")");
                AddRightBracket("}");
                AddRightBracket("]");
                AddRightBracket(",");
            }

            void DefaultOperators()
            {
                AddOperator("^", new Operators
                {
                    Assoc = Assoc.Right,
                    weight = 4,
                    Arguments = 2,
                    Compute = new Run(DoOperators.Power)
                });

                AddOperator("E", new Operators
                {
                    Assoc = Assoc.Right,
                    weight = 4,
                    Arguments = 2,
                    Compute = new Run(DoOperators.E)
                });

                AddOperator("!", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 4,
                    Arguments = 1,
                    Compute = new Run(DoOperators.Factorial)
                });

                AddOperator("%", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 3,
                    Arguments = 2,
                    Compute = new Run(DoOperators.Mod)
                });

                AddOperator("/", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 3,
                    Arguments = 2,
                    Compute = new Run(DoOperators.Divide)
                });

                AddOperator("*", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 3,
                    Arguments = 2,
                    Compute = new Run(DoOperators.Multiply)
                });

                AddOperator("+", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 2,
                    Arguments = 2,
                    Compute = new Run(DoOperators.Add)
                });

                AddOperator("++", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 2,
                    Arguments = 1,
                    Compute = new Run(DoOperators.AddSelf)
                });

                AddOperator("−", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 2,
                    Arguments = 2,
                    Compute = new Run(DoOperators.Subtract)
                });

                AddOperator("-", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 2,
                    Arguments = 2,
                    Compute = new Run(DoOperators.Subtract)
                });

                //Evaluations
                AddOperator(">", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 1,
                    Arguments = 2,
                    Compute = new Run(DoOperators.GreateerThan)
                });

                AddOperator("<", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 1,
                    Arguments = 2,
                    Compute = new Run(DoOperators.LessThan)
                });

                AddOperator("=", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 1,
                    Arguments = 2,
                    Compute = new Run(DoOperators.Equals)
                });

                AddOperator(">=", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 1,
                    Arguments = 2,
                    Compute = new Run(DoOperators.GreaterThanOrEquals)
                });

                AddOperator("<=", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 1,
                    Arguments = 2,
                    Compute = new Run(DoOperators.LessThanOrEquals)
                });

                //Logic
                AddOperator("!=", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 1,
                    Arguments = 2,
                    Compute = new Run(DoOperators.NotEquals)
                });

                AddOperator("&&", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 1,
                    Arguments = 2,
                    Compute = new Run(DoOperators.And)
                });

                AddOperator("||", new Operators
                {
                    Assoc = Assoc.Left,
                    weight = 1,
                    Arguments = 2,
                    Compute = new Run(DoOperators.Or)
                });

                //Assingment Operators
                //AddOperator(":", new Operators {Assoc = Assoc.Left,weight = 0, Arguments = 2, Compute = new Run(DoOperators.Store));
                //AddOperator("<-", new Operators { Assoc = Assoc.Left, weight = 0, Arguments = 2 });
            }

            void DefaultFunctions()
            {
                AddFunction("sin", new Functions
                {
                    Arguments = 1,
                    Compute = new Run(DoFunctions.Sin)
                });

                AddFunction("cos", new Functions
                {
                    Arguments = 1,
                    Compute = new Run(DoFunctions.Cos)
                });

                AddFunction("tan", new Functions
                {
                    Arguments = 1,
                    Compute = new Run(DoFunctions.Tan)
                });

                AddFunction("max", new Functions
                {
                    Arguments = 2,
                    Compute = new Run(DoFunctions.Max)
                });

                AddFunction("min", new Functions
                {
                    Arguments = 2,
                    Compute = new Run(DoFunctions.Min)
                });

                AddFunction("sqrt", new Functions
                {
                    Arguments = 1,
                    Compute = new Run(DoFunctions.Sqrt)
                });

                AddFunction("round", new Functions
                {
                    Arguments = 2,
                    Compute = new Run(DoFunctions.Round)
                });

                AddFunction("gcd", new Functions
                {
                    Arguments = 2,
                    Compute = new Run(DoFunctions.Gcd)
                });

                AddFunction("lcm", new Functions
                {
                    Arguments = 2,
                    Compute = new Run(DoFunctions.Lcm)
                });

                AddFunction("ln", new Functions
                {
                    Arguments = 1,
                    Compute = new Run(DoFunctions.ln)
                });

                AddFunction("log", new Functions
                {
                    Arguments = 2,
                    Compute = new Run(DoFunctions.Log)
                });

                AddFunction("pi", new Functions
                {
                    Arguments = 0,
                    Compute = new Run(DoFunctions.Pi)
                });

                AddFunction("e", new Functions
                {
                    Arguments = 0,
                    Compute = new Run(DoFunctions.EContstant)
                });
            }

            void DefaultFormats()
            {
                AddFormat(Math.Sqrt(2)/2, "√2 / 2");
                AddFormat(- Math.Sqrt(2) / 2, "-√2 / 2");
                AddFormat(Math.Sqrt(3)/2, "√3 / 2");
                AddFormat(- Math.Sqrt(3) / 2, "-√3 / 2");
            }
        }
    }
}