using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public class PostFix : IEvaluator<double>
    {
        private Calculator.RPN.DataStore _dataStore;
        private Queue<Calculator.RPN.Term> Input;
        private Stack<double> Stack;
        private Stopwatch Stopwatch;

        public event EventHandler<string> Logger;

        //Sadly the PostFix part of the code must know of RPN..
        public PostFix(Calculator.RPN RPN) 
        {
            _dataStore = RPN.Data;
            Reset();
        }

        public PostFix(Calculator.RPN.DataStore dataStore)
        {
            _dataStore = dataStore;
            Reset();
        }

        public void SetVariable(string variable,string number)
        {
            int Length = Input.Count;
            
            for (int i = 0; i < Length; i++)
            {
                Calculator.RPN.Term Token = Input.Dequeue();
                if (Token.Type == Calculator.RPN.Type.Variable && Token.Value == variable)
                {
                    Input.Enqueue(new Calculator.RPN.Term {Arguments = 0,Type = Calculator.RPN.Type.Number,Value = number });
                }
                else
                {
                    Input.Enqueue(Token);
                }
            }
        }

        public double Compute()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (Input.Count > 0)
            {
                Calculator.RPN.Term Token = Input.Dequeue();
                switch (Token.Type)
                {
                    case Calculator.RPN.Type.Number:
                        Stack.Push(double.Parse(Token.Value));
                        break;
                    case Calculator.RPN.Type.Variable:
                        break;
                    case Calculator.RPN.Type.Operator:
                        {
                            Calculator.RPN.Operator Operator = _dataStore.Operators[Token.Value];
                            double[] Arguments = GetArguments(Token.Arguments);
                            double Ans = Operator.Compute(Arguments);
                            Stack.Push(Ans);
                        }
                        break;
                    case Calculator.RPN.Type.Function:
                        {
                            //Looks up the function in the Dict
                            Calculator.RPN.Function function = _dataStore.Functions[Token.Value];

                            double[] Arguments = GetArguments(Token.Arguments);
                            double Ans = function.Compute(Arguments);
                            Stack.Push(Ans);
                        }
                        break;
                    default:
                        throw new NotImplementedException(Token + " " + Token.ToString().Length);
                }
            }

            if (Stack.Count == 1)
            {
                stopwatch.Stop();
                Write($"Evaluation Time: {stopwatch.ElapsedMilliseconds} (ms) {stopwatch.ElapsedTicks} Ticks");
                if (_dataStore.Format.ContainsKey(Stack.Peek()))
                {
                    Write($"The answer may also be written in the following manner: {_dataStore.Format[Stack.Peek()]}");
                }
                return Stack.Pop();
            }

            stopwatch.Stop();
            Write($"Evaluation Time: {stopwatch.ElapsedMilliseconds} (ms) {stopwatch.ElapsedTicks} Ticks");
            return double.NaN;
        }

        private double[] GetArguments(int ArgCount)
        {
            double[] Arguments = new double[ArgCount];
            
           if (Stack.Count < ArgCount )
            {
                throw new InvalidOperationException($"Syntax Error! Asked for {ArgCount} but only had {Stack.Count} in Stack");
            }
            
            for (int i = ArgCount; i > 0; i--)
            {
                Arguments[i - 1] = Stack.Pop();
            }
            return Arguments;
        }

        public void Reset()
        {
            Input = new Queue<Calculator.RPN.Term>(_dataStore.Polish);
            Stack = new Stack<double>();
        }

        void Write(string Message)
        {
            Logger?.Invoke(this, Message);
        }
    }
}
