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

        private isRunnable CanRun;

        private Run Compute;

        public string Name { get; private set; }

        private Rule preRule;
        private Rule postRule;

        public Rule(isRunnable isRunnable, Run run, string name)
        {
            CanRun = isRunnable;
            Compute = run;
            Name = name;
        }

        public void AddPreProcessingRule(Rule rule)
        {
            preRule = rule;
        }

        public void AddPostProcessingRule(Rule rule)
        {
            postRule = rule;
        }

        public RPN.Node Execute(RPN.Node node)
        {
            RPN.Node assignment = PreOrPostprocess(preRule, node);
            if (assignment != null)
            {
                node = assignment;
            }

            node = Compute.Invoke(node);

            if (node == null)
            {
                return null;
            }

            assignment = PreOrPostprocess(postRule, node);
            if (assignment != null)
            {
                node = assignment;
            }

            return node;

        }

        private RPN.Node PreOrPostprocess(Rule rule, RPN.Node node)
        {
            if (rule != null)
            {
                //if we the pre rule confirms it is applicable run it!
                if (rule.CanExecute(node))
                {
                     return rule.Execute(node);
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

    }
}
