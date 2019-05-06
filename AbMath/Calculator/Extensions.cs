using System.Collections.Generic;
using System.Linq;
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

        public static string Print<T>(this T[] array)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i]);
                if (i < (array.Length - 1))
                {
                    sb.Append(" ");
                }
            }

            return sb.ToString();
        }

        public static string Print<T>(this List<T> list)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(list[i]);
                if (i < (list.Count - 1))
                {
                    sb.Append(" ");
                }
            }

            return sb.ToString();
        }

        public static string Print<T>(this IEnumerable<T> enumerable)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var data in enumerable)
            {
                sb.Append(data);
                sb.Append(" ");
            }

            return sb.ToString();
        }

        public static string Print<T>(this Stack<T> stack)
        {
            return stack.ToArray().Print();
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
        public static void Pop<T>(this List<T> list,int count)
        {
            for (int i = 0; i < count; i++)
            {
                list.RemoveAt(list.Count - 1);
            }
        }
    }

    public static class TermExtensions
    {
        public static bool IsNumber(this RPN.Token token)
        {
            return token.Type == RPN.Type.Number;
        }

        public static bool IsNull(this RPN.Token token)
        {
            return token.Type == RPN.Type.Null;
        }

        public static bool IsFunction(this RPN.Token token)
        {
            return token.Type == RPN.Type.Function;
        }

        public static bool IsConstant(this RPN.Token token)
        {
            return IsFunction(token) && token.Arguments == 0;
        }

        public static bool IsOperator(this RPN.Token token)
        {
            return token.Type == RPN.Type.Operator;
        }

        public static bool IsVariable(this RPN.Token token)
        {
            return token.Type == RPN.Type.Variable;
        }

        public static bool IsLeftBracket(this RPN.Token token)
        {
            return token.Type == RPN.Type.LParen;
        }

        public static bool IsRightBracket(this RPN.Token token)
        {
            return token.Type == RPN.Type.RParen;
        }

        public static bool IsComma(this RPN.Token token)
        {
            return token.Value == ",";
        }

        public static bool IsAddition(this RPN.Token token)
        {
            return token.Value == "+";
        }

        public static bool IsSubtraction(this RPN.Token token)
        {
            return token.Value == "-";
        }

        public static bool IsExponent(this RPN.Token token)
        {
            return token.Value == "^";
        }
    }
}
