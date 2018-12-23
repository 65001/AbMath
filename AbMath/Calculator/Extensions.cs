using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public static class Extensions
    {
        public static string Print<T>(this Queue<T> queue)
        {
            int length = queue.Count;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                var value = queue.Dequeue();
                queue.Enqueue(value);
                sb.Append(value);

                if (i < (length - 1))
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
            var value = stack.Pop();
            stack.Push(value);
            return value;
        }
    }

    public static class TermExtensions
    {
        public static bool IsNumber(this RPN.Term term)
        {
            return term.Type == RPN.Type.Number;
        }

        public static bool IsNull(this RPN.Term term)
        {
            return term.Type == RPN.Type.Null;
        }

        public static bool IsFunction(this RPN.Term term)
        {
            return term.Type == RPN.Type.Function;
        }

        public static bool IsOperator(this RPN.Term term)
        {
            return term.Type == RPN.Type.Operator;
        }

        public static bool IsVariable(this RPN.Term term)
        {
            return term.Type == RPN.Type.Variable;
        }

        public static bool IsLeftBracket(this RPN.Term term)
        {
            return term.Type == RPN.Type.LParen;
        }

        public static bool IsRightBracket(this RPN.Term term)
        {
            return term.Type == RPN.Type.RParen;
        }

        public static bool IsComma(this RPN.Term term)
        {
            return term.Value == ",";
        }
    }
}
