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
                if (string.IsNullOrEmpty(value) || value == ".") { return Type.Null; }
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
                AddOperator("!", new Operator(Assoc.Left, 5, 1, DoOperators.Factorial));

                AddOperator("%", new Operator(Assoc.Left, 4, 2, DoOperators.Mod));

                AddOperator("/", new Operator(Assoc.Left, 4, 2, DoOperators.Divide));

                AddOperator("*", new Operator(Assoc.Left, 4, 2, DoOperators.Multiply));

                AddOperator("+", new Operator(Assoc.Left, 3, 2, DoOperators.Add));

                AddOperator("++", new Operator(Assoc.Left, 3, 1, DoOperators.AddSelf));

                AddOperator("−", new Operator(Assoc.Left, 3, 2, DoOperators.Subtract));

                AddOperator("-", new Operator(Assoc.Left, 3, 2, DoOperators.Subtract));

#region Evaluation
                AddOperator(">", new Operator(Assoc.Left, 2, 2, DoOperators.GreaterThan));

                AddOperator("<", new Operator(Assoc.Left, 2, 2, DoOperators.LessThan));

                AddOperator("=", new Operator(Assoc.Left, 2, 2, DoOperators.Equals));

                AddOperator("==", new Operator(Assoc.Left, 2, 2, DoOperators.Equals));

                AddOperator(">=", new Operator(Assoc.Left, 2, 2, DoOperators.GreaterThanOrEquals));

                AddOperator("<=", new Operator(Assoc.Left, 2, 2, DoOperators.LessThanOrEquals));
#endregion
#region Logic
                AddOperator("!=", new Operator(Assoc.Left, 1, 2, DoOperators.NotEquals));

                AddOperator("&&", new Operator(Assoc.Left, 1, 2, DoOperators.And));

                AddOperator("||", new Operator(Assoc.Left, 1, 2, DoOperators.Or));
#endregion
            }

            private void DefaultFunctions()
            {
                #region Trig
                AddFunction("sin", new Function(1, 1, 1, DoFunctions.Sin));

                AddFunction("cos", new Function(1, 1, 1, DoFunctions.Cos));

                AddFunction("tan", new Function(1, 1, 1, DoFunctions.Tan));

                AddFunction("sec", new Function(1, 1, 1, DoFunctions.Sec));

                AddFunction("csc", new Function(1, 1, 1, DoFunctions.Csc));

                AddFunction("cot", new Function(1, 1, 1, DoFunctions.Cot));

                AddFunction("arcsin", new Function(1, 1, 1, DoFunctions.Arcsin));

                AddFunction("arccos", new Function(1, 1, 1, DoFunctions.Arccos));

                AddFunction("arctan", new Function(1, 1, 1, DoFunctions.Arctan));

                AddFunction("arcsec", new Function(1, 1, 1, DoFunctions.Arcsec));

                AddFunction("arccsc", new Function(1, 1, 1, DoFunctions.Arccsc));

                AddFunction("arccot", new Function(1, 1, 1, DoFunctions.Arccot));

                AddFunction("rad", new Function(1, 1, 1, DoFunctions.rad));

                AddFunction("deg", new Function(1, 1, 1, DoFunctions.deg));
                #endregion

                Description max = new Description();
                max.Add("max(a,b,...)","Returns the highest value of all the passed in parameters.");
                AddFunction("max", new Function(2,2,int.MaxValue,DoFunctions.Max, max));

                Description min = new Description();
                min.Add("min(a,b,...)","Returns the lowest value of all the passed in parameters.");
                AddFunction("min", new Function(2,2,int.MaxValue,DoFunctions.Min, min));

                Description sqrt = new Description();
                sqrt.Add("sqrt(f(x))","Returns the square root of f(x).");
                AddFunction("sqrt", new Function(1,1,1,DoFunctions.Sqrt, sqrt));

                Description round = new Description();
                round.Add("round(a)","Rounds 'a' to the nearest integer");
                round.Add("round(a,b)", "Rounds 'a' to the 'b' position.");
                round.Add("round(2.3) = 2");
                round.Add("round(2.6) = 3");
                round.Add("round(2.555,0) = 3");
                round.Add("round(2.555,1) = 2.6");
                round.Add("round(2.555,2) = 2.56");
                AddFunction("round", new Function(1, 2, 2, DoFunctions.Round, round));

                Description gcd = new Description();
                gcd.Add("gcd(a,b)", "The greatest common denominator of 'a' and 'b'");
                AddFunction("gcd", new Function(2, 2, 2, DoFunctions.Gcd, gcd));

                Description lcm = new Description("lcm(a,b)","The least common multiple of 'a' and 'b'");
                AddFunction("lcm", new Function(2, 2, 2, DoFunctions.Lcm, lcm));

                Description ln = new Description("ln(a)","Takes the natural log of 'a'. Equivalent to log(e,a).");
                AddFunction("ln", new Function(1, 1, 1, DoFunctions.ln, ln));

                Description log = new Description("log(b,x)","Takes the log of 'x' with a base of 'b'.\nx = b^y <-> log(b,x) = y");
                log.Add("log(x)","Returns the natural log of a specified number");
                AddFunction("log", new Function(1,2,2, DoFunctions.Log, log));

                #region Constants
                Description pi = new Description("π", "Returns the value of π.");
                AddFunction("π", new Function(0, 0, 0, DoFunctions.Pi, pi));

                Description euler = new Description("e","Returns the euler number");
                AddFunction("e", new Function(0, 0, 0, DoFunctions.EContstant, euler));
                #endregion

                Description bounded = new Description("bounded(low,x,high)","Returns low if (x < low)\nReturns high if (x > high)\nReturns x otherwise.");
                AddFunction("bounded",new Function(3, 3, 3, DoFunctions.Bounded, bounded));

                Description total = new Description("total(a_0,...,a_n)","Totals up and returns the sum of all parameters.");
                AddFunction("total", new Function(1, 1, int.MaxValue, DoFunctions.Sum, total));

                Description sum = new Description("sum(f(x),x,a,b)","Computes or returns the sum of f(x) from 'a' to 'b'.\n'x' shall represent the index variable.");
                AddFunction("sum", new Function(4, 4, 4, sum));

                Description avg = new Description("avg(a,...,b)","Returns the average of all the passed in parameters.");
                AddFunction("avg", new Function(1,1,int.MaxValue, DoFunctions.Avg, avg));

                Description random = new Description("random()","Returns a non-negative random integer number.");
                random.Add("random(ceiling)","Returns a non-negative integer number that is below the ceiling");
                random.Add("random(min,max)","Returns a random integer that is between the min and maximum.");
                AddFunction("random", new Function(0, 0, 2, DoFunctions.Random, random));

                Description rand = new Description("rand()", "Returns a non-negative random integer number.");
                AddFunction("rand", new Function(0, 0, 0, DoFunctions.Random, rand));

                Description seed = new Description("seed(a)","Sets the seed for the random number generator.");
                AddFunction("seed", new Function(1, 1, 1, DoFunctions.Seed, seed));

                Description abs = new Description("abs(x)","Returns the absolute value of 'x'.");
                AddFunction("abs", new Function(1, 1, 1, DoFunctions.Abs, abs));

                Description binomial = new Description("binomial(n,k)","Returns the value of (n!)/[k!(n - k)!].\nThis is the equivalent of (n choose k).\nRestrictions:0 <= k <= n");
                AddFunction("binomial", new Function(2,2,2,DoFunctions.Binomial, binomial));

                Description gamma = new Description("Γ(x)", "The gamma function is related to factorials as: Γ(x) = (x - 1)!.\nSince the gamma function is really hard to compute we are using Gergő Nemes Approximation.");
                AddFunction("Γ", new Function(1, 1, 1, DoFunctions.Gamma, gamma));

                #region MetaCommands
                
                Description derivative = new Description("derivative(f(x),x)","Takes the derivative of f(x) in respect to x.");
                derivative.Add("derivative(f(x),x,n)","");
                derivative.Add("derivative(f(x),x,2) = derivative(derivative(f(x),x),x)");
                AddFunction("derivative", new Function(2, 2, 3, derivative));
                

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

                AddFunction("list", new Function()
                {
                    Arguments = 2,
                    MinArguments = 1,
                    MaxArguments = int.MaxValue
                });

                _meta_functions.Add("derivative");
                _meta_functions.Add("derive");
                _meta_functions.Add("integrate");
                _meta_functions.Add("table");
                _meta_functions.Add("plot");
                _meta_functions.Add("solve");
                _meta_functions.Add("sum");

                _meta_functions.Add("list");
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