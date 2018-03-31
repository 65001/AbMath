using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public partial class RPN
    {
        public static class DoMath
        {
            public static double Add(params double[] Arguments)
            {
                return Arguments[0] + Arguments[1];
            }

            public static double Subtract(params double[] Arguments)
            {
                return Arguments[0] - Arguments[1];
            }

            public static double Divide(params double[] Arguments)
            {
                if (Arguments[1] == 0)
                {
                    return double.NaN;
                }
                return Arguments[0] / Arguments[1];
            }

            public static double Multiply(params double[] Arguments)
            {
                return Arguments[0] * Arguments[1];
            }

            public static double Power(params double[] Arguments)
            {
                return Math.Pow(Arguments[0], Arguments[1]);
            }

            public static double Mod(params double[] Arguments)
            {
                return Arguments[0] % Arguments[1];
            }

            //TODO Implement
            public static double Factorial(params double[] Arguments)
            {
                double i = Arguments[0];
                if (i <= 1)
                    return 1;
                return i * Factorial(i - 1);
            }

            public static double GreateerThan(params double[] Arguments)
            {
                if (Arguments[0] > Arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double LessThan(params double[] Arguments)
            {
                if (Arguments[0] < Arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double GreaterThanOrEquals(params double[] Arguments)
            {
                if (Arguments[0] >= Arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double LessThanOrEquals(params double[] Arguments)
            {
                if (Arguments[0] <= Arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double Equals(params double[] Arguments)
            {
                if (Arguments[0] == Arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double NotEquals(params double[] Arguments)
            {
                if (Arguments[0] != Arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double And(params double[] Arguments)
            {
                if ( (Arguments[0] == 1 && Arguments[1] == 1)
                     ||
                     (Arguments[0] == 0 && Arguments[1] == 0)
                    )
                {
                    return 1;
                }
                return 0;
            }

            public static double Or(params double[] Arguments)
            {
                if ( Arguments[0] == 1 || Arguments[1] == 1 )
                {
                    return 1;
                }
                return 0;
            }
        }
    }
}
