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

            _dataStore.TotalMilliseconds += _stopwatch.ElapsedMilliseconds;
            _dataStore.TotalSteps += _stopwatch.ElapsedTicks;

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


        private int SeekNextFunction(int start)
        {
            for (int i = start; i < _input.Length; i++)
            {
                if (_input[i].IsFunction())
                {
                    RPN.Function func = _dataStore.Functions[_input[i].Value];
                    if (func.MaxArguments != func.MinArguments)
                    {
                        return i - start;
                    }
                }
            }

            return -1;
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
