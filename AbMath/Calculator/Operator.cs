using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public struct Operator
    {
        public int Weight;
        public int Arguments;
        public Assoc Assoc;
        public RPN.Run Compute;
        public Description Description;

        /// <summary>
        /// For operators without description
        /// </summary>
        /// <param name="assoc"></param>
        /// <param name="weight"></param>
        /// <param name="arguments"></param>
        /// <param name="compute"></param>
        public Operator(Assoc assoc, int weight, int arguments, RPN.Run compute)
        {
            Weight = weight;
            Arguments = arguments;
            Assoc = assoc;
            Compute = compute;
            Description = null;
        }

        /// <summary>
        /// For operators with a description
        /// </summary>
        /// <param name="assoc"></param>
        /// <param name="weight"></param>
        /// <param name="arguments"></param>
        /// <param name="compute"></param>
        /// <param name="description"></param>
        public Operator(Assoc assoc, int weight, int arguments, RPN.Run compute, Description description)
        {
            Weight = weight;
            Arguments = arguments;
            Assoc = assoc;
            Compute = compute;
            Description = description;
        }
    }
}
