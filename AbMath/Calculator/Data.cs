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
            public volatile bool DebugMode;

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

            public DataStore AddLeftBracket(string value)
            {
                _leftbracket.Add(value);
                return this;
            }
            
            public DataStore AddLeftBracket(string[] value)
            {
                _leftbracket.AddRange(value);
                return this;
            }

            public DataStore AddRightBracket(string value)
            {
                _rightbracket.Add(value);
                return this;
            }

            public DataStore AddRightBracket(string[] value)
            {
                _rightbracket.AddRange(value);
                return this;
            }

            public DataStore AddAlias(string key, string value)
            {
                _aliases.Add(key, value);
                return this;
            }

            public DataStore AddStore(string variable,string value)
            {
                if (_variableStore.ContainsKey(variable))
                {
                    _variableStore[variable] = value;
                    return this;
                }
                _variableStore.Add(variable, value);
                return this;
            }

            public DataStore AddFunction(string key, Function func)
            {
                _functions.Add(key, func);
                return this;
            }

            public DataStore AddMetaFunction(string key, Function func)
            {
                _functions.Add(key, func);
                _meta_functions.Add(key);
                return this;
            }

            public DataStore AddMetaFunction(string key)
            {
                _meta_functions.Add(key);
                return this;
            }

            public DataStore AddOperator(string key, Operator ops)
            {
                _operators.Add(key, ops);
                return this;
            }

            public DataStore AddFormat(double number, string format)
            {
                _autoFormat.Add(number,format);
                return this;
            }

            public DataStore RemoveFunction(string function)
            {
                _functions.Remove(function);
                return this;
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
                if (string.IsNullOrEmpty(value) || value == ".") { return Type.Null; }
                if (IsNumber(value)) { return Type.Number; }
                if (IsOperator(value)) { return Type.Operator; }
                if (IsFunction(value)) { return Type.Function; }
                if (IsLeftBracket(value)) { return Type.LParen; }
                if (IsRightBracket(value)) { return Type.RParen; }
                return Type.Variable;
            }
        }
    }
}