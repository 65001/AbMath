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
        public event EventHandler<string> Logger;

        public delegate bool isRunnable(RPN.Node node);
        public delegate RPN.Node Run(RPN.Node node);

        private isRunnable CanRun;

        private Run Compute;

        public string Name { get; private set; }

        private Rule preRule;
        private Rule postRule;

        private bool debug;

        public Rule(isRunnable isRunnable, Run run, string name, bool debugMode = false)
        {
            CanRun = isRunnable;
            Compute = run;
            Name = name;
            debug = debugMode;
        }

        /// <summary>
        /// Adds a pre processing rule.
        /// The rule is optional and does not have to be run
        /// for the main rule to be run. 
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public Rule AddPreProcessingRule(Rule rule)
        {
            preRule = rule;
            return this;
        }

        /// <summary>
        /// Adds a post processing rule.
        /// The rule is optional and does not have to be run
        /// for the main rule to be run. 
        /// </summary>
        /// <param name="rule"></param>
        /// <returns></returns>
        public Rule AddPostProcessingRule(Rule rule)
        {
            postRule = rule;
            return this;
        }

        public RPN.Node Execute(RPN.Node node)
        {
            String input = node.getRoot().ToInfix();
            RPN.Node assignment = PreOrPostprocess(preRule, node);
            if (assignment != null)
            {
                node = assignment;
            }

            node = Compute.Invoke(node);

            assignment = PreOrPostprocess(postRule, node);
            if (assignment != null)
            {
                node = assignment;
            }
            Write($"\t{Name}");

            return node;

        }

        private RPN.Node PreOrPostprocess(Rule rule, RPN.Node node)
        {
            if (rule != null)
            {
                //if we the pre rule confirms it is applicable run it!
                if (rule.CanExecute(node))
                {
                     Write($"\t{rule.Name}");
                     return rule.Execute(node.Clone());
                }
            }

            return null;
        }

        public bool CanExecute(RPN.Node node)
        {
            if (preRule != null && preRule.CanExecute(node))
            {
                return CanRun.Invoke(preRule.Execute(node));
            }

            return CanRun.Invoke(node);
        }

        public bool DebugMode()
        {
            return debug;
        }

        private void Write(string message)
        {
            Logger?.Invoke(this, message);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
            {
                return false;
            }

            Rule temp = (Rule) obj;
            return temp.Name == this.Name;
        }

    }
}
