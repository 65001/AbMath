using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public partial class RPN
    {
        public class DataStore
        {
            private readonly Dictionary<string,Function> _functions;
            private readonly Dictionary<string,Operator> _operators;
            private readonly Dictionary<string, string> _aliases;

            private readonly Dictionary<double, string> _autoFormat;

            private readonly List<string> _leftbracket;
            private readonly List<string> _rightbracket;
            private List<string> _variables;
            private readonly Dictionary<string, string> _variableStore;

            public IReadOnlyDictionary<string,Function> Functions => _functions; 
            public IReadOnlyDictionary<string,Operator> Operators => _operators; 
            public IReadOnlyDictionary<string, string> Aliases =>  _aliases; 
            public IReadOnlyDictionary<double,string> Format => _autoFormat; 
            public IReadOnlyList<string> LeftBracket => _leftbracket; 
            public IReadOnlyList<string> RightBracket => _rightbracket;
            public IReadOnlyList<string> Variables => _variables; 

            public string Equation;
            public Queue<Term> Polish { get; set; }
            public bool ContainsVariables { get; private set; }

            public DataStore(string equation)
            {
                Equation = equation;
                _functions = new Dictionary<string, Function>();
                _operators = new Dictionary<string, Operator>();
                _aliases = new Dictionary<string, string>();
                _autoFormat = new Dictionary<double, string>();

                _leftbracket = new List<string>();
                _rightbracket = new List<string>();
                _variables = new List<string>();
                _variableStore = new Dictionary<string, string>();

                DefaultFunctions();
                DefaultOperators();
                DefaultAliases();
                DefaultBrackets();
                DefaultFormats();
            }

            public void AddLeftBracket(string value)
            {
                _leftbracket.Add(value);
            }

            public void AddRightBracket(string value)
            {
                _rightbracket.Add(value);
            }

            public void AddAlias(string key, string value)
            {
                _aliases.Add(key, value);
            }

            public void AddVariable(string token)
            {
                ContainsVariables = true;
                _variables.Add(token);
                _variables = _variables.Distinct().ToList();
            }

            public void AddStore(string variable,string value)
            {
                AddVariable(variable);
                if (_variableStore.ContainsKey(variable))
                {
                    _variableStore[variable] = value;
                    return;
                }
                _variableStore.Add(variable, value);
            }

            public void AddFunction(string key, Function func)
            {
                _functions.Add(key, func);
            }

            public void AddOperator(string key, Operator ops)
            {
                _operators.Add(key, ops);
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

            private void DefaultAliases()
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

            private void DefaultBrackets()
            {
                AddLeftBracket("(");
                AddLeftBracket("{");
                AddLeftBracket("[");

                AddRightBracket(")");
                AddRightBracket("}");
                AddRightBracket("]");
                AddRightBracket(",");
            }

            private void DefaultOperators()
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

                #region Evaluation
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
                #endregion
                #region Logic
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
                #endregion
            }

            private void DefaultFunctions()
            {
                #region Trig
                AddFunction("sin", new Function
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Sin
                });

                AddFunction("cos", new Function
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Cos
                });

                AddFunction("tan", new Function
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Tan
                });
                #endregion

                AddFunction("max", new Function
                {
                    MinArguments = 2,
                    Arguments = 2,
                    MaxArguments = int.MaxValue,
                    Compute = DoFunctions.Max
                });

                AddFunction("min", new Function
                {
                    MinArguments = 2,
                    Arguments = 2,
                    MaxArguments = int.MaxValue,
                    Compute = DoFunctions.Min
                });

                AddFunction("sqrt", new Function
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Sqrt
                });

                AddFunction("round", new Function
                {
                    MinArguments = 1,
                    Arguments = 2,
                    MaxArguments = 2,
                    Compute = DoFunctions.Round
                });

                AddFunction("gcd", new Function
                {
                    MinArguments = 2,
                    Arguments = 2,
                    MaxArguments = 2,
                    Compute = DoFunctions.Gcd
                });

                AddFunction("lcm", new Function
                {
                    MinArguments = 2,
                    Arguments = 2,
                    MaxArguments = 2,
                    Compute = DoFunctions.Lcm
                });

                AddFunction("ln", new Function
                {
                    Arguments = 1,
                    MaxArguments = 1,
                    MinArguments = 1,
                    Compute = DoFunctions.ln
                });

                AddFunction("log", new Function
                {
                    MinArguments = 2,
                    Arguments = 2,
                    MaxArguments = 2,
                    Compute = DoFunctions.Log
                });

                #region Constants
                AddFunction("pi", new Function
                {
                    Arguments = 0,
                    MinArguments = 0,
                    MaxArguments = 1,
                    Compute = DoFunctions.Pi
                });

                AddFunction("e", new Function
                {
                    Arguments = 0,
                    MinArguments = 0,
                    MaxArguments = 1,
                    Compute = DoFunctions.EContstant
                });
                #endregion

                AddFunction("bounded",new Function()
                {
                    Arguments = 3,
                    MinArguments = 3,
                    MaxArguments = 3,
                    Compute = DoFunctions.Bounded
                }
                );

                AddFunction("sum", new Function()
                {
                    Arguments = 2,
                    MinArguments = 2,
                    MaxArguments = 2,
                    Compute = DoFunctions.Sum
                }
                );

                AddFunction("random", new Function()
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 2,
                    Compute = DoFunctions.Random
                }
                );

                AddFunction("rand", new Function()
                {
                    MinArguments = 0,
                    Arguments = 0,
                    MaxArguments = 0,
                    Compute = DoFunctions.Random
                });

                AddFunction("seed", new Function()
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Seed
                });

                AddFunction("abs", new Function()
                {
                    Arguments = 1,
                    Compute = DoFunctions.Abs,
                    MaxArguments = 1,
                    MinArguments = 1
                });
            }
            private void DefaultFormats()
            {
                AddFormat(Math.Sqrt(2)/2, "√2 / 2");
                AddFormat(- Math.Sqrt(2) / 2, "-√2 / 2");
                AddFormat(Math.Sqrt(3)/2, "√3 / 2");
                AddFormat(- Math.Sqrt(3) / 2, "-√3 / 2");
            }
        }
    }
}