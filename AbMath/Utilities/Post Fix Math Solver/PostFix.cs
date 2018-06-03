using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public class PostFix
    {
        private RPN.Data Data;
        private Queue<string> Input;
        private Stack<double> Stack;

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
                string Token = Input.Dequeue();
                if (Data.IsVariable(Token) && Token == variable)
                {
                    Input.Enqueue(number);
                }
                else
                {
                    Input.Enqueue(Token);
                }
            }
        }

        public double Compute()
        {
            while (Input.Count > 0)
            {
                string Token = Input.Dequeue();
                if (Data.IsNumber(Token))
                {
                    Stack.Push(double.Parse(Token));
                }
                else if (Data.IsOperator(Token))
                {
                    RPN.Operators Operator = Data.Operators[Token];
                    double[] Arguments = GetArguments(Operator.Arguments);
                    Stack.Push(Operator.Compute(Arguments));
                }
                else if (Data.IsFunction(Token))
                {
                    RPN.Functions functions = Data.Functions[Token];
                    double[] Arguments = GetArguments(functions.Arguments);
                    Stack.Push(functions.Compute(Arguments));
                }
                else
                {
                    throw new NotImplementedException(Token + " " + Token.Length);
                }
            }

            if (Stack.Count == 1)
            {
                return Stack.Pop();
            }
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
            Input = new Queue<string>(Data.Polish);
            Stack = new Stack<double>();
        }
    }
}
