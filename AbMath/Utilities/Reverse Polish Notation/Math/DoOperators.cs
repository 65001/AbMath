using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public partial class RPN
    {

        public static class DoOperators
        {
            public static double AddSelf(params double[] Arguments)
            {
                return ++Arguments[0];
            }

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
                double max = Arguments[0];
                if (max < 0)
                {
                    return double.NaN;
                }

                double answer = 1;
                for (int i = 1; i <= max; i++)
                {
                    answer *= i;

                    if (answer >= double.PositiveInfinity)
                    {
                        break;
                    }
                }
                return answer;
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
                if ( (Arguments[0] == 1 && Arguments[1] == 1) ||(Arguments[0] == 0 && Arguments[1] == 0))
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

            public static double E(params double[] Arguments)
            {
                return Arguments[0] * Math.Pow(10, Arguments[1]);
            }

            public static void Store(ref DataStore dataStore,params string[] Arguments)
            {
                dataStore.AddStore(Arguments[0], Arguments[1]);
            }
        }
    }
}
