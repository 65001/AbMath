using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using AbMath.Utilities;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public class DataStore
        {
            private readonly List<string> _meta_functions;

            private readonly Dictionary<string, Function> _functions;
            private readonly Dictionary<string, Operator> _operators;

            private readonly Dictionary<string, string> _aliases;

            private readonly Dictionary<double, string> _autoFormat;

            private readonly List<string> _leftbracket;
            private readonly List<string> _rightbracket;

            private List<TimeRecord> _time;
            private readonly Dictionary<string, string> _variableStore;

            protected internal object LockObject = new object();

            protected internal Logger Logger;

            /// <summary>
            /// A list of all the functions that are supported
            /// by this calculator.
            /// </summary>
            public IReadOnlyDictionary<string,Function> Functions => _functions;

            public IReadOnlyList<string> MetaFunctions => _meta_functions;

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

            public string SimplifiedEquation;

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

            public double TotalMilliseconds => this.Time.Where(t => !t.Type.Contains(".")).Sum(t => t.ElapsedMilliseconds);
            public double TotalSteps => this.Time.Where(t => !t.Type.Contains(".")).Sum(t => t.ElapsedTicks);

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
            /// Implicit multiplication in some interpretations
            /// of order of operations has a higher priority
            /// compared to that of division. 
            /// Set this to true to enable that feature.
            /// </summary>
            public bool ImplicitMultiplicationPriority;
            #endregion


            /// <summary>
            /// Determines the default format of CLI Tables
            /// based on the value of MarkdownTables
            /// </summary>
            public Format DefaultFormat => (MarkdownTables) ? Utilities.Format.MarkDown : Utilities.Format.Default ;

            public DataStore(string equation)
            {
                Equation = equation;
                _functions = new Dictionary<string, Function>();
                _meta_functions = new List<string>(5);
                _operators = new Dictionary<string, Operator>();
                _aliases = new Dictionary<string, string>();
                _autoFormat = new Dictionary<double, string>();

                _leftbracket = new List<string>() { "(", "{", "[" };
                _rightbracket = new List<string>() { ")", "}", "]", "," };

                _time = new List<TimeRecord>(4);
                _variableStore = new Dictionary<string, string>();
                Logger = new Logger();

                DefaultFunctions();
                DefaultOperators();
                DefaultAliases();
                DefaultFormats();
            }

            public void ClearTimeRecords()
            {
                _time.Clear();
            }

            public void SetEquation(string equation)
            {
                Equation = equation;
            }

            public void AddTimeRecord(string type, Stopwatch stopwatch)
            {
                AddTimeRecord(new TimeRecord
                {
                    ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                    ElapsedTicks = stopwatch.ElapsedTicks,
                    Type = type,
                    Count = 1
                });
            }

            public void AddTimeRecord(TimeRecord time)
            {
                if (_time.Count > 0 && _time[_time.Count - 1].Type == time.Type)
                {
                    TimeRecord prev = _time[_time.Count - 1];

                    prev.ElapsedMilliseconds += time.ElapsedMilliseconds;
                    prev.ElapsedTicks += time.ElapsedTicks;
                    prev.Count += 1;

                    _time[_time.Count - 1] = prev;
                }
                //If a Type contains a period it denotes that it should always be merged.
                else if (time.Type.Contains(".") && _time.Any(t => t.Type == time.Type) )
                {
                    int index = _time.FindIndex(t => t.Type == time.Type);
                    TimeRecord prev = _time[index];

                    prev.ElapsedMilliseconds += time.ElapsedMilliseconds;
                    prev.ElapsedTicks += time.ElapsedTicks;
                    prev.Count += 1;

                    _time[index] = prev;
                }
                else
                {
                    _time.Add(time);
                }
            }

            public Tables<string> TimeRecords()
            {
                Tables<string> times = new Tables<string>(new Config()
                {
                    Format = DefaultFormat,
                    Title = "Time"
                });

                times.Add(new Schema("Type"));
                times.Add(new Schema("# Calls"));
                times.Add(new Schema("Time (ms)"));

                times.Add(new Schema("Ticks"));
                times.Add(new Schema("% Milliseconds"));
                times.Add(new Schema("% Ticks"));

                double miliseconds = TotalMilliseconds;
                double steps = TotalSteps;

                for (int i = 0; i < _time.Count; i++)
                {
                    RPN.TimeRecord TR = _time[i];

                    times.Add(new string[]
                    {
                        TR.Type,
                        TR.Count.ToString(),
                        TR.ElapsedMilliseconds.ToString(),
                        TR.ElapsedTicks.ToString("N0"),
                        Math.Round((100 * TR.ElapsedMilliseconds / miliseconds), 2).ToString(),
                        (100 * TR.ElapsedTicks / steps).ToString("N0")
                    });
                }

                times.Add(new string[]
                {
                    "Total", "", miliseconds.ToString("N0") , steps.ToString("N0"), " ", " "
                });

                times.Add(new string[] {Equation, "", "", "", "", "" });

                return times;
            }

            public void AddLeftBracket(string value)
            {
                _leftbracket.Add(value);
            }
            
            public void AddLeftBracket(string[] value)
            {
                _leftbracket.AddRange(value);
            }

            public void AddRightBracket(string value)
            {
                _rightbracket.Add(value);
            }

            public void AddRightBracket(string[] value)
            {
                _rightbracket.AddRange(value);
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

            public void RemoveFunction(string function)
            {
                _functions.Remove(function);
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
                return value != "." &&  !(IsOperator(value) || IsFunction(value) || IsLeftBracket(value) || IsRightBracket(value) || IsNumber(value));
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
                AddAlias("gamma", "Γ");
                AddAlias("pi","π");
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

            private void DefaultOperators()
            {
                AddOperator("^", new Operator(Assoc.Right, 5, 2, DoOperators.Power));
                AddOperator("E", new Operator(Assoc.Right, 5, 2, DoOperators.E));

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

                AddFunction("sec", new Function
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Sec
                });

                AddFunction("csc", new Function
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Csc
                });

                AddFunction("cot", new Function
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Cot
                });

                AddFunction("arcsin", new Function
                {
                    Arguments = 1,
                    Compute = DoFunctions.Arcsin,
                    MaxArguments = 1,
                    MinArguments = 1
                });

                AddFunction("arccos", new Function
                {
                    Arguments = 1,
                    Compute = DoFunctions.Arccos,
                    MaxArguments = 1,
                    MinArguments = 1
                });

                AddFunction("arctan", new Function
                {
                    Arguments = 1,
                    Compute = DoFunctions.Arctan,
                    MaxArguments = 1,
                    MinArguments = 1
                });

                AddFunction("arcsec", new Function
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Arcsec
                });

                AddFunction("arccsc", new Function
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Arccsc
                });

                AddFunction("arccot", new Function
                {
                    MinArguments = 1,
                    Arguments = 1,
                    MaxArguments = 1,
                    Compute = DoFunctions.Arccot
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
                AddFunction("π", new Function
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

                AddFunction("total", new Function()
                {
                    Arguments = 1,
                    MinArguments = 1,
                    MaxArguments = int.MaxValue,
                    Compute = DoFunctions.Sum
                }
                );

                AddFunction("sum", new Function()
                {
                    Arguments = 1,
                    MinArguments = 4,
                    MaxArguments = 5
                });

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

                AddFunction("Γ", new Function()
                {
                    Arguments = 1,
                    Compute = DoFunctions.Gamma,
                    MaxArguments = 1,
                    MinArguments = 1
                });

                #region MetaCommands
                
                AddFunction("derivative", new Function()
                {
                    Arguments = 2,
                    MinArguments = 2,
                    MaxArguments = 3,
                });
                
                AddFunction("integrate", new Function()
                {
                    Arguments = 4,
                    MinArguments = 4,
                    MaxArguments = 5
                }
                );

                AddFunction("table", new Function()
                {
                    Arguments = 4,
                    MinArguments = 4,
                    MaxArguments = 5
                });

                AddFunction("solve", new Function()
                {
                    Arguments = 2,
                    MinArguments = 2,
                    MaxArguments = 3
                });

                /*
                AddFunction("plot", new Function()
                {
                    Arguments = 4,
                    MinArguments = 4,
                    MaxArguments = 4
                });
                */
                _meta_functions.Add("derivative");
                _meta_functions.Add("derive");
                _meta_functions.Add("integrate");
                _meta_functions.Add("table");
                _meta_functions.Add("plot");
                _meta_functions.Add("solve");
                _meta_functions.Add("sum");
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