using System;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public static class DoFunctions
        {
            private static Random _rand;

            public static double Sin(params double[] arguments)
            {
                // -1 <= sin(x) <= 1
                return Math.Sin(arguments[0]);
            }

            public static double Cos(params double[] arguments)
            {
                return Math.Cos(arguments[0]);
            }

            public static double Tan(params double[] arguments)
            {
                return Math.Tan(arguments[0]);
            }

            public static double Arcsin(params double[] arguments)
            {
                return Math.Asin(arguments[0]);
            }

            public static double Arccos(params double[] arguments)
            {
                return Math.Acos(arguments[0]);
            }

            public static double Arctan(params double[] arguments)
            {
                return Math.Atan(arguments[0]);
            }

            public static double Sqrt(params double[] arguments)
            {
                return Math.Sqrt(arguments[0]);
            }

            public static double Round(params double[] arguments)
            {
                double digits;
                if (arguments.Length == 2) { digits = arguments[1]; }
                else { digits = 0; }
                return Math.Round(arguments[0] * Math.Pow(10, digits)) / Math.Pow(10, digits);
            }

            public static double Max(params double[] arguments)
            {
                double max = arguments[0];
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (arguments[i] > max)
                    {
                        max = arguments[i];
                    }
                }

                return max;
            }

            public static double Min(params double[] arguments)
            {
                double min = arguments[0];
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (arguments[i] < min)
                    {
                        min = arguments[i];
                    }
                }

                return min;
            }

            /// <summary>
            /// Pass in a double array of three values and get the bounded value back
            /// 0 - Test, 1 - Floor , 2 - Ceiling
            /// </summary>
            /// <param name="arguments"></param>
            /// <returns>the bounded value</returns>
            public static double Bounded(params double[] arguments)
            {
               
                return Math.Max(arguments[1], Math.Min(arguments[0], arguments[2]));
            }

            public static double Lcm(params double[] arguments)
            {
                return (arguments[0] * arguments[1]) / Gcd(arguments);
            }

            public static double Gcd(params double[] arguments)
            {
                arguments[0] = Math.Abs(arguments[0]);
                arguments[1] = Math.Abs(arguments[1]);
                return (arguments[1] == 0) ? arguments[0] : Gcd(arguments[1], arguments[0] % arguments[1]);
            }

            public static double ln(params double[] arguments)
            {
                return Math.Log(arguments[0]);
            }

            public static double Log (params double[] arguments)
            {
                if (arguments.Length == 1)
                {
                    return Math.Log(arguments[0]);
                }
                return Math.Log(arguments[0], arguments[1]);
            }

            public static double Seed(params double[] arguments)
            {
                _rand = new Random((int)arguments[0]);
                return double.NaN;
            }

            public static double Random(params double[] arguments)
            {
                if (_rand == null)
                {
                    _rand = new Random();
                }

                if (arguments.Length == 0)
                {
                    return _rand.Next();
                }
                return arguments.Length == 1 ? _rand.Next((int)arguments[0]) : _rand.Next((int)arguments[0], (int)arguments[1]);
            }

            public static double Sum(params double[] arguments)
            {
                double sum = 0;
                for (int i = 0; i < arguments.Length; i++)
                {
                    sum += arguments[i];
                }
                return sum;
            }

            public static double Avg(params double[] args)
            {
                return Sum(args) / args.Length;
            }

            public static double Abs(params double[] arguments)
            {
                return Math.Abs(arguments[0]);
            }

            public static double rad(params double[] arguments)
            {
                return arguments[0] * Math.PI/180;
            }

            public static double deg(params double[] arguments)
            {
                return arguments[0] * 180/Math.PI;
            }

            //Constants 
            public static double Pi(params double[] arguments)
            {
                return Math.PI;
            }

            public static double EContstant(params double[] arguments)
            {
                return Math.E;
            }
        }
    }
}
