using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public class PostFix : IEvaluator<double>
    {
        private RPN.Data Data;
        private Queue<RPN.Term> Input;
        private Stack<double> Stack;
        private Stopwatch Stopwatch;

        public event EventHandler<string> Logger;

        //Sadly the PostFix part of the code must know of RPN..
        public PostFix(RPN RPN) 
        {
            Data = RPN.data;
            Reset();
        }

        public PostFix(RPN.Data data)
        {
            Data = data;
            Reset();
        }

        public void SetVariable(string variable,string number)
        {
            int Length = Input.Count;
            
            for (int i = 0; i < Length; i++)
            {
                RPN.Term Token = Input.Dequeue();
                if (Token.Type == RPN.Type.Variable && Token.Value == variable)
                {
                    Input.Enqueue(new RPN.Term {Arguments = 0,Type = RPN.Type.Number,Value = number });
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
                RPN.Term Token = Input.Dequeue();
                switch (Token.Type)
                {
                    case RPN.Type.Number:
                        Stack.Push(double.Parse(Token.Value));
                        break;
                    case RPN.Type.Variable:
                        break;
                    case RPN.Type.Operator:
                        {
                            RPN.Operator Operator = Data.Operators[Token.Value];
                            double[] Arguments = GetArguments(Token.Arguments);
                            double Ans = Operator.Compute(Arguments);
                            Stack.Push(Ans);
                        }
                        break;
                    case RPN.Type.Function:
                        {
                            RPN.Function function = Data.Functions[Token.Value];
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
                if (Data.Format.ContainsKey(Stack.Peek()))
                {
                    Write($"The answer may also be written in the following manner: {Data.Format[Stack.Peek()]}");
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
                throw new InvalidOperationException($"Syntax Error!");
            }
            
            for (int i = ArgCount; i > 0; i--)
            {
                Arguments[i - 1] = Stack.Pop();
            }
            return Arguments;
        }

        public void Reset()
        {
            Input = new Queue<RPN.Term>(Data.Polish);
            Stack = new Stack<double>();
        }

        void Write(string Message)
        {
            Logger?.Invoke(this, Message);
        }
    }
}
