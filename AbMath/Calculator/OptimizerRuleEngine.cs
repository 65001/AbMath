using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AbMath.Utilities;

namespace AbMath.Calculator
{
    /// <summary>
    /// The Rule Engine that powers all of the Abstract Syntax Tree (AST) simplifications.
    /// It allows us to quickly manage large number of rules.
    /// </summary>
    public class OptimizerRuleEngine
    {
        public static Dictionary<AST.SimplificationMode, List<Rule>> ruleSet { get; private set; }
        public static Dictionary<AST.SimplificationMode, Rule> setRule { get; private set; }

        private Dictionary<AST.SimplificationMode, Stopwatch> ruleSetTracker;
        private Dictionary<AST.SimplificationMode, Stopwatch> canExecuteTracker;
        private Dictionary<AST.SimplificationMode, int> hits;

        public bool debug;

        private Logger logger;
        private static HashSet<Rule> contains;

        public OptimizerRuleEngine(Logger logger, bool debugMode = false)
        {
            if (ruleSet == null)
            {
                ruleSet = new Dictionary<AST.SimplificationMode, List<Rule>>();
            }

            if (contains == null)
            {
                contains = new HashSet<Rule>();
            }

            if (setRule == null)
            {
                setRule = new Dictionary<AST.SimplificationMode, Rule>();
            }

            hits = new Dictionary<AST.SimplificationMode, int>();
            debug = debugMode;
            this.logger = logger;

            if (debug)
            {
                ruleSetTracker = new Dictionary<AST.SimplificationMode, Stopwatch>();
                canExecuteTracker = new Dictionary<AST.SimplificationMode, Stopwatch>();
            }
        }

        public void Add(AST.SimplificationMode mode, Rule rule)
        {
            //If the rule already exists we should not
            //add it!
            if (contains.Contains(rule))
            {
                return;
            }

            contains.Add(rule);
            rule.Logger += Write;

            //When the key already exists we just add onto the existing list 
            if (ruleSet.ContainsKey(mode))
            {
                List<Rule> rules = ruleSet[mode];
                rules.Add(rule);
                ruleSet[mode] = rules;
                return;
            }
            //When the key does not exist we should create a list and add the rule onto it
            ruleSet.Add(mode, new List<Rule> { rule });
            hits.Add(mode, 0);

            if (debug)
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                ruleSetTracker.Add(mode, sw);

                Stopwatch sw2 = new Stopwatch();
                sw2.Reset();
                canExecuteTracker.Add(mode, sw2);
            }
        }

        public void AddSetRule(AST.SimplificationMode mode, Rule rule)
        {
            if (!this.HasSetRule(mode))
            {
                setRule.Add(mode, rule);
            }
        }

        public List<Rule> Get(AST.SimplificationMode mode)
        {
            return ruleSet[mode];
        }

        public Rule GetSetRule(AST.SimplificationMode mode)
        {
            return setRule[mode];
        }

        /// <summary>
        /// This executes any possible simplification in the appropriate
        /// set.
        /// </summary>
        /// <param name="mode">The set to look in</param>
        /// <param name="node">The node to apply over</param>
        /// <returns>A new node that is the result of the application of the rule or null
        /// when no rule could be run</returns>
        public RPN.Node Execute(AST.SimplificationMode mode, RPN.Node node)
        {
            if (!ruleSet.ContainsKey(mode))
            {
                throw new KeyNotFoundException("The optimization set was not found");
            }

            List<Rule> rules = ruleSet[mode];
            Stopwatch sw = null;

            if (debug)
            {
                if (ruleSetTracker == null)
                {
                    ruleSetTracker = new Dictionary<AST.SimplificationMode, Stopwatch>();
                }

                if (ruleSetTracker.ContainsKey(mode))
                {
                    sw = ruleSetTracker[mode];
                }
                else
                {
                    sw = new Stopwatch();
                    ruleSetTracker.Add(mode, sw);
                }

                sw.Start();
            }

            if (!hits.ContainsKey(mode))
            {
                hits.Add(mode, 0);
            }

            for (int i = 0; i < rules.Count; i++)
            {
                Rule rule = rules[i];
                if (rule.CanExecute(node))
                {
                    RPN.Node temp = rule.Execute(node);
                    if (debug)
                    {
                        sw.Stop();
                    }

                    if (rule.DebugMode())
                    {
                        Write("The output of : " + temp.ToInfix());
                    }

                    hits[mode] = hits[mode] + 1;
                    return temp;
                }
            }

            if (debug)
            {
                sw.Stop();
            }

            return null;
        }

        public bool ContainsSet(AST.SimplificationMode mode)
        {
            return ruleSet.ContainsKey(mode);
        }

        public bool HasSetRule(AST.SimplificationMode mode)
        {
            return setRule.ContainsKey(mode);
        }

        public bool CanRunSet(AST.SimplificationMode mode, RPN.Node node)
        {
            if (debug)
            {
                if (canExecuteTracker == null)
                {
                    canExecuteTracker = new Dictionary<AST.SimplificationMode, Stopwatch>();
                }

                if (!canExecuteTracker.ContainsKey(mode))
                {
                    Stopwatch temp = new Stopwatch();
                    temp.Reset();
                    canExecuteTracker.Add(mode, temp);
                }
            }

            bool result = false;
            Stopwatch sw = null;
            if (debug)
            {
                sw = canExecuteTracker[mode];
                sw.Start();
            }

            result = ContainsSet(mode) && (!HasSetRule(mode) || GetSetRule(mode).CanExecute(node));
            if (debug)
            {
                sw.Stop();
            }
            return result;
        }

        private void Write(object obj, string message)
        {
            Write(message);
        }

        private void Write(string message)
        {
            logger.Log(Channels.Debug, message);
        }

        public override string ToString()
        {
            return this.ToString(null);
        }

        public string ToString(string format)
        {
            Config config = new Config() { Format = Format.Default, Title = "Simplification Rule Set Overview" };
            if (format == "%M")
            {
                config.Format = Format.MarkDown;
            }

            Tables<string> ruleTables = new Tables<string>(config);
            ruleTables.Add(new Schema("Name"));
            ruleTables.Add(new Schema("Count"));
            ruleTables.Add(new Schema("Set Rule"));
            ruleTables.Add(new Schema("Execution Time (ms | Ticks)"));
            ruleTables.Add(new Schema("Check Time (ms | Ticks)"));
            ruleTables.Add(new Schema("Hits"));

            int totalRules = 0;
            long totalExecutionElapsedMilliseconds = 0;
            long totalExecutionElappsedTicks = 0;
            long totalCheckElapsedMilliseconds = 0;
            long totalCheckElapsedTicks = 0;
            int totalHits = 0;
            foreach (var KV in ruleSet)
            {
                string executionTime = "-";
                string checkTime = "-";
                string hit = "-";

                if (debug && ruleSetTracker != null && ruleSetTracker.ContainsKey(KV.Key))
                {
                    executionTime = ruleSetTracker[KV.Key].ElapsedMilliseconds.ToString() + " | " + ruleSetTracker[KV.Key].ElapsedTicks.ToString("N0");
                    checkTime = canExecuteTracker[KV.Key].ElapsedMilliseconds.ToString() + " | " + canExecuteTracker[KV.Key].ElapsedTicks.ToString("N0");

                    totalExecutionElapsedMilliseconds += ruleSetTracker[KV.Key].ElapsedMilliseconds;
                    totalExecutionElappsedTicks += ruleSetTracker[KV.Key].ElapsedTicks;

                    totalCheckElapsedMilliseconds += canExecuteTracker[KV.Key].ElapsedMilliseconds;
                    totalCheckElapsedTicks += canExecuteTracker[KV.Key].ElapsedTicks;
                }

                if (hits.ContainsKey(KV.Key))
                {
                    hit = hits[KV.Key].ToString();
                    totalHits += hits[KV.Key];
                }

                totalRules += KV.Value.Count;

                string[] row = new string[] { KV.Key.ToString(), KV.Value.Count.ToString(), setRule.ContainsKey(KV.Key) ? "✓" : "X", executionTime, checkTime, hit };
                ruleTables.Add(row);
            }
            string[] total = new string[] { "Total", totalRules.ToString(), "", $"{totalExecutionElapsedMilliseconds} | {totalExecutionElappsedTicks}", $"{totalCheckElapsedMilliseconds} | {totalCheckElapsedTicks}", totalHits.ToString() };
            ruleTables.Add(total);

            return ruleTables.ToString();
        }
    }
}
