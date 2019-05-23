using System;
using System;
using System.Linq;
using System.Collections.Generic;
using CLI;

namespace AbMath.Calculator
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
            private List<TimeRecord> _time;
            private readonly Dictionary<string, string> _variableStore;

            /// <summary>
            /// A list of all the functions that are supported
            /// by this calculator.
            /// </summary>
            public IReadOnlyDictionary<string,Function> Functions => _functions; 

            /// <summary>
            /// A list of all operators that are supported
            /// by this calculator.
            /// </summary>
            public IReadOnlyDictionary<string,Operator> Operators => _operators; 
            /// <summary>
            /// A dictionary of expressions that the calculator
            /// treats as equivalent. 
            /// </summary>
            public IReadOnlyDictionary<string, string> Aliases =>  _aliases; 

            /// <summary>
            /// A dictionary of numerical constants and known
            /// representations of them
            /// </summary>
            public IReadOnlyDictionary<double,string> Format => _autoFormat; 

            /// <summary>
            /// A list of all strings that would
            /// start a function or work as grouping symbols 
            /// </summary>
            public IReadOnlyList<string> LeftBracket => _leftbracket; 

            /// <summary>
            /// A list of all strings that end function calls and
            /// terminate grouping symbols
            /// </summary>
            public IReadOnlyList<string> RightBracket => _rightbracket;

            /// <summary>
            /// A list of variables that the calculator
            /// has found in an expression
            /// </summary>
            public IReadOnlyList<string> Variables => Polish.Where(t => t.Type == Type.Variable).Select(t => t.Value).Distinct().ToList();

            public IReadOnlyList<TimeRecord> Time => _time;

            /// <summary>
            /// The equation passed to the calculator 
            /// </summary>
            public string Equation;
            public Token[] Polish { get; set; }

            /// <summary>
            /// Whether an expression contains variables
            /// </summary>
            public bool ContainsVariables => Polish.Any(t => t.Type == Type.Variable);

            /// <summary>
            /// Whether an expression contains a evaluator
            /// such as =, > , > or a combination of those
            /// operators.
            /// </summary>
            public bool ContainsEquation => 
                Equation.Contains("=") || Equation.Contains(">") || Equation.Contains("<") ;

            public double TotalMilliseconds => this.Time.Sum(t => t.ElapsedMilliseconds);
            public double TotalSteps => this.Time.Sum(t => t.ElapsedTicks);

            #region Config
            /// <summary>
            /// If true, auto generated debug tables
            /// will write data in markdown format
            /// </summary>
            public bool MarkdownTables;

            public bool AllowMismatchedParentheses;

            /// <summary>
            /// If true, the program will write tables to the
            /// log
            /// </summary>
            public bool DebugMode;

            /// <summary>
            /// This type of optimization can remove some
            /// variables entirely or combine them to optimize
            /// the calculation over many iterations.
            /// These optimizations cannot optimize across a grouping symbols.
            /// </summary>
            public bool PreOptimization = true;

            /// <summary>
            /// This type of optimizations can remove
            /// variables entirely or combine them to
            /// optimize the calculations over many iterations.
            /// Log simplification rules and other similar rules are implemented
            /// here.
            /// These optimizations happen on the Reverse Polish Notation and
            /// hence can optimize across a traditional grouping symbol. 
            /// </summary>
            public bool PostOptimization = true;
            #endregion


            /// <summary>
            /// Determines the default format of CLI Tables
            /// based on the value of MarkdownTables
            /// </summary>
            public Format DefaultFormat => (MarkdownTables) ? CLI.Format.MarkDown : CLI.Format.Default ;

            public DataStore(string equation)
            {
                Equation = equation;
                _functions = new Dictionary<string, Function>();
                _operators = new Dictionary<string, Operator>();
                _aliases = new Dictionary<string, string>();
                _autoFormat = new Dictionary<double, string>();

                _leftbracket = new List<string>();
                _rightbracket = new List<string>();
                _time = new List<TimeRecord>(4);
                _variableStore = new Dictionary<string, string>();

                DefaultFunctions();
                DefaultOperators();
                DefaultAliases();
                DefaultBrackets();
                DefaultFormats();
            }

            public void AddTimeRecord(TimeRecord time)
            {
                _time.Add(time);
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

            public void AddStore(string variable,string value)
            {
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

            public bool IsOperator(Token token)
            {
                return token.Type == Type.Operator;
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
                AddAlias("Γ","gamma");
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
                AddAlias("infinity","∞");
                AddAlias("-infinity", "-∞");
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
                    Weight = 5,
                    Arguments = 2,
                    Compute = DoOperators.Power
                });

                AddOperator("E", new Operator
                {
                    Assoc = Assoc.Right,
                    Weight = 5,
                    Arguments = 2,
                    Compute = DoOperators.E
                });

                AddOperator("!", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 5,
                    Arguments = 1,
                    Compute = DoOperators.Factorial
                });

                AddOperator("%", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 4,
                    Arguments = 2,
                    Compute = DoOperators.Mod
                });

                AddOperator("/", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 4,
                    Arguments = 2,
                    Compute = DoOperators.Divide
                });

                AddOperator("*", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 4,
                    Arguments = 2,
                    Compute = DoOperators.Multiply
                });

                AddOperator("+", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 3,
                    Arguments = 2,
                    Compute = DoOperators.Add
                });

                AddOperator("++", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 3,
                    Arguments = 1,
                    Compute = DoOperators.AddSelf
                });

                AddOperator("−", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 3,
                    Arguments = 2,
                    Compute = DoOperators.Subtract
                });

                AddOperator("-", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 3,
                    Arguments = 2,
                    Compute = DoOperators.Subtract
                });

#region Evaluation
                AddOperator(">", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 2,
                    Arguments = 2,
                    Compute = DoOperators.GreaterThan
                });

                AddOperator("<", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 2,
                    Arguments = 2,
                    Compute = DoOperators.LessThan
                });

                AddOperator("=", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 2,
                    Arguments = 2,
                    Compute = DoOperators.Equals
                });

                AddOperator("==", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 2,
                    Arguments = 2,
                    Compute = DoOperators.Equals
                });

                AddOperator(">=", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 2,
                    Arguments = 2,
                    Compute = DoOperators.GreaterThanOrEquals
                });

                AddOperator("<=", new Operator
                {
                    Assoc = Assoc.Left,
                    Weight = 2,
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

                AddFunction("arcsin", new Function()
                {
                    Arguments = 1,
                    Compute = DoFunctions.Arcsin,
                    MaxArguments = 1,
                    MinArguments = 1
                });

                AddFunction("arccos", new Function()
                {
                    Arguments = 1,
                    Compute = DoFunctions.Arccos,
                    MaxArguments = 1,
                    MinArguments = 1
                });

                AddFunction("arctan", new Function()
                {
                    Arguments = 1,
                    Compute = DoFunctions.Arctan,
                    MaxArguments = 1,
                    MinArguments = 1
                });

                AddFunction("rad", new Function()
                {
                    Arguments = 1,
                    Compute = DoFunctions.rad,
                    MaxArguments = 1,
                    MinArguments = 1
                });

                AddFunction("deg", new Function()
                {
                    Arguments = 1,
                    Compute = DoFunctions.deg,
                    MaxArguments = 1,
                    MinArguments = 1
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
                    MinArguments = 1,
                    Arguments = 2,
                    MaxArguments = 2,
                    Compute = DoFunctions.Log
                });

#region Constants
                AddFunction("pi", new Function
                {
                    Arguments = 0,
                    MinArguments = 0,
                    MaxArguments = 0,
                    Compute = DoFunctions.Pi
                });

                AddFunction("e", new Function
                {
                    Arguments = 0,
                    MinArguments = 0,
                    MaxArguments = 0,
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
                    Arguments = 1,
                    MinArguments = 1,
                    MaxArguments = int.MaxValue,
                    Compute = DoFunctions.Sum
                }
                );

                AddFunction("avg", new Function()
                {
                    Arguments = 1,
                    MinArguments = 1,
                    MaxArguments = int.MaxValue,
                    Compute = DoFunctions.Avg
                });

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

                AddFunction("gamma", new Function()
                {
                    Arguments = 1,
                    Compute = DoFunctions.Gamma,
                    MaxArguments = 1,
                    MinArguments = 1
                });

                #region MetaCommands
                AddFunction("derive", new Function()
                {
                    Arguments = 2,
                    MinArguments = 2,
                    MaxArguments = 2,
                });

                AddFunction("integrate", new Function()
                {
                    Arguments = 4,
                    MinArguments = 4,
                    MaxArguments = 4
                }
                );

                AddFunction("table", new Function()
                {
                    Arguments = 4,
                    MinArguments = 4,
                    MaxArguments = 4
                });

                AddFunction("plot", new Function()
                {
                    Arguments = 4,
                    MinArguments = 4,
                    MaxArguments = 4
                });
                #endregion
            }
            private void DefaultFormats()
            {
                AddFormat(Math.Sqrt(2)/2, "√2 / 2");
                AddFormat(- Math.Sqrt(2) / 2, "-√2 / 2");

                AddFormat(Math.Sqrt(3)/2, "√3 / 2");
                AddFormat(- Math.Sqrt(3) / 2, "-√3 / 2");

                AddFormat(Math.PI / 2, "π/2");

                AddFormat(Math.PI / 3, "π/3");

                AddFormat(Math.PI / 4, "π/4");
            }
        }
    }
}