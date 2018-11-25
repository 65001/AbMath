using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public partial class RPN
    {
        public static class DoFunctions
        {
            public static double Sin(params double[] Arguments)
            {
                // -1 <= sin(x) <= 1
                return Math.Sin(Arguments[0]);
            }

            public static double Cos(params double[] Arguments)
            {
                return Math.Cos(Arguments[0]);
            }

            public static double Tan(params double[] Arguments)
            {
                return Math.Tan(Arguments[0]);
            }

            public static double Sqrt(params double[] Arguments)
            {
                return Math.Sqrt(Arguments[0]);
            }

            public static double Round(params double[] Arguments)
            {
                double digits;
                if (Arguments.Length == 2) { digits = Arguments[1]; }
                else { digits = 0; }
                return Math.Round(Arguments[0] * Math.Pow(10, digits)) / Math.Pow(10, digits);
            }

            //Two Arguments
            public static double Max(params double[] Arguments)
            {
                double max = 0;
                for (int i = 0; i < Arguments.Length; i++)
                {
                    if (Arguments[i] > max)
                    {
                        max = Arguments[i];
                    }
                }

                return max;
            }

            public static double Min(params double[] Arguments)
            {
                if (Arguments[0] < Arguments[1])
                {
                    return Arguments[0];
                }
                return Arguments[1];
            }

            public static double Bounded(params double[] Arguments)
            {
                //0 - Test, 1 - Floor , 2 - Ceiling 
                if (Arguments[1] <= Arguments[0] && Arguments[0] <= Arguments[2])
                {
                    return Arguments[0];
                }

                if (Arguments[1] >= Arguments[0])
                {
                    return Arguments[1];
                }

                if (Arguments[2] <= Arguments[0])
                {
                    return Arguments[2];
                }

                return Arguments[0];
            }

            public static double Lcm(params double[] Arguments)
            {
                return (Arguments[0] * Arguments[1]) / Gcd(Arguments);
            }

            public static double Gcd(params double[] Arguments)
            {
                Arguments[0] = Math.Abs(Arguments[0]);
                Arguments[1] = Math.Abs(Arguments[1]);
                return (Arguments[1] == 0) ? Arguments[0] : Gcd(Arguments[1], Arguments[0] % Arguments[1]);
            }

            public static double ln(params double[] Arguments)
            {
                return Math.Log(Arguments[0]);
            }

            public static double Log (params double[] Arguments)
            {
                if (Arguments.Length == 1)
                {
                    return Math.Log(Arguments[0]);
                }
                return Math.Log(Arguments[0], Arguments[1]);
            }

            public static double Sum(params double[] Arguments)
            {
                double sum = 0;
                int lowerBound = (int)Arguments[1];
                int upperBound = (int)Arguments[2];
                for (int i = lowerBound; i < upperBound; i++)
                {
                    sum += Arguments[0];
                }
                return sum;
            }

            //Constants 
            public static double Pi(params double[] Arguments)
            {
                return Math.PI;
            }

            public static double EContstant(params double[] Arguments)
            {
                return Math.E;
            }

            
        }
    }
}
