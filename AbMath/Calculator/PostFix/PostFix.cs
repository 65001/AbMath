using System;
using System.Diagnostics;
using System.Collections.Generic;
using CLI;

namespace AbMath.Calculator
{
    public class PostFix : IEvaluator<double>
    {
        private readonly RPN.DataStore _dataStore;
        private RPN.Token[] _input;
        private Stack<double> _stack;
        private Stack<RPN.Token> _variables;
        private readonly Stopwatch _stopwatch;

        public event EventHandler<string> Logger;

        //Sadly the PostFix part of the code must know of RPN..
        public PostFix(RPN RPN) : this(RPN.Data)
        {
        }

        public PostFix(RPN.DataStore dataStore)
        {
            _dataStore = dataStore;
            Reset();
            _variables = new Stack<RPN.Token>();
            _stopwatch = new Stopwatch();
        }

        public void SetVariable(string variable, double number)
        {
            SetVariable(variable, number.ToString());
        }

        public void SetVariable(string variable,string number)
        {
            int length = _input.Length;
            
            for (int i = 0; i < length; i++)
            {
                RPN.Token token = _input[i];
                if (token.Type == RPN.Type.Variable && token.Value == variable)
                {
                    _input[i] = (new RPN.Token {Arguments = 0,Type = RPN.Type.Number,Value = number });
                }
            }
        }

        public double Compute()
        {
            _stopwatch.Start();

            for (int i = 0; i < _input.Length; i++) { 
                RPN.Token token = _input[i];

                switch (token.Type)
                {
                    case RPN.Type.Number:
                        _stack.Push(double.Parse(token.Value));
                        break;
                    case RPN.Type.Variable:
                        _variables.Push(token);
                        break;
                    case RPN.Type.Operator:
                    {
                        RPN.Operator Operator = _dataStore.Operators[token.Value];
                        double[] arguments = GetArguments(token.Arguments);
                        double ans = Operator.Compute(arguments);
                        _stack.Push(ans);
                        break;
                    }
                    case RPN.Type.Function:
                    {
                        //Looks up the function in the Dict
                        RPN.Function function = _dataStore.Functions[token.Value];

                        double[] arguments = GetArguments(token.Arguments);
                        double ans = function.Compute(arguments);
                        _stack.Push(ans);
                        break;
                    }
                    default:
                        throw new NotImplementedException(token + " " + token.ToString().Length);
                }
            }

            _stopwatch.Stop();

            _dataStore.AddTimeRecord(new RPN.TimeRecord()
            {
                Type = "Evaluation",
                ElapsedMilliseconds = _stopwatch.ElapsedMilliseconds,
                ElapsedTicks = _stopwatch.ElapsedTicks
            });

            if (_dataStore.DebugMode)
            {
                Tables<string> times = new Tables<string>(new Config()
                {
                    Format = _dataStore.DefaultFormat,
                    Title = "Time"
                });

                times.Add(new Schema() {Column = "Type", Width = 18});
                times.Add(new Schema() {Column = "Time (ms)", Width = 10});
                times.Add(new Schema() {Column = "Ticks", Width = 8});
                times.Add(new Schema() {Column = "% Milliseconds", Width = 16});
                times.Add(new Schema() {Column = "% Ticks", Width = 9});

                for (int i = 0; i < _dataStore.Time.Count; i++)
                {
                    RPN.TimeRecord TR = _dataStore.Time[i];

                    times.Add(new string[]
                    {
                        TR.Type, TR.ElapsedMilliseconds.ToString(), TR.ElapsedTicks.ToString("N0"),
                        Math.Round((100 * TR.ElapsedMilliseconds / _dataStore.TotalMilliseconds), 2).ToString(),
                        (100 * TR.ElapsedTicks / _dataStore.TotalSteps).ToString("N0")
                    });
                }

                times.Add(new string[]
                {
                    "Total", _dataStore.TotalMilliseconds.ToString(), _dataStore.TotalSteps.ToString("N0"), " ", " "
                });

                Write(times.ToString());
                Write($"Frequency: {Stopwatch.Frequency}");
                Write("");
            }

            if (_stack.Count != 1) return double.NaN;

            if (_dataStore.Format.ContainsKey(_stack.Peek()))
            {
                Write($"The answer may also be written in the following manner: {_dataStore.Format[_stack.Peek()]}");
            }

            return _stack.Pop();
        }

        public static string GetDecimalFormat(double n)
        {
            
            string format = getDecimalFormat(n * Math.PI);
            if (format != null)
            {
                return "1/pi *" + format;
            }

            format = getDecimalFormat(n / Math.PI);
            if (format != null)
            {
                return "pi *" + format;
            }

            format = getDecimalFormat(n);
            if (format != null)
            {
                return format;
            }

            return null;
        }

        private static string getDecimalFormat(double value, double accuracy = 1E-4, int maxIteration = 10000)
        {
            //Algorithm from stack overflow. 
            try
            {
                if (accuracy <= 0.0 || accuracy >= 1.0)
                {
                    throw new ArgumentOutOfRangeException("accuracy", "Must be > 0 and < 1.");
                }

                int sign = Math.Sign(value);

                if (sign == -1)
                {
                    value = Math.Abs(value);
                }

                // Accuracy is the maximum relative error; convert to absolute maxError
                double maxError = sign == 0 ? accuracy : value * accuracy;

                int n = (int)Math.Floor(value);
                value -= n;

                if (value < maxError)
                {
                    return null;
                }

                if (1 - maxError < value)
                {
                    return null;
                }

                // The lower fraction is 0/1
                int lower_n = 0;
                int lower_d = 1;

                // The upper fraction is 1/1
                int upper_n = 1;
                int upper_d = 1;

                int i = 0;

                while (true)
                {
                    // The middle fraction is (lower_n + upper_n) / (lower_d + upper_d)
                    int middle_n = lower_n + upper_n;
                    int middle_d = lower_d + upper_d;

                    if (middle_d * (value + maxError) < middle_n)
                    {
                        // real + error < middle : middle is our new upper
                        upper_n = middle_n;
                        upper_d = middle_d;
                    }
                    else if (middle_n < (value - maxError) * middle_d)
                    {
                        // middle < real - error : middle is our new lower
                        lower_n = middle_n;
                        lower_d = middle_d;
                    }
                    else
                    {
                        int numerator = (n * middle_d + middle_n);
                        int denominator = middle_d;

                        if (numerator > 10000 || denominator > 10000)
                        {
                            return null;
                        }

                        // Middle is our best fraction
                        return $"{numerator * sign}/{denominator}";
                    }

                    i++;

                    if (i > maxIteration)
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private double[] GetArguments(int argCount)
        {
            double[] arguments = new double[argCount];
            
           if (_stack.Count < argCount )
            {
                throw new InvalidOperationException($"Syntax Error! Asked for {argCount} but only had {_stack.Count} in Stack.");
            }
            
            for (int i = argCount; i > 0; i--)
            {
                arguments[i - 1] = _stack.Pop();
            }
            return arguments;
        }

        public void Reset()
        {
            _input = new RPN.Token[_dataStore.Polish.Length];
            _dataStore.Polish.CopyTo(_input,0);
            _stack = new Stack<double>();
        }

        void Write(string message)
        {
            Logger?.Invoke(this, message);
        }
    }
}
