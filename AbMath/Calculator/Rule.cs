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

        public EventHandler<string> output { get; private set; }

        public isRunnable CanRun { get; private set; }
        public Run Compute { get; private set; }

        public string Name { get; private set; }

        public Rule(isRunnable isRunnable, Run run, string name, EventHandler<string> std)
        {
            CanRun = isRunnable;
            Compute = run;
            Name = name;
            output = std;
        }

        public bool Execute(RPN.Node node)
        {
            output?.Invoke(this, Name);
            Compute.Invoke(node);
            return true;
        }

    }
}
