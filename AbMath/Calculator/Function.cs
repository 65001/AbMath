using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    /// <summary>
    /// A list of related information of functions.
    /// </summary>
    public struct Function
    {
        public int Arguments;
        public int MaxArguments;
        public int MinArguments;

        public RPN.Run Compute;
        public Description Description;

        /// <summary>
        /// For meta functions
        /// </summary>
        /// <param name="Min"></param>
        /// <param name="Args"></param>
        /// <param name="Max"></param>
        /// <param name="description"></param>
        public Function(int Min, int Args, int Max, Description description)
        {
            MinArguments = Min;
            Arguments = Args;
            MaxArguments = Max;
            Compute = null;
            Description = description;
        }

        /// <summary>
        /// For meta functions
        /// </summary>
        /// <param name="Min"></param>
        /// <param name="Args"></param>
        /// <param name="Max"></param>
        /// <param name="description"></param>
        public Function(int Min, int Args, int Max)
        {
            MinArguments = Min;
            Arguments = Args;
            MaxArguments = Max;
            Compute = null;
            Description = null;
        }

        /// <summary>
        /// For functions without descriptions
        /// </summary>
        /// <param name="min"></param>
        /// <param name="args"></param>
        /// <param name="max"></param>
        /// <param name="compute"></param>
        public Function(int min, int args, int max, RPN.Run compute)
        {
            MinArguments = min;
            Arguments = args;
            MaxArguments = max;
            Compute = compute;
            Description = null;
        }

        /// <summary>
        /// For functions with descriptions
        /// </summary>
        /// <param name="min"></param>
        /// <param name="args"></param>
        /// <param name="max"></param>
        /// <param name="compute"></param>
        /// <param name="description"></param>
        public Function(int min, int args, int max, RPN.Run compute, Description description)
        {
            MinArguments = min;
            Arguments = args;
            MaxArguments = max;
            Compute = compute;
            Description = description;
        }
    }
}
