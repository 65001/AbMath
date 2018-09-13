using System;
using System.Collections.Generic;
using static AbMath.Utilities.RPN;
using System.Text;

namespace AbMath.Utilities
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
        public static bool IsNumber(this Term term)
        {
            return term.Type == RPN.Type.Number;
        }

        public static bool IsNull(this Term term)
        {
            return term.Type == RPN.Type.Null;
        }

        public static bool IsFunction(this Term term)
        {
            return term.Type == RPN.Type.Function;
        }

        public static bool IsVariable(this Term term)
        {
            return term.Type == RPN.Type.Variable;
        }

        public static bool IsLeftBracket(this Term term)
        {
            return term.Type == RPN.Type.LParen;
        }

        public static bool IsRightBracket(this Term term)
        {
            return term.Type == RPN.Type.RParen;
        }
    }
}
