using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public static class Extensions
    {
        const string _cross = " ├─";
        const string _corner = " └─";
        const string _vertical = " │ ";
        const string _space = "   ";

        //Code: https://stackoverflow.com/questions/2094239/swap-two-items-in-listt
        public static List<T> Swap<T>(this List<T> array, int index, int index2)
        {
            T temp = array[index];
            array[index] = array[index2];
            array[index2] = temp;
            return array;
        }

        public static T[] Swap<T>(this T[] array, int index, int index2)
        {
            T temp = array[index];
            array[index] = array[index2];
            array[index2] = temp;
            return array;
        }

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

        //Code from https://andrewlock.net/creating-an-ascii-art-tree-in-csharp/
        public static string Print(this RPN.Node tree)
        {
            StringBuilder sb = new StringBuilder();
            PrintNode(tree, "", ref sb);
            return sb.ToString();
        }

        static void PrintNode(RPN.Node node, string indent, ref StringBuilder sb)
        {
            //node [Hash] ID:[$ID] Children:[$#]
            sb.AppendLine($"{node} [{node.ID} | {node.Children.Count} | {node.Token.Type} | {node.isRoot} | {node.GetHash()}]");

            // Loop through the children recursively, passing in the
            // indent, and the isLast parameter
            var numberOfChildren = node.Children.Count;
            for (var i = 0; i < numberOfChildren; i++)
            {
                var child = node.Children[i];
                var isLast = (i == (numberOfChildren - 1));
                PrintChildNode(child, indent, isLast, ref sb);
            }
        }

        static void PrintChildNode(RPN.Node node, string indent, bool isLast, ref StringBuilder sb)
        {
            // Print the provided pipes/spaces indent
            sb.Append(indent);

            // Depending if this node is a last child, print the
            // corner or cross, and calculate the indent that will
            // be passed to its children
            if (isLast)
            {
                sb.Append(_corner);
                indent += _space;
            }
            else
            {
                sb.Append(_cross);
                indent += _vertical;
            }

            PrintNode(node, indent, ref sb);
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
            list.RemoveRange(list.Count - count, count);
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
    }
}