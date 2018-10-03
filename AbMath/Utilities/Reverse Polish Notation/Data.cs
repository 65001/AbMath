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
            private readonly Dictionary<string,Function> functions;
            private readonly Dictionary<string,Operator> operators;
            private readonly Dictionary<string, string> aliases;

            private readonly Dictionary<double, string> _autoFormat;

            private readonly List<string> leftbracket;
            private readonly List<string> rightbracket;
            private List<string> variables;
            private readonly Dictionary<string, string> variableStore;

            public IReadOnlyDictionary<string,Function> Functions => functions; 
            public IReadOnlyDictionary<string,Operator> Operators => operators; 
            public IReadOnlyDictionary<string, string> Aliases =>  aliases; 
            public IReadOnlyDictionary<double,string> Format => _autoFormat; 
            public IReadOnlyList<string> LeftBracket => leftbracket; 
            public IReadOnlyList<string> RightBracket => rightbracket;
            public IReadOnlyList<string> Variables => variables; 

            public string Equation;
            public Queue<Term> Polish { get; set; }
            public bool ContainsVariables { get; private set; }


            public Data(string equation)
            {
                Equation = equation;
                functions = new Dictionary<string, Function>();
                operators = new Dictionary<string, Operator>();
                aliases = new Dictionary<string, string>();
                _autoFormat = new Dictionary<double, string>();

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

            public void AddVariable(string token)
            {
                ContainsVariables = true;
                variables.Add(token);
                variables = variables.Distinct().ToList();
            }

            public void AddStore(string variable,string value)
            {
                AddVariable(variable);
                if (variableStore.ContainsKey(variable))
                {
                    variableStore[variable] = value;
                    return;
                }
                variableStore.Add(variable, value);
            }

            public void AddFunction(string key, Function func)
            {
                functions.Add(key, func);
            }

            public void AddOperator(string key, Operator ops)
            {
                operators.Add(key, ops);
            }

            public void AddFormat(double number, string format)
            {
                _autoFormat.Add(number,format);
            }

            public bool IsOperator(string value)
            {
                return Operators.ContainsKey(value);
            }

            public bool IsOperator(Term term)
            {
                return term.Type == Type.Operator;
            }

            public bool IsUnary(string value)
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
                AddAlias("Σ","sum");
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
                AddOperator("^", new Operator
                {
                    Assoc = Assoc.Right,
                    Weight = 4,
                    Arguments = 2,
                    Compute = DoOperators.Power
                });

                AddOperator("E", new Operator
                {
                    Assoc = Assoc.Right,
                    Weight = 4,
                    Arguments = 2,
                    Compute = DoOperators.E
                });

                AddOperator("!", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 4,
                    Arguments = 1,
                    Compute = DoOperators.Factorial
                });

                AddOperator("%", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 3,
                    Arguments = 2,
                    Compute = DoOperators.Mod
                });

                AddOperator("/", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 3,
                    Arguments = 2,
                    Compute = DoOperators.Divide
                });

                AddOperator("*", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 3,
                    Arguments = 2,
                    Compute = DoOperators.Multiply
                });

                AddOperator("+", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 2,
                    Arguments = 2,
                    Compute = DoOperators.Add
                });

                AddOperator("++", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 2,
                    Arguments = 1,
                    Compute = DoOperators.AddSelf
                });

                AddOperator("−", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 2,
                    Arguments = 2,
                    Compute = DoOperators.Subtract
                });

                AddOperator("-", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 2,
                    Arguments = 2,
                    Compute = DoOperators.Subtract
                });

                //Evaluations
                AddOperator(">", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 1,
                    Arguments = 2,
                    Compute = DoOperators.GreateerThan
                });

                AddOperator("<", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 1,
                    Arguments = 2,
                    Compute = DoOperators.LessThan
                });

                AddOperator("=", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 1,
                    Arguments = 2,
                    Compute = DoOperators.Equals
                });

                AddOperator("==", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 1,
                    Arguments = 2,
                    Compute = DoOperators.Equals
                });

                AddOperator(">=", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 1,
                    Arguments = 2,
                    Compute = DoOperators.GreaterThanOrEquals
                });

                AddOperator("<=", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 1,
                    Arguments = 2,
                    Compute = DoOperators.LessThanOrEquals
                });

                //Logic
                AddOperator("!=", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 1,
                    Arguments = 2,
                    Compute = DoOperators.NotEquals
                });

                AddOperator("&&", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 1,
                    Arguments = 2,
                    Compute = DoOperators.And
                });

                AddOperator("||", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 1,
                    Arguments = 2,
                    Compute = DoOperators.Or
                });

                //Assingment Operators
                //AddOperator(":", new Operators {Assoc = Assoc.Left,Weight = 0, Arguments = 2, Compute = new Run(DoOperators.Store));
                //AddOperator("<-", new Operators { Assoc = Assoc.Left, Weight = 0, Arguments = 2 });
            }

            void DefaultFunctions()
            {
                AddFunction("sin", new Function
                {
                    Arguments = 1,
                    Compute = DoFunctions.Sin
                });

                AddFunction("cos", new Function
                {
                    Arguments = 1,
                    Compute = DoFunctions.Cos
                });

                AddFunction("tan", new Function
                {
                    Arguments = 1,
                    Compute = DoFunctions.Tan
                });

                AddFunction("max", new Function
                {
                    Arguments = 2,
                    Compute = DoFunctions.Max
                });

                AddFunction("min", new Function
                {
                    Arguments = 2,
                    Compute = DoFunctions.Min
                });

                AddFunction("sqrt", new Function
                {
                    Arguments = 1,
                    Compute = DoFunctions.Sqrt
                });

                AddFunction("round", new Function
                {
                    Arguments = 2,
                    Compute = DoFunctions.Round
                });

                AddFunction("gcd", new Function
                {
                    Arguments = 2,
                    Compute = DoFunctions.Gcd
                });

                AddFunction("lcm", new Function
                {
                    Arguments = 2,
                    Compute = DoFunctions.Lcm
                });

                AddFunction("ln", new Function
                {
                    Arguments = 1,
                    Compute = DoFunctions.ln
                });

                AddFunction("log", new Function
                {
                    Arguments = 2,
                    Compute = DoFunctions.Log
                });

                AddFunction("pi", new Function
                {
                    Arguments = 0,
                    Compute = DoFunctions.Pi
                });

                AddFunction("e", new Function
                {
                    Arguments = 0,
                    Compute = DoFunctions.EContstant
                });

                AddFunction("bounded",new Function()
                    {
                        Arguments = 3,
                        Compute = DoFunctions.Bounded
                    }
                );

                AddFunction("sum", new Function()
                    {
                        Arguments = 3,
                        Compute = DoFunctions.Sum
                    }
                );
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