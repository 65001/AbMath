using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    /// <summary>
    /// The rules class has been created to reduce complexity of the code
    /// by allowing us to add rules for optimizations
    /// and derivatives.
    /// </summary>
    public class Rule
    {
        public delegate bool isRunnable(RPN.Node node);
        public delegate RPN.Node Run(RPN.Node node);

        public isRunnable CanRun { get; private set; }

        private Run Compute;

        public string Name { get; private set; }

        public Rule(isRunnable isRunnable, Run run, string name)
        {
            CanRun = isRunnable;
            Compute = run;
            Name = name;
        }

        public RPN.Node Execute(RPN.Node node)
        {
            //Thoughts: Maybe we could have a pre run and post run rules for this kind of thing and let 
            //execute do that ? 
            return Compute.Invoke(node);
        }

    }
}
