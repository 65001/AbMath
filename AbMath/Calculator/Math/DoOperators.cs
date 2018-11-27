using System;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public static class DoOperators
        {
            public static double AddSelf(params double[] arguments)
            {
                return ++arguments[0];
            }

            public static double Add(params double[] arguments)
            {
                return arguments[0] + arguments[1];
            }

            public static double Subtract(params double[] arguments)
            {
                return arguments[0] - arguments[1];
            }

            public static double Divide(params double[] arguments)
            {
                if (arguments[1] == 0)
                {
                    return double.NaN;
                }
                return arguments[0] / arguments[1];
            }

            public static double Multiply(params double[] arguments)
            {
                return arguments[0] * arguments[1];
            }

            public static double Power(params double[] arguments)
            {
                return Math.Pow(arguments[0], arguments[1]);
            }

            public static double Mod(params double[] arguments)
            {
                return arguments[0] % arguments[1];
            }

            public static double Factorial(params double[] arguments)
            {
                double max = arguments[0];
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

            public static double GreaterThan(params double[] arguments)
            {
                if (arguments[0] > arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double LessThan(params double[] arguments)
            {
                if (arguments[0] < arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double GreaterThanOrEquals(params double[] arguments)
            {
                if (arguments[0] >= arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double LessThanOrEquals(params double[] arguments)
            {
                if (arguments[0] <= arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double Equals(params double[] arguments)
            {
                if (arguments[0] == arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double NotEquals(params double[] arguments)
            {
                if (arguments[0] != arguments[1])
                {
                    return 1;
                }
                return 0;
            }

            public static double And(params double[] arguments)
            {
                if ( (arguments[0] == 1 && arguments[1] == 1) ||(arguments[0] == 0 && arguments[1] == 0))
                {
                    return 1;
                }
                return 0;
            }

            public static double Or(params double[] arguments)
            {
                if ( arguments[0] == 1 || arguments[1] == 1 )
                {
                    return 1;
                }
                return 0;
            }

            public static double E(params double[] arguments)
            {
                return arguments[0] * Math.Pow(10, arguments[1]);
            }

            public static void Store(ref DataStore dataStore,params string[] arguments)
            {
                dataStore.AddStore(arguments[0], arguments[1]);
            }
        }
    }
}
