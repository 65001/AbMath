using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public static class Extenstions
    {
        //Todo: Implement
        public static string Print<T>(this Queue<T> Queue)
        {
            int Length = Queue.Count;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Length; i++)
            {
                T value = Queue.Dequeue();
                Queue.Enqueue(value);
                sb.Append(value.ToString());

                if (i < (Length - 1))
                {
                    sb.Append(" ");
                }
            }
            return sb.ToString();
        }

        public static T SafePeek<T>(this Stack<T> stack)
        {
            if (stack.Count == 0)
            {
                return default(T);
            }
            T value = stack.Pop();
            stack.Push(value);
            return value;
        }
    }

    public static class TermExtensions
    {
        public static bool IsNumber(this Calculator.RPN.Term term)
        {
            return term.Type == Calculator.RPN.Type.Number;
        }

        public static bool IsNull(this Calculator.RPN.Term term)
        {
            return term.Type == Calculator.RPN.Type.Null;
        }

        public static bool IsFunction(this Calculator.RPN.Term term)
        {
            return term.Type == Calculator.RPN.Type.Function;
        }

        public static bool IsVariable(this Calculator.RPN.Term term)
        {
            return term.Type == Calculator.RPN.Type.Variable;
        }

        public static bool IsLeftBracket(this Calculator.RPN.Term term)
        {
            return term.Type == Calculator.RPN.Type.LParen;
        }

        public static bool IsRightBracket(this Calculator.RPN.Term term)
        {
            return term.Type == Calculator.RPN.Type.RParen;
        }
    }
}
