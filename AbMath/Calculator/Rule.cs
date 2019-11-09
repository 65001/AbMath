using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    /// <summary>
    /// The rules class has been created to reduce complexity of the code
    /// </summary>
    public class Rule
    {
        public delegate bool isRunnable(RPN.Node node);
        public delegate void Run(RPN.Node node);

        public isRunnable CanRun { get; private set; }
        public Run Compute { get; private set; }

        public string Name { get; private set; }

        public Rule(isRunnable isRunnable, Run run, string name)
        {
            CanRun = isRunnable;
            Compute = run;
            Name = name;
        }

        public bool Execute(RPN.Node node)
        {
            if (!CanRun.Invoke(node))
            {
                return false;
            }

            Compute.Invoke(node);
            return true;
        }
    }
}
