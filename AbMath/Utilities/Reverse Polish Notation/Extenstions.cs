using System;
using System.Collections.Generic;
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
}
