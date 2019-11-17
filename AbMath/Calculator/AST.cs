using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AbMath.Calculator.Simplifications;

namespace AbMath.Calculator
{
    public class AST
    {
        public RPN.Node Root { get; private set; }

        /// <summary>
        ///  Sqrt -
        ///  Log -
        ///  Imaginary -
        ///  Division -
        ///  Exponent -
        ///  Subtraction -
        ///  Addition -
        ///  Multiplication -
        ///  Swap - 
        ///  Trig - 
        ///  Trig Half Angle - Converts fractions to trig functions when appropriate
        ///  Trig Half Angle Expansion - Converts trig functions to fractions
        ///  Power Reduction -  Converts trig functions to fractions
        ///  Power Expansion - Converts fractions to trig functions
        ///  Double Angle - Converts double angles to trig functions
        ///  Double Angle Reduction - Converts trig functions to double angles
        ///  Constants - 
        /// </summary>
        public enum SimplificationMode
        {
            Sqrt, Log, Imaginary, Division, Exponent, Subtraction, Addition, Multiplication, Swap,
            Trig, TrigHalfAngle, TrigHalfAngleExpansion,
            TrigPowerReduction, TrigPowerExpansion,
            TrigDoubleAngle, TrigDoubleAngleReduction,
            Constants, Compress, COUNT
        }

        private RPN _rpn;
        private RPN.DataStore _data;

        private bool debug => _rpn.Data.DebugMode;

        private readonly RPN.Token _derive = new RPN.Token("derive", 1, RPN.Type.Function);

        public event EventHandler<string> Logger;
        public event EventHandler<string> Output;

        private OptimizerRuleSet ruleManager;

        public AST(RPN rpn)
        {
            _rpn = rpn;
            _data = rpn.Data;
            RPN.Node.ResetCounter();
        }

        /// <summary>
        /// Generates the rule set for all simplifications
        /// </summary>
        private void GenerateRuleSetSimplifications()
        {
            GenerateSqrtSimplifications();
            GenerateLogSimplifications();
            GenerateSubtractionSimplifications();
        }

        private void GenerateSqrtSimplifications()
        {
            Rule sqrt = new Rule(Sqrt.SqrtToFuncRunnable, Sqrt.SqrtToFunc, "sqrt(g(x))^2 - > g(x)");
            Rule abs = new Rule(Sqrt.SqrtToAbsRunnable, Sqrt.SqrtToAbs, "sqrt(g(x)^2) -> abs(g(x))");
            Rule sqrtPower = new Rule(Sqrt.SqrtPowerFourRunnable, Sqrt.SqrtPowerFour, "sqrt(g(x)^n) where n is a multiple of 4. -> g(x)^n/2");
            ruleManager.Add(SimplificationMode.Sqrt, sqrt);
            ruleManager.Add(SimplificationMode.Sqrt, abs);
            ruleManager.Add(SimplificationMode.Sqrt, sqrtPower);
        }

        private void GenerateLogSimplifications()
        {
            Rule logToLn = new Rule(Log.LogToLnRunnable, Log.LogToLn, "log(e,f(x)) - > ln(f(x))");
            
            //This rule only can be a preprocessor rule and therefore should not be added to the rule manager!
            Rule LnToLog = new Rule(Log.LnToLogRunnable, Log.LnToLog, "ln(f(x)) -> log(e,f(x))");

            //These are candidates for preprocessing and post processing:
            Rule logOne = new Rule(Log.LogOneRunnable,Log.LogOne,"log(b,1) -> 0");
            Rule logIdentical = new Rule(Log.LogIdentitcalRunnable, Log.LogIdentitcal, "log(b,b) -> 1");
            Rule logPowerExpansion = new Rule(Log.LogExponentExpansionRunnable, Log.LogExponentExpansion, "log(b,R^c) -> c * log(b,R)");

            logOne.AddPreProcessingRule(LnToLog).AddPostProcessingRule(logToLn);
            logIdentical.AddPreProcessingRule(LnToLog).AddPostProcessingRule(logToLn);
            logPowerExpansion.AddPreProcessingRule(LnToLog).AddPostProcessingRule(logToLn);

            Rule logPower = new Rule(Log.LogPowerRunnable, Log.LogPower, "b^log(b,x) -> x");
            Rule logSummation = new Rule(Log.LogSummationRunnable, Log.LogSummation, "log(b,R) + log(b,S) -> log(b,R*S)");
            Rule logSubtraction = new Rule(Log.LogSubtractionRunnable, Log.LogSubtraction, "log(b,R) - log(b,S) -> log(b,R/S)");

            //TODO: lnPower e^ln(x) -> x
            //TODO: log(b,R^c)
            //TODO: e^ln(x) -> x
            //TODO: ln(e) -> 1
            //TODO: ln(R^c) -> log(e,R^c) -> c * ln(R)

            Rule lnSummation = new Rule(Log.LnSummationRunnable, Log.LnSummation, "ln(R) + ln(S) -> log(e,R) + log(e,S) -> ln(R*S)");
            Rule lnSubtraction = new Rule(Log.LnSubtractionRunnable, Log.LnSubtraction, "ln(R) - ln(S) -> log(e,R) - log(e,S) -> ln(R/S)");
            Rule lnPowerExpansion = new Rule(Log.LnPowerRuleRunnable, Log.LnPowerRule, "ln(R^c) -> c*ln(R)");

            ruleManager.Add(SimplificationMode.Log, logOne);
            ruleManager.Add(SimplificationMode.Log, logIdentical);
            ruleManager.Add(SimplificationMode.Log, logPower);
            ruleManager.Add(SimplificationMode.Log, logPowerExpansion);

            ruleManager.Add(SimplificationMode.Log, logSummation);
            ruleManager.Add(SimplificationMode.Log, logSubtraction);
            ruleManager.Add(SimplificationMode.Log, lnSummation);
            ruleManager.Add(SimplificationMode.Log, lnSubtraction);

            ruleManager.Add(SimplificationMode.Log, lnPowerExpansion);

            ruleManager.Add(SimplificationMode.Log, logToLn);
        }

        private void GenerateSubtractionSimplifications()
        {
            Rule setRule = new Rule(Subtraction.setRule, null, "Subtraction Set Rule");
            ruleManager.AddSetRule(SimplificationMode.Subtraction, setRule);

            Rule sameFunction = new Rule(Subtraction.SameFunctionRunnable, Subtraction.SameFunction, "f(x) - f(x) -> 0");
            Rule coefficientOneReduction = new Rule(Subtraction.CoefficientOneReductionRunnable, Subtraction.CoefficientOneReduction, "cf(x) - f(x) -> (c - 1)f(x)");
            Rule subtractionByZero = new Rule(Subtraction.SubtractionByZeroRunnable, Subtraction.SubtractionByZero, "f(x) - 0 -> f(x)");

            Rule subtractionDivisionCommmonDenominator = new Rule(Subtraction.SubtractionDivisionCommonDenominatorRunnable,
                Subtraction.SubtractionDivisionCommonDenominator, "f(x)/g(x) - h(x)/g(x) -> [f(x) - h(x)]/g(x)");

            ruleManager.Add(SimplificationMode.Subtraction, sameFunction);
            ruleManager.Add(SimplificationMode.Subtraction, coefficientOneReduction);
            ruleManager.Add(SimplificationMode.Subtraction, subtractionByZero);

            ruleManager.Add(SimplificationMode.Subtraction, subtractionDivisionCommmonDenominator);
        }

        public RPN.Node Generate(RPN.Token[] input)
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();
            RPN.Node node = RPN.Node.Generate(input);

            //This prevents the reassignment of the root node
            if (Root is null)
            {
                Root = node;
            }

            SW.Stop();
            _rpn.Data.AddTimeRecord("AST Generate", SW);
            return node;
        }

        /// <summary>
        /// Simplifies the current tree.
        /// </summary>
        /// <returns></returns>
        public AST Simplify()
        {
            Normalize();

            Stopwatch sw = new Stopwatch();

            ruleManager = new OptimizerRuleSet();
            ruleManager.Logger += Logger;
            //Let us generate the rules here if not already creates 
            GenerateRuleSetSimplifications();

            sw.Start();
            int pass = 0;
            string hash = string.Empty;
            Dictionary<string, OptimizationTracker> tracker = new Dictionary<string, OptimizationTracker>();

            while (hash != Root.GetHash())
            {
                Stopwatch sw1 = new Stopwatch();
                sw1.Start();
                hash = Root.GetHash();
                if (tracker.ContainsKey(hash))
                {
                    if (tracker[hash].count > 10)
                    {
                        stdout("An infinite optimization loop may be occuring. Terminating optimization.");
                        return this;
                    }

                    var foo = tracker[hash];
                    foo.count++;
                    tracker[hash] = foo;
                }
                else
                {
                    tracker.Add(hash, new OptimizationTracker() { count = 1, Hash = hash });
                }

                _data.AddTimeRecord("AST.GetHash", sw1);

                if (debug)
                {
                    Write($"{pass}. {Root.ToInfix()}.");
                }

                Simplify(Root);
                pass++;
            }
            sw.Stop();
            _data.AddTimeRecord("AST Simplify", sw);

            Normalize();
            return this;
        }

        private void Normalize()
        {
            //This should in theory normalize the tree
            //so that exponents etc come first etc
            Stopwatch sw = new Stopwatch();
            sw.Start();

            _rpn.Data.AddFunction("internal_product", new RPN.Function());
            _rpn.Data.AddFunction("internal_sum", new RPN.Function());

            expand(Root);
            InternalSwap(Root);
            compress(Root);

            _rpn.Data.RemoveFunction("internal_product");
            _rpn.Data.RemoveFunction("internal_sum");
            sw.Stop();
            this._rpn.Data.AddTimeRecord("AST.Normalize :: AST Simplify", sw);
        }

        private void Simplify(RPN.Node node)
        {
#if DEBUG
                Write(Root.ToInfix());       
#endif

            Simplify(node, SimplificationMode.Sqrt);
            Simplify(node, SimplificationMode.Log);
            Simplify(node, SimplificationMode.Imaginary);
            Simplify(node, SimplificationMode.Division);

            Simplify(node, SimplificationMode.Exponent); //This will make all negative exponennts into divisions
            Simplify(node, SimplificationMode.Subtraction);
            Simplify(node, SimplificationMode.Addition);
            Simplify(node, SimplificationMode.Trig);
            Simplify(node, SimplificationMode.Multiplication);
            Simplify(node, SimplificationMode.Swap);

            Swap(node);
#if DEBUG
                Write(Root.ToInfix());
#endif
        }

        private void Simplify(RPN.Node node, SimplificationMode mode)
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();

            Stack<RPN.Node> stack = new Stack<RPN.Node>();
            stack.Push(node);
            while (stack.Count > 0)
            {
                node = stack.Pop();
                //Write(node.GetHash());

                //If Root is a number abort. 
                if (Root.IsNumber())
                {
                    return;
                }

                if (node.IsNumber() || node.IsConstant())
                {
                    continue;
                }

                //This is the rule manager execution code
                if (ruleManager.ContainsSet(mode))
                {
                    bool canRunTestSuite = (!ruleManager.HasSetRule(mode) || ruleManager.GetSetRule(mode).CanExecute(node));

                    if (canRunTestSuite)
                    {
                        RPN.Node assignment = ruleManager.Execute(mode, node);
                        if (assignment != null)
                        {
                            Assign(node, assignment);
                        }
                    }
                }

                if (mode == SimplificationMode.Imaginary && node.IsSqrt())
                {
                    //Any sqrt function with a negative number -> Imaginary number to the root node
                    //An imaginary number propagates anyways
                    if (node.Children[0].IsLessThanNumber(0))
                    {
                        SetRoot(new RPN.Node(double.NaN));
                        Write($"\tSqrt Imaginary Number -> Root.");
                    }

                    //MAYBE: Any sqrt function with any non-positive number -> Cannot simplify further??
                }
                else if (mode == SimplificationMode.Division && node.IsDivision())
                {
                    //if there are any divide by zero exceptions -> NaN to the root node
                    //NaN propagate anyways
                    if (node.Children[0].IsNumber(0))
                    {
                        SetRoot(new RPN.Node(double.NaN));
                        Write("\tDivision by zero -> Root");
                    }
                    else if (node.Children[0].IsNumber(1))
                    {
                        Write("\tDivision by one");
                        Assign(node, node.Children[1]);
                    }
                    //gcd if the leafs are both numbers since the values of the leafs themselves are changed
                    //we don't have to worry about if the node is the root or not
                    else if (node.Children[0].IsInteger() && node.Children[1].IsInteger())
                    {
                        double num1 = node.Children[0].GetNumber();
                        double num2 = node.Children[1].GetNumber();
                        double gcd = RPN.DoFunctions.Gcd(new double[] { num1, num2 });

                        node.Replace(node.Children[0], new RPN.Node((num1 / gcd)));
                        node.Replace(node.Children[1], new RPN.Node((num2 / gcd)));
                        Write("\tDivision GCD.");
                    }
                    else if (node.Children[0].IsDivision() && node.Children[1].IsDivision())
                    {
                        Write("\tDivison Flip");
                        RPN.Node[] numerator = { node.Children[0].Children[1], node.Children[1].Children[1] };
                        RPN.Node[] denominator = { node.Children[0].Children[0], node.Children[1].Children[0] };

                        RPN.Node top = new RPN.Node(new[] { denominator[0], numerator[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node bottom = new RPN.Node(new[] { denominator[1], numerator[0] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node division = new RPN.Node(new[] { bottom, top }, new RPN.Token("/", 2, RPN.Type.Operator));
                        Assign(node, division);
                    }
                    else if (node[1].IsMultiplication() && node[0].IsNumberOrConstant() &&
                             node[0].Matches(node[1, 1]) && !node[1, 1].IsNumber(0))
                    {
                        Write("\t(c * f(x))/c -> f(x) where c is not 0");
                        Assign(node, node[1, 0]);
                    }
                    else if (node[0].IsExponent() && node[1].IsExponent() && node[0, 0].IsInteger() &&
                             node[1, 0].IsInteger() && node[0, 1].Matches(node[1, 1]))
                    {
                        int reduction = System.Math.Min((int)node[0, 0].GetNumber(), (int)node[1, 0].GetNumber()) - 1;
                        node[0, 0].Replace(node[0, 0].GetNumber() - reduction);
                        node[1, 0].Replace(node[1, 0].GetNumber() - reduction);
                        Write("\tPower Reduction");
                    }
                    else if (node[1].IsDivision())
                    {
                        Write("\t[f(x)/g(x)]/ h(x) -> [f(x)/g(x)]/[h(x)/1] - > f(x)/[g(x) * h(x)]");
                        RPN.Node numerator = node[1, 1];
                        RPN.Node denominator = new RPN.Node(new[] { node[0], node[1, 0] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node division = new RPN.Node(new[] { denominator, numerator },
                            new RPN.Token("/", 2, RPN.Type.Operator));
                        Assign(node, division);
                    }

                    //TODO: (c_0 * f(x))/c_1 where c_0, c_1 share a gcd that is not 1 and c_0 and c_1 are integers 
                    //TODO: (c_0 * f(x))/(c_1 * g(x)) where ...
                }
                else if (mode == SimplificationMode.Subtraction && node.IsSubtraction())
                {

                    //0 - 3sin(x)
                    if (!(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[1].IsNumber(0))
                    {
                        RPN.Node multiply = new RPN.Node(new[] { new RPN.Node(-1), node.Children[0] },
                            new RPN.Token("*", 2, RPN.Type.Operator));

                        Write($"\t0 - f(x) -> -f(x)");
                        Assign(node, multiply);
                    }
                    else if (!(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[0].IsMultiplication() && node[0, 1].IsLessThanNumber(0))
                    {
                        //(cos(x)^2)-(-1*(sin(x)^2)) 
                        //(cos(x)^2)-(-2*(sin(x)^2)) 
                        //((-2*(cos(x)^2))+(2*(sin(x)^2)))
                        Write("\tf(x) - (-c * g(x)) -> f(x) + c *g(x)");
                        node[0, 1].Replace(  node[0,1].GetNumber() * -1);
                        node.Replace(new RPN.Token("+", 2, RPN.Type.Operator));
                    }
                    else if (!(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[0].IsNumber() && node[0].IsLessThanNumber(0))
                    {
                        Write("\tf(x) - (-c) -> f(x) + c");
                        RPN.Node multiplication = new RPN.Node(new[] { Clone(node[0]), new RPN.Node(-1) }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node addition = new RPN.Node(new[] { multiplication, node[1] }, new RPN.Token("+", 2, RPN.Type.Operator));
                        Assign(node, addition);
                        Simplify(multiplication, SimplificationMode.Multiplication);
                    }
                    //3sin(x) - 2sin(x)
                    else if (node[0].IsMultiplication() && node[1].IsMultiplication())
                    {
                        if (node.Children[0].Children[1].IsNumber() && node.Children[1].Children[1].IsNumber() &&
                            node.Children[0].Children[0].Matches(node.Children[1].Children[0]))
                        {
                            Write("\tCf(x) - cf(x) -> (C - c)f(x)");
                            double coefficient = node.Children[1].Children[1].GetNumber() -
                                                 node.Children[0].Children[1].GetNumber();
                            node.Children[0].Replace(node.Children[0].Children[1], new RPN.Node(0));
                            node.Children[1].Replace(node.Children[1].Children[1], new RPN.Node(coefficient));
                        }
                    }

                    //TODO: f(x)/g(x) - i(x)/j(x) -> [f(x)j(x)]/g(x)j(x) - i(x)g(x)/g(x)j(x) -> [f(x)j(x) - g(x)i(x)]/[g(x)j(x)]
                }
                else if (mode == SimplificationMode.Addition && node.IsAddition())
                {
                    //Is root and leafs have the same hash
                    if (node.ChildrenAreIdentical())
                    {
                        RPN.Node multiply = new RPN.Node(new[] { node.Children[0], new RPN.Node(2) },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiply);
                        Write("\tSimplification: Addition -> Multiplication");
                    }
                    //Zero addition
                    else if (!(node[0].IsMultiplication() && node[1].IsMultiplication()) && node.Children[0].IsNumber(0))
                    {
                        Write("\tZero Addition.");
                        Assign(node, node.Children[1]);
                    }
                    //Case: 0 + sin(x)
                    else if (!(node[0].IsMultiplication() && node[1].IsMultiplication()) && node.Children[1].IsNumber(0))
                    {
                        //Child 1 is the expression in this case.
                        Write("\tZero Addition. Case 2.");
                        Assign(node, node.Children[0]);
                    }
                    //7sin(x) + sin(x)
                    //C0: Anything
                    //C1:C0: Compare hash to C0.
                    else if (!(node[0].IsMultiplication() && node[1].IsMultiplication()) && node.Children[1].IsMultiplication() && node.Children[1].Children[1].IsNumber() &&
                             node.Children[1].Children[0].Matches(node.Children[0]))
                    {
                        Write("\tSimplification Addition Dual Node.");
                        node.Children[0].Remove(new RPN.Node(0));
                        node.Children[1].Replace(node.Children[1].Children[1],
                            new RPN.Node(node.Children[1].Children[1].GetNumber() + 1));
                    }
                    else if (!(node[0].IsMultiplication() && node[1].IsMultiplication()) && node.Children[0].IsMultiplication() && node.Children[0].Children[1].IsLessThanNumber(0))
                    {
                        Write("\tAddition can be converted to subtraction");
                        node.Replace("-");
                        node.Children[0].Replace(node.Children[0].Children[1], new RPN.Node(1));
                    }
                    else if (!(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[0].IsLessThanNumber(0) && node[1].IsMultiplication())
                    {
                        Write("\tAddition can be converted to subtraction");
                        node.Replace("-");
                        node.Replace(node[0], new RPN.Node(System.Math.Abs(node[0].GetNumber())));
                    }
                    else if (!(node[0].IsMultiplication() && node[1].IsMultiplication()) && node.Children[0].IsSubtraction() && node[1].Matches(node[0, 1]))
                    {
                        Write("\tf(x) + f(x) - g(x) -> 2 * f(x) - g(x)");

                        node[0].Replace(node[0, 1], new RPN.Node(0));
                        RPN.Node multiplication = new RPN.Node(new[] { node[1], new RPN.Node(2) },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        node.Replace(node[1], multiplication);
                    }
                    else if (!(node[0].IsMultiplication() && node[1].IsMultiplication()) && node[1].IsMultiplication() && node[1, 1].IsNumber(-1))
                    {
                        Write("\t-f(x) + g(x) -> g(x) - f(x)");
                        node[1, 1].Replace(1);
                        node.Swap(0, 1);
                        node.Replace(new RPN.Token("-", 2, RPN.Type.Operator));
                    }
                    //Both nodes are multiplications with 
                    //the parent node being addition
                    //Case: 2sin(x) + 3sin(x)
                    else if (node.Children[0].IsMultiplication() && node.Children[1].IsMultiplication())
                    {
                        if (node.Children[0].Children[1].IsNumber() && node.Children[1].Children[1].IsNumber() &&
                            node.Children[0].Children[0].Matches(node.Children[1].Children[0]))
                        {
                            Write("\tSimplification: Addition");
                            double sum = (node.Children[0].Children[1].GetNumber() +
                                          node.Children[1].Children[1].GetNumber());
                            node.Children[1].Replace(node.Children[1].Children[1], new RPN.Node(sum));
                            node.Children[0].Replace(node.Children[0].Children[1], new RPN.Node(0));
                        }
                    }

                    //TODO: -c * f(x) + g(x) -> g(x) - c * f(x)

                    //TODO f(x)/g(x) + h(x)/g(x) -> [f(x) + h(x)]/g(x)
                    //TODO: f(x)/g(x) + i(x)/j(x) -> [f(x)j(x)]/g(x)j(x) + i(x)g(x)/g(x)j(x) -> [f(x)j(x) + g(x)i(x)]/[g(x)j(x)]
                }
                else if (mode == SimplificationMode.Trig)
                {

                    if (node.IsAddition() &&
                        node.Children[0].IsExponent() &&
                        node.Children[1].IsExponent() &&
                        node.Children[0].Children[0].IsNumber(2) &&
                        node.Children[1].Children[0].IsNumber(2) &&
                        (node.Children[0].Children[1].IsFunction("cos") ||
                         node.Children[0].Children[1].IsFunction("sin")) &&
                        (node.Children[1].Children[1].IsFunction("sin") ||
                         node.Children[1].Children[1].IsFunction("cos")) &&
                        !node.ChildrenAreIdentical() &&
                        !node.containsDomainViolation() &&
                        node.Children[0].Children[1].Children[0].Matches(node.Children[1].Children[1].Children[0])
                    )
                    {
                        RPN.Node head = new RPN.Node(1);
                        Write("\tsin²(x) + cos²(x) -> 1");
                        Assign(node, head);
                    }
                    else if (node.IsDivision() && node.Children[0].IsFunction("sin") &&
                             node.Children[1].IsFunction("cos") &&
                             node.Children[0].Children[0].Matches(node.Children[1].Children[0]))
                    {
                        Write("\tcos(x)/sin(x) -> cot(x)");
                        RPN.Node cot = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("cot", 1, RPN.Type.Function));
                        Assign(node, cot);
                    }
                    else if (node.IsDivision() && node.Children[0].IsFunction("cos") &&
                             node.Children[1].IsFunction("sin") &&
                             node.Children[0].Children[0].Matches(node.Children[1].Children[0]))
                    {
                        Write("\tsin(x)/cos(x) -> tan(x)");
                        RPN.Node tan = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("tan", 1, RPN.Type.Function));
                        Assign(node, tan);
                    }
                    else if (node.IsDivision() && node.Children[1].IsMultiplication() &&
                             node.Children[0].IsFunction("sin") && node.Children[1].Children[0].IsFunction("cos") &&
                             node.Children[0].Children[0].Matches(node.Children[1].Children[0].Children[0]))
                    {
                        Write("\t[f(x) * cos(x)]/sin(x) -> f(x) * cot(x)");
                        RPN.Node cot = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("cot", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { cot, node.Children[1].Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsDivision() && node.Children[0].IsFunction("sec"))
                    {
                        Write("\tf(x)/sec(g(x)) -> f(x)cos(g(x))");
                        RPN.Node cos = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("cos", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { cos, node.Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsDivision() && node.Children[0].IsFunction("csc"))
                    {
                        Write("\tf(x)/csc(g(x)) -> f(x)sin(g(x))");
                        RPN.Node sin = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("sin", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { sin, node.Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsDivision() && node.Children[0].IsFunction("cot"))
                    {
                        Write("\tf(x)/cot(g(x)) -> f(x)tan(g(x))");
                        RPN.Node tan = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("tan", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { tan, node.Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsDivision() && node.Children[0].IsFunction("cos"))
                    {
                        Write("\tf(x)/cos(g(x)) -> f(x)sec(g(x))");
                        RPN.Node sec = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("sec", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { sec, node.Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsDivision() && node.Children[0].IsFunction("sin"))
                    {
                        Write("\tf(x)/sin(g(x)) -> f(x)csc(g(x))");
                        RPN.Node csc = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("csc", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { csc, node.Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsDivision() && node.Children[0].IsFunction("tan"))
                    {
                        Write("\tf(x)/tan(g(x)) -> f(x)cot(g(x))");
                        RPN.Node cot = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("cot", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { cot, node.Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsFunction("cos") && node.Children[0].IsMultiplication() &&
                             node.Children[0].Children[1].IsNumber(-1))
                    {
                        Write("\tcos(-f(x)) -> cos(f(x))");
                        node.Children[0].Replace(node.Children[0].Children[1], new RPN.Node(1));
                    }
                    else if (node.IsFunction("sec") && node.Children[0].IsMultiplication() &&
                             node.Children[0].Children[1].IsNumber(-1))
                    {
                        Write("\tsec(-f(x)) -> sec(f(x))");
                        node.Children[0].Replace(node.Children[0].Children[1], new RPN.Node(1));
                    }
                    else if (node.IsFunction("sin") && node.Children[0].IsMultiplication() &&
                             node.Children[0].Children[1].IsNumber(-1))
                    {
                        Write("\tsin(-f(x)) -> -1 * sin(f(x))");
                        RPN.Node sin = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("sin", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { sin, node.Children[0].Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsFunction("tan") && node.Children[0].IsMultiplication() &&
                             node.Children[0].Children[1].IsNumber(-1))
                    {
                        Write("\ttan(-f(x)) -> -1 * tan(f(x))");
                        RPN.Node tan = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("tan", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { tan, node.Children[0].Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsFunction("csc") && node.Children[0].IsMultiplication() &&
                             node.Children[0].Children[1].IsNumber(-1))
                    {
                        Write("\tcsc(-f(x)) -> -1 * csc(f(x))");
                        RPN.Node csc = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("csc", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { csc, node.Children[0].Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsFunction("cot") && node.Children[0].IsMultiplication() &&
                             node.Children[0].Children[1].IsNumber(-1))
                    {
                        Write("\tcot(-f(x)) -> -1 * cot(f(x))");
                        RPN.Node cot = new RPN.Node(new[] { node.Children[0].Children[0] },
                            new RPN.Token("cot", 1, RPN.Type.Function));
                        RPN.Node multiplication = new RPN.Node(new[] { cot, node.Children[0].Children[1] },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        Assign(node, multiplication);
                    }
                    else if (node.IsSubtraction() && node[0].IsExponent() && node[1].IsNumber(1) && node[0, 0].IsNumber(2) && node[0, 1].IsFunction("sin"))
                    {
                        Write("\t1 - sin(x)^2 -> cos(x)^2");
                        RPN.Node cos = new RPN.Node(new[] { node[0, 1, 0] }, new RPN.Token("cos", 1, RPN.Type.Function));
                        RPN.Node exponent = new RPN.Node(new[] { node[0, 0], cos }, new RPN.Token("^", 2, RPN.Type.Operator));
                        Assign(node, exponent);
                    }
                    else if (node.IsSubtraction() && node[0].IsExponent() && node[1].IsNumber(1) && node[0, 0].IsNumber(2) && node[0, 1].IsFunction("cos"))
                    {
                        Write("\t1 - cos(x)^2 -> sin(x)^2");
                        RPN.Node sin = new RPN.Node(new[] { node[0, 1, 0] }, new RPN.Token("sin", 1, RPN.Type.Function));
                        RPN.Node exponent = new RPN.Node(new[] { node[0, 0], sin }, new RPN.Token("^", 2, RPN.Type.Operator));
                        Assign(node, exponent);
                    }
                    else if (node.IsDivision() && node[0].IsMultiplication() && node[1].IsFunction("cos") && node[0, 0].IsFunction("sin") && node[0, 0, 0].Matches(node[1, 0]))
                    {
                        Write("\tcos(x)/(f(x) * sin(x)) -> cot(x)/f(x)");
                        //cos(x)/[sin(x) * f(x)] -> cot(x)/f(x) is also implemented due to swapping rules. 

                        RPN.Node cot = new RPN.Node(new[] { node[1, 0] }, new RPN.Token("cot", 1, RPN.Type.Function));
                        RPN.Node division = new RPN.Node(new[] { node[0, 1], cot }, new RPN.Token("/", 2, RPN.Type.Operator));
                        Assign(node, division);
                    }
                    //TODO:
                    //[f(x) * cos(x)]/[g(x) * sin(x)] -> [f(x) * cot(x)]/g(x) 

                    //[f(x) * sin(x)]/cos(x) -> f(x) * tan(x)
                    //sin(x)/[f(x) * cos(x)] -> tan(x)/f(x)
                    //[f(x) * sin(x)]/[g(x) * cos(x)] -> [f(x) * tan(x)]/g(x) 

                    //TODO: [1 + tan(f(x))^2] -> sec(f(x))^2
                    //TODO: [cot(f(x))^2 + 1] -> csc(f(x))^2

                    //These will probably violate domain constraints ?
                    //TODO: sec(x)^2 - tan(x)^2 = 1
                    //TODO: cot(x)^2 + 1 = csc(x)^2 
                    //TODO: csc(x)^2 - cot(x)^2 = 1

                    //TODO: Double Angle
                    //[cos(x)^2 - sin(x)^2] = cos(2x)
                    //1 - 2sin(x)^2 = cos(2x)
                    //2cos(x)^2 - 1 = cos(2x) 
                    //2sin(x)cos(x) = sin(2x)
                    //[2tan(x)]/1 - tan(x)^2] = tan(2x) 

                    //TODO: Power Reducing 
                    //[1 - cos(2x)]/2 = sin(x)^2
                    //[1 + cos(2x)]/2 = cos(x)^2
                    //[1 - cos(2x)]/[1 + cos(2x)] = tan(x)^2 


                }
                else if (mode == SimplificationMode.Multiplication && node.IsMultiplication())
                {
                    //TODO: If one of the leafs is a division and the other a number or variable
                    if (node.ChildrenAreIdentical())
                    {
                        RPN.Node head = new RPN.Node(new[] { new RPN.Node(2), node.Children[0] },
                            new RPN.Token("^", 2, RPN.Type.Operator));
                        Assign(node, head);
                        Write("\tSimplification: Multiplication -> Exponent");
                    }
                    else if (node.Children[0].IsNumber(1) || node.Children[1].IsNumber(1))
                    {
                        RPN.Node temp = node.Children[1].IsNumber(1) ? node.Children[0] : node.Children[1];
                        Assign(node, temp);
                        Write($"\tMultiplication by one simplification.");
                    }
                    //TODO: Replace the requirement that we cannot do a simplification when a division is present to 
                    //that we cannot do a simplification when a division has a variable in the denominator!
                    else if ((node.Children[1].IsNumber(0) || node.Children[0].IsNumber(0)) && !node.containsDomainViolation())
                    {
                        Write($"\tMultiplication by zero simplification.");
                        Assign(node, new RPN.Node(0));
                    }
                    //sin(x)sin(x)sin(x) -> sin(x)^3
                    else if (node.Children[1].IsExponent() && node.Children[1].Children[0].IsNumber() &&
                             node.Children[0].Matches(node.Children[1].Children[1]))
                    {
                        Write("\tIncrease Exponent");
                        node.Replace(node.Children[0], new RPN.Node(1));
                        node.Replace(node.Children[1].Children[0],
                            new RPN.Node(node.Children[1].Children[0].GetNumber() + 1));
                    }
                    else if (node.Children[0].IsExponent() && node.Children[1].IsMultiplication() &&
                             node.Children[0].Children[0].IsGreaterThanNumber(0) && node.Children[1].Children[0]
                                 .Matches(node.Children[0].Children[1]))
                    {
                        Write($"\tIncrease Exponent 2:");
                        RPN.Node temp = node.Children[0].Children[0];
                        temp.Replace(temp.GetNumber() + 1);
                        node.Children[1].Children[0].Remove(new RPN.Node(1));
                    }
                    else if (node.Children[0].IsExponent() && node.Children[1].IsMultiplication() &&
                             node.Children[0].Children[1].Matches(node.Children[1]))
                    {
                        Write("\tIncrease Exponent 3");
                        RPN.Node temp = node.Children[0].Children[0];
                        temp.Replace(temp.GetNumber() + 1);
                        node.Children[1].Remove(new RPN.Node(1));
                    }
                    else if (node.Children[1].IsNumber() && node.Children[0].IsMultiplication() &&
                             node.Children[0].Children[1].IsNumber() && !node.Children[0].Children[0].IsNumber())
                    {
                        Write($"\tDual Node Multiplication.");
                        double num1 = double.Parse(node.Children[0].Children[1].Token.Value);
                        double num2 = double.Parse(node.Children[1].Token.Value);

                        node.Children[0].Replace(node.Children[0].Children[1], new RPN.Node(1));
                        node.Replace(node.Children[1], new RPN.Node(num1 * num2));
                    }
                    else if ((node.Children[0].IsDivision() || node.Children[1].IsDivision()) &&
                             !(node.Children[0].IsDivision() && node.Children[1].IsDivision()))
                    {
                        Write($"\tExpression times a division -> Division ");
                        RPN.Node division;
                        RPN.Node expression;
                        if (node.Children[0].IsDivision())
                        {
                            division = node.Children[0];
                            expression = node.Children[1];
                        }
                        else
                        {
                            division = node.Children[1];
                            expression = node.Children[0];
                        }

                        RPN.Node numerator = division.Children[1];
                        RPN.Node multiply = new RPN.Node(new[] { Clone(numerator), Clone(expression) },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        numerator.Remove(multiply);
                        expression.Remove(new RPN.Node(1));
                    }
                    else if (node.Children[0].IsDivision() && node.Children[1].IsDivision())
                    {
                        Write($"\tDivision times a division -> Division");
                        RPN.Node[] numerator = { node.Children[0].Children[1], node.Children[1].Children[1] };
                        RPN.Node[] denominator = { node.Children[0].Children[0], node.Children[1].Children[0] };
                        RPN.Token multiply = new RPN.Token("*", 2, RPN.Type.Operator);

                        RPN.Node top = new RPN.Node(numerator, multiply);
                        RPN.Node bottom = new RPN.Node(denominator, multiply);
                        RPN.Node division = new RPN.Node(new[] { bottom, top }, new RPN.Token("/", 2, RPN.Type.Operator));

                        node.Children[0].Remove(division);
                        node.Children[1].Remove(new RPN.Node(1));
                    }
                    else if (node.Children[0].IsLessThanNumber(0) && node.Children[1].IsLessThanNumber(0))
                    {
                        Write("\tA negative times a negative is always positive.");
                        node.Replace(node.Children[0],
                            new RPN.Node(System.Math.Abs(double.Parse(node.Children[0].Token.Value))));
                        node.Replace(node.Children[1],
                            new RPN.Node(System.Math.Abs(double.Parse(node.Children[1].Token.Value))));
                    }
                    else if (node[0].IsMultiplication() && node[0, 1].IsLessThanNumber(0) &&
                             node[1].IsLessThanNumber(0))
                    {
                        Write("\tComplex: A negative times a negative is always positive.");
                        node.Replace(node[0, 1], new RPN.Node(System.Math.Abs(node[0, 1].GetNumber())));
                        node.Replace(node[1], new RPN.Node(System.Math.Abs(node[1].GetNumber())));
                    }
                    else if (node[0].IsNumber(-1) && node[1].IsNumber())
                    {
                        Write("\t-1 * c -> -c");
                        node.Replace(node[0], new RPN.Node(1));
                        node.Replace(node[1], new RPN.Node(node[1].GetNumber() * -1));
                    }
                    else if (node[0].IsNumber() && node[1].IsNumber(-1))
                    {
                        Write("\tc * -1 -> -c");
                        node.Replace(node[1], new RPN.Node(1));
                        node.Replace(node[0], new RPN.Node(node[0].GetNumber() * -1));
                    }
                    else if (node[0].IsSubtraction() && node[1].IsNumber(-1))
                    {
                        Write("\t-1[f(x) - g(x)] -> -f(x) + g(x) -> g(x) - f(x)");
                        node[0].Swap(0, 1);
                        node[1].Replace(1);
                    }
                }
                else if (mode == SimplificationMode.Swap)
                {
                    //We can do complex swapping in here
                    if (node.IsMultiplication() && node.Children[0].IsMultiplication() &&
                        node.Children[0].Children[0].Matches(node.Children[1]))
                    {
                        Write($"\tComplex Swap: Dual Node Multiplication Swap");
                        RPN.Node temp = node.Children[0].Children[1];


                        node.Children[0].Children[1] = node.Children[1];
                        node.Children[1] = temp;
                    }
                    else if (node.IsMultiplication() && node.Children[0].IsMultiplication() &&
                             node.Children[1].IsMultiplication())
                    {
                        if (node.Children[0].Children[1].IsNumber() && node.Children[1].Children[1].IsNumber())
                        {
                            Write($"\tComplex Swap: Tri Node Multiplication Swap");
                            RPN.Node multiply =
                                new RPN.Node(
                                    new[] { Clone(node.Children[0].Children[1]), Clone(node.Children[1].Children[1]) },
                                    new RPN.Token("*", 2, RPN.Type.Operator));
                            node.Children[1].Children[1].Remove(multiply);
                            node.Children[0].Children[1].Remove(new RPN.Node(1));
                        }
                    }
                }
                else if (mode == SimplificationMode.Exponent && node.IsExponent())
                {
                    RPN.Node baseNode = node.Children[1];
                    RPN.Node power = node.Children[0];
                    if (power.IsNumber(1))
                    {
                        Write("\tf(x)^1 -> f(x)");
                        Assign(node, baseNode);
                        power.Delete();
                        node.Delete();
                    }
                    else if (power.IsNumber(0))
                    {
                        Write("\tf(x)^0 -> 1");
                        node.Replace(1);
                        node.Children.Clear();
                    }
                    else if (baseNode.IsNumber(1))
                    {
                        Write("\t1^(fx) -> 1");
                        node.Replace(1);
                        node.Children.Clear();
                    }
                    else if (power.IsLessThanNumber(0))
                    {
                        RPN.Node powerClone = new RPN.Node(new[] { new RPN.Node(-1), Clone(power) },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node exponent = new RPN.Node(new[] { powerClone, Clone(baseNode) },
                            new RPN.Token("^", 2, RPN.Type.Operator));
                        RPN.Node division = new RPN.Node(new[] { exponent, new RPN.Node(1) },
                            new RPN.Token("/", 2, RPN.Type.Operator));
                        Assign(power.Parent, division);
                        Write($"\tf(x)^-c -> 1/f(x)^c");
                    }
                    else if (power.IsNumber(0.5))
                    {
                        RPN.Node sqrt = new RPN.Node(new[] { Clone(baseNode) },
                            new RPN.Token("sqrt", 1, RPN.Type.Function));
                        Assign(power.Parent, sqrt);
                        Write("\tf(x)^0.5 -> sqrt( f(x) )");
                    }
                    else if ((power.IsNumber() || power.IsConstant()) && baseNode.IsExponent() &&
                             (baseNode.Children[0].IsNumber() || baseNode.Children[0].IsConstant()))
                    {
                        Write("\t(f(x)^c)^a -> f(x)^[c * a]");
                        RPN.Node multiply;

                        if (power.IsNumber() && baseNode.Children[0].IsNumber())
                        {
                            multiply = new RPN.Node(power.GetNumber() * baseNode.Children[0].GetNumber());
                        }
                        else
                        {
                            multiply = new RPN.Node(new[] { Clone(power), Clone(baseNode.Children[0]) },
                                new RPN.Token("*", 2, RPN.Type.Operator));
                        }

                        RPN.Node func = Clone(baseNode.Children[1]);
                        RPN.Node exponent = new RPN.Node(new[] { multiply, func },
                            new RPN.Token("^", 2, RPN.Type.Operator));
                        Assign(power.Parent, exponent);
                    }
                    else if (power.IsNumberOrConstant() && baseNode.IsLessThanNumber(0) && power.GetNumber() % 2 == 0)
                    {
                        Write("c_1^c_2 where c_2 % 2 = 0 and c_1 < 0 -> [-1 * c_1]^c_2");
                        node.Replace(baseNode, new RPN.Node(-1 * baseNode.GetNumber()));
                    }
                }
                else if (mode == SimplificationMode.Constants)
                {
                    if (node.IsMultiplication())
                    {
                        if (node[0].IsInteger() && node[1].IsInteger())
                        {
                            Solve(node);
                        }
                        else if (node[0].IsNumber() && node[1].IsNumber())
                        {
                            if ((int)(node[0].GetNumber() * node[1].GetNumber()) ==
                                node[0].GetNumber() * node[1].GetNumber())
                            {
                                Solve(node);
                            }
                        }

                    }
                    else if (node.IsExponent() && node[0].IsInteger() && node[1].IsInteger())
                    {
                        Solve(node);
                    }
                }

                //Propagate down the tree IF there is a root 
                //which value is not NaN or a number
                if (Root == null || Root.IsNumber() || Root.IsNumber(double.NaN))
                {
                    return;
                }


                SW.Stop();
                _data.AddTimeRecord("AST.Simplify:Compute", SW);
                SW.Restart();
                //Propagate down the tree
                for (int i = (node.Children.Count - 1); i >= 0; i--)
                {
                    stack.Push(node.Children[i]);
                }

                _data.AddTimeRecord("AST.Simplify:Propogate", SW);
            }

        }

        private void Swap(RPN.Node node)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();


            Queue<RPN.Node> unvisited = new Queue<RPN.Node>();
            unvisited.Enqueue(node);

            while (unvisited.Count > 0)
            {
                node = unvisited.Dequeue();

                for (int i = (node.Children.Count - 1); i >= 0; i--)
                {
                    unvisited.Enqueue(node.Children[i]);
                }

                //Addition operator
                if (node.IsAddition())
                {
                    if (node[1].IsNumberOrConstant() && !node[0].IsNumberOrConstant())
                    {
                        Write("\tNode Swap: Constants and numbers always yield.");
                        node.Swap(0, 1);
                    }
                    else if (node[1].IsVariable() && node[0].IsMultiplication() && !node[0].IsSolveable())
                    {
                        Write($"\tNode Swap: Single variables yields to generic expression");
                        node.Swap(0, 1);
                    }
                    else if (node[1].IsVariable() && node[0].IsExponent())
                    {
                        Write("\tNode Swap: Single variables yields to exponent");
                        node.Swap(0, 1);
                    }
                    else if (node[1].IsMultiplication() && node[0].IsMultiplication() &&
                             !node.Children[1].Children.Any(n => n.IsExponent()) &&
                             node.Children[0].Children.Any(n => n.IsExponent()))
                    {
                        Write("\tNode Swap:Straight multiplication gives way to multiplication with an exponent");
                        node.Swap(0, 1);
                    }

                    //TODO: A straight exponent should give way to a multiplication with an exponent if...
                    //TODO: Swapping exponent with non exponent
                }
                //Multiplication operator
                else if (node.IsMultiplication())
                {
                    //Numbers and constants take way
                    if (!node.Children[1].IsNumberOrConstant() && node.Children[0].IsNumberOrConstant())
                    {
                        node.Swap(0, 1);
                        Write("\tNode Swap: Numbers and constants take way.");
                    }
                    //Sort functions alphabetically
                    else if (node[1].IsFunction() && node[0].IsFunction() && !node[1].IsConstant() &&
                             !node[0].IsConstant())
                    {
                        StringComparison foo = StringComparison.CurrentCulture;

                        int comparison = string.Compare(node.Children[0].Token.Value, node.Children[1].Token.Value
                            , foo);
                        if (comparison == -1)
                        {
                            node.Swap(0, 1);
                        }
                    }
                    else if (!(node.Children[1].IsExponent() || node.Children[1].IsNumberOrConstant() ||
                               node.Children[1].IsSolveable()) && node.Children[0].IsExponent())
                    {
                        Write("\tNode Swap: Exponents take way");
                        node.Swap(0, 1);
                    }
                    else if (node[1].IsVariable() && node[0].IsExponent())
                    {
                        Write("\tNode Swap: Variable yields to Exponent");
                        node.Swap(0, 1);
                    }

                    //a number and a expression
                    else if (node.Children[0].IsNumber() &&
                             !(node.Children[1].IsNumber() || node.Children[1].IsVariable()))
                    {
                        Write($"\tMultiplication Swap.");
                        node.Swap(1, 0);
                    }
                }
            }

            sw.Stop();
            this._rpn.Data.AddTimeRecord("AST.Swap :: AST Simplify", sw);
        }

        private void InternalSwap(RPN.Node node)
        {
            Queue<RPN.Node> unvisited = new Queue<RPN.Node>();
            unvisited.Enqueue(node);

            while (unvisited.Count > 0)
            {
                node = unvisited.Dequeue();
                //Propagate down the tree
                for (int i = (node.Children.Count - 1); i >= 0; i--)
                {
                    unvisited.Enqueue(node.Children[i]);
                }

                if (node.IsFunction("internal_sum") || node.IsFunction("sum"))
                {
                    /*
                    1) A constant or number should always be swapped with any other expression if it comes before another expression if 
                    that expression is not a constant or number. 
                    2) An expression that has a multiplication or exponent can only be swapped if it has a higher exponent or coefficient
    
                    Swapping should be done til there are no more changes on the tree.
                    */
                    //x + 2 + 2x -> 2x + x + 2
                    //x + 2 + x^2 -> x^2 + x + 2
                    //5 + x^3 + x + x^2 + 2x + 3x^2
                    node.Children.Reverse();
                    string hash = string.Empty;
                    while (node.GetHash() != hash)
                    {
                        hash = node.GetHash();
                        //Swapping code here
                        for (int i = 0; i < node.Children.Count; i++)
                        {
                            if (i - 1 < 0)
                            {
                                continue;
                            }

                            //Constants and numbers should give way.
                            if ((node.Children[i - 1].IsNumber() || node.Children[i - 1].IsConstant()) &&
                                !(node.Children[i].IsNumber() || node.Children[i].IsConstant()))
                            {
                                node.Swap(i - 1, i);
                                Write($"\tConstants and numbers always yield: Swap {i - 1} and {i}. {node.ToInfix()}");
                            }
                            //Single variables give way to other expressions that are not constants and numbers 
                            else if (node.Children[i - 1].IsVariable() &&
                                     (node.Children[i].IsMultiplication() ||
                                      node.Children[i].IsFunction("internal_product")) &&
                                     !node.Children[i].IsSolveable())
                            {
                                node.Swap(i - 1, i);
                                Write(
                                    $"\tSingle variables yields to generic expression: Swap {i - 1} and {i}. {node.ToInfix()}");
                            }
                            //Single variable gives way to exponent 
                            else if (node.Children[i - 1].IsVariable() && node.Children[i].IsExponent())
                            {
                                node.Children.Swap(i - 1, i);
                                Write($"\tSingle variables yields to exponent: Swap {i - 1} and {i}. {node.ToInfix()}");
                            }
                            //Straight multiplication gives way to multiplication with an exponent
                            else if ((node.Children[i - 1].IsMultiplication() ||
                                      node.Children[i].IsFunction("internal_product")) &&
                                     !node.Children[i - 1].Children.Any(n => n.IsExponent()) &&
                                     (node.Children[i].IsMultiplication() ||
                                      node.Children[i].IsFunction("internal_product")) &&
                                     node.Children[i].Children.Any(n => n.IsExponent()))
                            {
                                node.Children.Swap(i - 1, i);
                                Write(
                                    $"\tStraight multiplication gives way to multiplication with an exponent: Swap {i - 1} and {i}. {node.ToInfix()}");
                            }
                            //A straight exponent should give way to a multiplication with an exponent if...
                            else if (node.Children[i - 1].IsExponent() &&
                                     (node.Children[i].IsMultiplication() ||
                                      node.Children[i].IsFunction("internal_product")) &&
                                     node.Children[i].Children[0].IsExponent())
                            {
                                //its degree is higher or equal
                                if (node[i - 1, 0].IsNumberOrConstant() && node[i, 0, 0].IsNumberOrConstant() &&
                                    node[i, 0, 0].IsGreaterThanOrEqualToNumber(node[i - 1, 0].GetNumber()))
                                {
                                    node.Children.Swap(i - 1, i);
                                    Write(
                                        $"\tA straight exponent should give way to a multiplication with an exponent if its degree is higher or equal : Swap {i - 1} and {i}. {node.ToInfix()}");
                                }

                                //TODO: its degree is an expression and the straight exponent's is not an expression 
                            }
                            else if ((node.Children[i].IsMultiplication() ||
                                      node.Children[i].IsFunction("internal_product")) &&
                                     node.Children[i].Children[1].IsExponent() &&
                                     !node.Children[i].Children[0].IsExponent())
                            {
                                node.Children[i].Children.Swap(0, 1);
                                Write("\tSwapping exponent with nonexponent");
                            }
                        }
                    }
                    //Write(node.Print()); //TODO
                }
                else if (node.IsFunction("internal_product") || node.IsFunction("product"))
                {
                    node.Children.Reverse();
                    string hash = string.Empty;
                    while (node.GetHash() != hash)
                    {
                        hash = node.GetHash();
                        for (int i = 0; i < node.Children.Count; i++)
                        {
                            if (i - 1 < 0)
                            {
                                continue;
                            }

                            //Numbers and constants take way
                            if (!node.Children[i - 1].IsNumberOrConstant() && node.Children[i].IsNumberOrConstant())
                            {
                                node.Children.Swap(i - 1, i);
                                Write("\tNumbers and constants take way.");
                            }
                            else if (node[i - 1].Matches(node.Children[i]))
                            {

                                Write("\tIP: f(x) * f(x) -> f(x) ^ 2");

                                RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), node[i] },
                                    new RPN.Token("^", 2, RPN.Type.Operator));
                                node.Replace(node[i], exponent);
                                node.Replace(node[i - 1], new RPN.Node(1));
                            }
                            //Sort functions alphabetically
                            else if (node[i - 1].IsFunction() && node[i].IsFunction() && !node[i - 1].IsConstant() &&
                                     !node[i].IsConstant())
                            {
                                StringComparison foo = StringComparison.CurrentCulture;

                                int comparison = string.Compare(node.Children[i].Token.Value,
                                    node.Children[i - 1].Token.Value
                                    , foo);
                                if (comparison == -1)
                                {
                                    node.Children.Swap(i - 1, i);
                                }
                            }
                            else if (!(node.Children[i - 1].IsExponent() || node.Children[i - 1].IsNumberOrConstant() ||
                                       node.Children[i - 1].IsSolveable()) && node.Children[i].IsExponent())
                            {
                                Write("\tExponents take way");
                                node.Children.Swap(i - 1, i);
                            }
                            else if (node[i - 1].IsVariable() && node[i].IsExponent())
                            {
                                Write("\tVariable yields to Exponent");
                                node.Children.Swap(i - 1, i);
                            }

                            //TODO: Exponents and other expressions right of way
                        }
                    }
                    Write(node.Print());
                }
            }
        }

        /// <summary>
        /// Simplifies or evaluates meta functions that
        /// cannot be easily represented or understood by PostFix.
        /// </summary>
        public AST MetaFunctions()
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();

            //This makes derive an internal only function
            //when called from the outside of this function
            //it will not appear to be a function
            if (!_rpn.Data.Functions.ContainsKey("derive"))
            {
                _rpn.Data.AddFunction("derive", new RPN.Function { Arguments = 1, MaxArguments = 1, MinArguments = 1 });
            }

            bool go = MetaFunctions(Root);

            if (_rpn.Data.Functions.ContainsKey("derive"))
            {
                _rpn.Data.RemoveFunction("derive");
            }

            SW.Stop();

            _data.AddTimeRecord("AST MetaFunctions", SW);

            if (go)
            {
                Simplify();
            }


            return this;
        }

        private bool MetaFunctions(RPN.Node node)
        {
            //Propagate down the tree
            for (int i = 0; i < node.Children.Count; i++)
            {
                MetaFunctions(node.Children[i]);
            }

            if (node.IsFunction() && _data.MetaFunctions.Contains(node.Token.Value))
            {
                if (node.IsFunction("integrate"))
                {
                    double answer = double.NaN;
                    if (node.Children.Count == 4)
                    {
                        node.Children.Insert(0, new RPN.Node(0.001));
                    }

                    if (node.Children.Count == 5)
                    {
                        answer = MetaCommands.Integrate(_rpn,
                            node.Children[4],
                            node.Children[3],
                            node.Children[2],
                            node.Children[1],
                            node.Children[0]);
                    }

                    RPN.Node temp = new RPN.Node(answer);
                    Assign(node, temp);
                }
                else if (node.IsFunction("table"))
                {
                    string table;

                    if (node.Children.Count == 4)
                    {
                        node.Children.Insert(0, new RPN.Node(0.001));
                    }

                    table = MetaCommands.Table(_rpn,
                        node.Children[4],
                        node.Children[3],
                        node.Children[2],
                        node.Children[1],
                        node.Children[0]);

                    stdout(table);
                    SetRoot(new RPN.Node(double.NaN));
                }
                else if (node.IsFunction("derivative"))
                {
                    if (node.Children.Count == 2)
                    {
                        GenerateDerivativeAndReplace(node.Children[1]);
                        Derive(node.Children[0]);
                        Assign(node, node.Children[1]);
                        node.Delete();
                    }
                    else if (node.Children.Count == 3)
                    {
                        if (!node[0].IsNumberOrConstant() && (int)node[0].GetNumber() == node[0].GetNumber())
                        {
                            throw new Exception("Expected a number or constant");
                        }

                        //This code is suspect!
                        int count = (int)node[0].GetNumber();

                        node.RemoveChild(node[0]);


                        for (int i = 0; i < count; i++)
                        {
                            GenerateDerivativeAndReplace(node.Children[1]);
                            Derive(node.Children[0]);
                            Simplify(node);
                        }
                        Assign(node, node.Children[1]);
                        node.Delete();


                    }
                }
                else if (node.IsFunction("solve"))
                {
                    if (node.Children.Count == 2)
                    {
                        node.AddChild(new RPN.Node(new RPN.Token("=", 2, RPN.Type.Operator)));
                    }
                    else
                    {
                        node.Swap(0, 2);
                        node[2].Children.Clear();
                    }

                    Write(node.Print());

                    RPN.Node temp = node[2];
                    node.RemoveChild(temp);

                    Algebra(node, ref temp);

                    temp.AddChild(new[] { node[0], node[1] });

                    //This is done to fix a bug
                    if (!temp.IsOperator("="))
                    {
                        temp.Swap(0, 1);
                    }

                    node.AddChild(temp);
                    Write($"{node[1].ToInfix()} {node[2].ToInfix()} {node[0].ToInfix()}");
                    node.RemoveChild(temp);

                    Write(temp.ToInfix());
                    Assign(node, temp);
                }
                else if (node.IsFunction("sum"))
                {

                }

                return true;
            }

            return false;
        }

        private void Solve(RPN.Node node)
        {
            if (node == null)
            {
                return;
            }

            //All children of a node must be either numbers of constant functions
            bool isSolveable = node.IsSolveable();

            //Functions that are not constants and/or meta functions 
            if ((node.IsFunction() && !(node.IsConstant() || _data.MetaFunctions.Contains(node.Token.Value)) || node.IsOperator()) && isSolveable)
            {
                PostFix math = new PostFix(_rpn);
                double answer = math.Compute(node.ToPostFix().ToArray());
                Assign(node, new RPN.Node(answer));
                //Since we solved something lower in the tree we may be now able 
                //to solve something higher up in the tree!
                Solve(node.Parent);
            }

            //Propagate down the tree
            for (int i = 0; i < node.Children.Count; i++)
            {
                Solve(node.Children[i]);
            }
        }


        private AST Derive(RPN.Node variable)
        {
            if (!variable.IsVariable())
            {
                throw new ArgumentException("The variable of deriviation is not a variable!", nameof(variable));
            }

            Simplify(Root);
            //Simplify(Root, SimplificationMode.Constants);

            Write($"Starting to derive ROOT: {Root.ToInfix()}");
            Derive(Root, variable);

            Write("\tSimplifying Post!\n");
            Simplify(Root);
            //Simplify(Root, SimplificationMode.Constants);
            Write("");

            return this;
        }

        private void Derive(RPN.Node foo, RPN.Node variable)
        {
            //We do not know in advance the depth of a tree 
            //and given a big enough expression the current recursion 
            //is more likley to fail compared to an itterative approach.

            Stack<RPN.Node> stack = new Stack<RPN.Node>();
            stack.Push(foo);
            //Write(foo.ToInfix());
            string v = variable.ToInfix();
            RPN.Node node = null;
            while (stack.Count > 0)
            {
                node = stack.Pop();
                //Write($"Current Node: {node.ToInfix()}");
                //Propagate down the tree
                for (int i = (node.Children.Count - 1); i >= 0; i--)
                {
                    stack.Push(node.Children[i]);
                    //Write($"Pushing {node.Children[i]} {stack.Count}");
                }


                if (node.Token.Value != "derive")
                {
                    //Write($"Skipping Node: {node.ToInfix()}");
                    continue;
                }

                if (node.Children[0].IsAddition() || node.Children[0].IsSubtraction())
                {
                    if (debug)
                    {
                        string f_x = node.Children[0].Children[0].ToInfix();
                        string g_x = node.Children[0].Children[1].ToInfix();
                        Write($"\td/d{v}[ {f_x} ± {g_x} ] -> d/d{v}( {f_x} ) ± d/d{v}( {g_x} )");
                    }
                    else
                    {
                        Write("\td/dx[ f(x) ± g(x) ] -> d/dx( f(x) ) ± d/dx( g(x) )");
                    }

                    GenerateDerivativeAndReplace(node.Children[0].Children[0]);
                    GenerateDerivativeAndReplace(node.Children[0].Children[1]);
                    //Recurse explicitly down these branches
                    stack.Push(node.Children[0].Children[0]);
                    stack.Push(node.Children[0].Children[1]);
                    //Delete myself from the tree
                    node.Remove();
                }
                else if (node.Children[0].IsNumber() || node.Children[0].IsConstant() ||
                         (node.Children[0].IsVariable() && node.Children[0].Token.Value != variable.Token.Value) ||
                         node.IsSolveable())
                {
                    if (debug)
                    {
                        Write($"\td/d{v}[ {node.Children[0].ToInfix()} ] -> 0");
                    }
                    else
                    {
                        Write("\td/dx[ c ] -> 0");
                    }

                    node.Children[0].Parent = null;
                    RPN.Node temp = new RPN.Node(0);
                    //Remove myself from the tree
                    node.Remove(temp);
                }
                else if (node.Children[0].IsVariable() && node.Children[0].Token.Value == variable.Token.Value)
                {
                    if (debug)
                    {
                        Write($"\td/d{v}[ {node.Children[0].ToInfix()} ] -> 1");
                    }
                    else
                    {
                        Write("\td/dx[ x ] -> 1");
                    }

                    node.Children[0].Parent = null;
                    RPN.Node temp = new RPN.Node(1);
                    //Remove myself from the tree
                    node.Remove(temp);
                }
                else if (node.Children[0].IsMultiplication())
                {
                    //Both numbers
                    if (node.Children[0].Children[0].IsNumberOrConstant() &&
                        node.Children[0].Children[1].IsNumberOrConstant())
                    {
                        if (debug)
                        {
                            Write(
                                $"\td/d{v}[ {node.Children[0].Children[0].ToInfix()} * {node.Children[0].Children[1].ToInfix()} ] -> 0");
                        }
                        else
                        {
                            Write("\td/dx[ c_0 * c_1 ] -> 0");
                        }

                        RPN.Node temp = new RPN.Node(0);
                        //Remove myself from the tree
                        node.Remove(temp);
                    }
                    //Constant multiplication - 0
                    else if (node.Children[0].Children[0].IsNumberOrConstant() &&
                             node.Children[0].Children[1].IsExpression())
                    {
                        if (debug)
                        {
                            Write(
                                $"\td/d{v}[ {node.Children[0].Children[1].ToInfix()} * {node.Children[0].Children[0].ToInfix()}] -> d/d{v}[ {node.Children[0].Children[1].ToInfix()} ] * {node.Children[0].Children[0].ToInfix()}");
                        }
                        else
                        {
                            Write("\td/dx[ f(x) * c] -> d/dx[ f(x) ] * c");
                        }

                        GenerateDerivativeAndReplace(node.Children[0].Children[1]);
                        //Recurse explicitly down these branches
                        stack.Push(node.Children[0].Children[1]);
                        //Remove myself from the tree
                        node.Remove();
                    }
                    //Constant multiplication - 1
                    else if (node.Children[0].Children[1].IsNumberOrConstant())
                    {
                        if (debug)
                        {
                            string constant = node.Children[0].Children[1].ToInfix();
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ {constant} * {expr}] -> {constant} * d/d{v}[ {expr} ]");
                        }
                        else
                        {
                            Write("\td/dx[ c * f(x)] -> c * d/dx[ f(x) ]");
                        }

                        GenerateDerivativeAndReplace(node.Children[0].Children[0]);
                        //Recurse explicitly down these branches
                        stack.Push(node.Children[0].Children[0]);

                        //Remove myself from the tree
                        node.Remove();
                    }
                    //Product Rule [Two expressions] 
                    else
                    {
                        RPN.Token multiply = new RPN.Token("*", 2, RPN.Type.Operator);

                        RPN.Node fNode = node.Children[0].Children[0];
                        RPN.Node gNode = node.Children[0].Children[1];

                        if (debug)
                        {
                            string f = fNode.ToInfix();
                            string g = gNode.ToInfix();
                            Write($"\td/d{v}[ {f} * {g} ] -> {f} * d/d{v}[ {g} ] + d/d{v}[ {f} ] * {g}");
                        }
                        else
                        {
                            Write($"\td/dx[ f(x) * g(x) ] -> f(x) * d/dx[ g(x) ] + d/dx[ f(x) ] * g(x)");
                        }

                        RPN.Node fDerivative = new RPN.Node(new[] { Clone(fNode) }, _derive);
                        RPN.Node gDerivative = new RPN.Node(new[] { Clone(gNode) }, _derive);

                        RPN.Node multiply1 = new RPN.Node(new[] { gDerivative, fNode }, multiply);
                        RPN.Node multiply2 = new RPN.Node(new[] { fDerivative, gNode }, multiply);

                        RPN.Node add = new RPN.Node(new[] { multiply1, multiply2 },
                            new RPN.Token("+", 2, RPN.Type.Operator));

                        //Remove myself from the tree
                        node.Remove(add);

                        //Explicit recursion
                        stack.Push(fDerivative);
                        stack.Push(gDerivative);
                    }
                }
                else if (node.Children[0].IsDivision())
                {
                    //Quotient Rule
                    RPN.Token multiply = new RPN.Token("*", 2, RPN.Type.Operator);

                    RPN.Node numerator = node.Children[0].Children[1];
                    RPN.Node denominator = node.Children[0].Children[0];

                    RPN.Node numeratorDerivative = new RPN.Node(new[] { Clone(numerator) }, _derive);
                    RPN.Node denominatorDerivative = new RPN.Node(new[] { Clone(denominator) }, _derive);

                    RPN.Node multiplicationOne = new RPN.Node(new[] { numeratorDerivative, denominator }, multiply);
                    RPN.Node multiplicationTwo = new RPN.Node(new[] { denominatorDerivative, numerator }, multiply);

                    RPN.Node subtraction = new RPN.Node(new[] { multiplicationTwo, multiplicationOne },
                        new RPN.Token("-", 2, RPN.Type.Operator));

                    RPN.Node denominatorSquared = new RPN.Node(new[] { new RPN.Node(2), Clone(denominator) },
                        new RPN.Token("^", 2, RPN.Type.Operator));

                    if (debug)
                    {
                        string n = numerator.ToInfix();
                        string d = denominator.ToInfix();
                        Write($"\td/d{v}[ {n} / {d} ] -> [ d/d{v}( {n} ) * {d} - {n} * d/d{v}( {d} ) ]/{d}^2");
                    }
                    else
                    {
                        //d/dx[ f(x)/g(x) ] = [ g(x) * d/dx( f(x)) - f(x) * d/dx( g(x) )]/ g(x)^2
                        Write($"\td/dx[ f(x) / g(x) ] -> [ d/dx( f(x) ) * g(x) - f(x) * d/dx( g(x) ) ]/g(x)^2");
                    }

                    //Replace in tree
                    node.Children[0].Replace(numerator, subtraction);
                    node.Children[0].Replace(denominator, denominatorSquared);
                    //Delete myself from the tree
                    node.Remove();

                    //Explicitly recurse down these branches
                    stack.Push(subtraction);
                }
                //Exponents! 
                else if (node.Children[0].IsExponent())
                {
                    RPN.Node baseNode = node.Children[0].Children[1];
                    RPN.Node power = node.Children[0].Children[0];
                    if ((baseNode.IsVariable() || baseNode.IsFunction() || baseNode.IsExpression()) &&
                        power.IsNumberOrConstant() && baseNode.Token.Value == variable.Token.Value)
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix();
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ {b}^{p} ] -> {p} * {b}^({p} - 1)");
                        }
                        else
                        {
                            Write("\td/dx[ x^n ] -> n * x^(n - 1)");
                        }

                        RPN.Node powerClone = Clone(power);
                        RPN.Node exponent;

                        if (!powerClone.IsNumber())
                        {
                            //1
                            RPN.Node one = new RPN.Node(1);

                            //(n - 1)
                            RPN.Node subtraction = new RPN.Node(new[] { one, powerClone },
                                new RPN.Token("-", 2, RPN.Type.Operator));

                            //x^(n - 1) 
                            exponent = new RPN.Node(new RPN.Node[] { subtraction, baseNode },
                                new RPN.Token("^", 2, RPN.Type.Operator));
                        }
                        else
                        {
                            exponent = new RPN.Node(new RPN.Node[] { new RPN.Node(powerClone.GetNumber() - 1), baseNode },
                                new RPN.Token("^", 2, RPN.Type.Operator));
                        }

                        RPN.Node multiplication = new RPN.Node(new[] { exponent, power },
                            new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], multiplication);

                        //Delete self from the tree
                        node.Remove();
                    }
                    else if ((baseNode.IsFunction() || baseNode.IsExpression()) && power.IsNumberOrConstant())
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix();
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ {b}^{p} ] -> {p} * {b}^({p} - 1) * d/d{v}[ {b} ]");
                        }
                        else
                        {
                            Write("\td/dx[ f(x)^n ] -> n * f(x)^(n - 1) * d/dx[ f(x) ]");
                        }

                        RPN.Node bodyDerive = new RPN.Node(new[] { Clone(baseNode) }, _derive);

                        RPN.Node powerClone = Clone(power);

                        RPN.Node subtraction;
                        if (power.IsConstant())
                        {
                            RPN.Node one = new RPN.Node(1);
                            subtraction = new RPN.Node(new[] { one, powerClone },
                                new RPN.Token("-", 2, RPN.Type.Operator));
                        }
                        else
                        {
                            subtraction = new RPN.Node(power.GetNumber() - 1);
                        }

                        //Replace n with (n - 1) 
                        RPN.Node exponent = new RPN.Node(new RPN.Node[] { subtraction, baseNode },
                            new RPN.Token("^", 2, RPN.Type.Operator));

                        RPN.Node temp = new RPN.Node(new[] { exponent, power }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node multiply = new RPN.Node(new[] { bodyDerive, temp },
                            new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], multiply);

                        //Delete self from the tree
                        node.Remove();
                        stack.Push(bodyDerive);
                    }
                    else if (baseNode.IsConstant("e"))
                    {
                        if (debug)
                        {
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ e^{p} ] -> d/d{v}[ {p} ] * e^{p}");
                        }
                        else
                        {
                            Write("\td/dx[ e^g(x) ] -> d/dx[ g(x) ] * e^g(x)");
                        }

                        RPN.Node exponent = baseNode.Parent;
                        RPN.Node powerDerivative = new RPN.Node(new[] { Clone(power) }, _derive);
                        RPN.Node multiply = new RPN.Node(new[] { powerDerivative, exponent },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        node.Replace(power.Parent, multiply);
                        //Delete self from the tree
                        node.Remove();
                        stack.Push(powerDerivative);
                    }
                    else if (baseNode.IsNumberOrConstant() && (power.IsExpression() || power.IsVariable()))
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix();
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ {b}^{p} ] -> ln({b}) * {b}^{p} * d/d{v}[ {p} ]");
                        }
                        else
                        {
                            Write($"\td/dx[ b^g(x) ] -> ln(b) * b^g(x) * d/dx[ g(x) ]");
                        }

                        RPN.Node exponent = baseNode.Parent;
                        RPN.Node ln = new RPN.Node(new[] { Clone(baseNode) }, new RPN.Token("ln", 1, RPN.Type.Function));
                        RPN.Node powerDerivative = new RPN.Node(new[] { Clone(power) }, _derive);
                        RPN.Node temp = new RPN.Node(new[] { exponent, ln }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node multiply = new RPN.Node(new[] { temp, powerDerivative },
                            new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(power.Parent, multiply);
                        //Delete self from the tree
                        node.Remove();
                        stack.Push(powerDerivative);
                    }
                    else
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix();
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ {b}^{p} ] -> {b}^{p} * d/d{v}[ {b} * ln( {p} ) ]");
                        }
                        else
                        {
                            Write("\td/dx[ f(x)^g(x) ] -> f(x)^g(x) * d/dx[ g(x) * ln( f(x) ) ]");
                        }

                        RPN.Node exponent = Clone(baseNode.Parent);
                        RPN.Node ln = new RPN.Node(new[] { Clone(baseNode) }, new RPN.Token("ln", 1, RPN.Type.Function));
                        RPN.Node temp = new RPN.Node(new[] { Clone(power), ln },
                            new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node derive = new RPN.Node(new[] { temp }, _derive);
                        RPN.Node multiply = new RPN.Node(new[] { exponent, derive },
                            new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(power.Parent, multiply);
                        //Delete self from the tree
                        node.Remove();

                        stack.Push(derive);
                    }
                }

                #region Trig

                else if (node.Children[0].IsFunction("sin"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ sin({expr}) ] -> cos({expr}) * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ sin(g(x)) ] -> cos(g(x)) * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];

                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node cos = new RPN.Node(new[] { body }, new RPN.Token("cos", 1, RPN.Type.Function));

                    RPN.Node multiply = new RPN.Node(new[] { cos, bodyDerive }, new RPN.Token("*", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("cos"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ cos({expr}) ] -> -sin({expr}) * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ cos(g(x)) ] -> -sin(g(x)) * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node sin = new RPN.Node(new[] { body }, new RPN.Token("sin", 1, RPN.Type.Function));
                    RPN.Node negativeOneMultiply = new RPN.Node(new[] { new RPN.Node(-1), sin },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node multiply = new RPN.Node(new[] { negativeOneMultiply, bodyDerive },
                        new RPN.Token("*", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("tan"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ tan({expr}) ] -> sec({expr})^2 * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ tan(g(x)) ] -> sec(g(x))^2 * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node sec = new RPN.Node(new[] { body }, new RPN.Token("sec", 1, RPN.Type.Function));
                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), sec },
                        new RPN.Token("^", 2, RPN.Type.Operator));

                    RPN.Node multiply = new RPN.Node(new[] { exponent, bodyDerive },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("sec"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ sec({expr}) ] -> tan({expr}) * sec({expr}) * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ sec(g(x)) ] -> tan(g(x)) * sec(g(x)) * d/dx[ g(x) ]");
                    }

                    RPN.Token multiplyToken = new RPN.Token("*", 2, RPN.Type.Operator);

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node sec = node.Children[0];
                    RPN.Node tan = new RPN.Node(new[] { Clone(body) }, new RPN.Token("tan", 1, RPN.Type.Function));
                    RPN.Node temp = new RPN.Node(new[] { sec, tan }, multiplyToken);
                    RPN.Node multiply = new RPN.Node(new[] { bodyDerive, temp }, multiplyToken);

                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("csc"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ csc({expr}) ] -> - cot({expr}) * csc({expr}) * d/d{v}[ {expr} ] ");
                    }
                    else
                    {
                        Write("\td/dx[ csc(g(x)) ] -> - cot(g(x)) * csc(g(x)) * d/dx[ g(x) ] ");
                    }

                    RPN.Token multiplyToken = new RPN.Token("*", 2, RPN.Type.Operator);

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node csc = node.Children[0];
                    RPN.Node cot = new RPN.Node(new[] { Clone(body) }, new RPN.Token("cot", 1, RPN.Type.Function));

                    RPN.Node temp = new RPN.Node(new[] { csc, cot }, multiplyToken);
                    RPN.Node multiply = new RPN.Node(new[] { temp, bodyDerive }, multiplyToken);

                    node.Replace(node.Children[0], new RPN.Node(new[] { new RPN.Node(-1), multiply }, multiplyToken));
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("cot"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ cot({expr}) ] -> -csc({expr})^2 * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ cot(g(x)) ] -> -csc(g(x))^2 * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node csc = new RPN.Node(new[] { body }, new RPN.Token("csc", 1, RPN.Type.Function));
                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), csc },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node temp = new RPN.Node(new[] { new RPN.Node(-1), exponent },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node multiply =
                        new RPN.Node(new[] { bodyDerive, temp }, new RPN.Token("*", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arcsin"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arcsin({expr}) ] -> d/d{v}[ {expr} ]/sqrt(1 - {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arcsin(g(x)) ] -> d/dx[ g(x) ]/sqrt(1 - g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node subtraction = new RPN.Node(new[] { exponent, new RPN.Node(1) },
                        new RPN.Token("-", 2, RPN.Type.Operator));
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node division =
                        new RPN.Node(new[] { sqrt, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arccos"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arccos({expr}) ] -> -1 * d/d{v}[ {expr} ]/sqrt(1 - {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arccos(g(x)) ] -> -1 * d/dx[ g(x) ]/sqrt(1 - g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node subtraction = new RPN.Node(new[] { exponent, new RPN.Node(1) },
                        new RPN.Token("-", 2, RPN.Type.Operator));
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node division =
                        new RPN.Node(new[] { sqrt, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                    RPN.Node multiplication = new RPN.Node(new[] { new RPN.Node(-1), division },
                        new RPN.Token("*", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], multiplication);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arctan"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arctan({expr}) ] -> d/d{v}[ {expr} ]/(1 + {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arctan(g(x)) ] -> d/dx[ g(x) ]/(1 + g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node add = new RPN.Node(new[] { new RPN.Node(1), exponent },
                        new RPN.Token("+", 2, RPN.Type.Operator));
                    RPN.Node division = new RPN.Node(new[] { add, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arccot"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arccot({expr}) ] -> -1 * d/d{v}[ {expr} ]/(1 + {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arccot(g(x)) ] -> -1 * d/dx[ g(x) ]/(1 + g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node add = new RPN.Node(new[] { new RPN.Node(1), exponent },
                        new RPN.Token("+", 2, RPN.Type.Operator));
                    RPN.Node multiplication = new RPN.Node(new[] { new RPN.Node(-1), bodyDerive },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node division = new RPN.Node(new[] { add, multiplication },
                        new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arcsec"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arcsec({expr}) ] -> d/d{v}[ {expr} ]/( {expr} * sqrt({expr}^2 - 1 ) )");
                    }
                    else
                    {
                        Write("\td/dx[ arcsec(g(x)) ] -> d/dx[ g(x) ]/( g(x) * sqrt(g(x)^2 - 1 ) )");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node subtraction = new RPN.Node(new[] { new RPN.Node(1), exponent },
                        new RPN.Token("-", 2, RPN.Type.Operator));
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node denominator =
                        new RPN.Node(new[] { sqrt, Clone(body) }, new RPN.Token("*", 2, RPN.Type.Operator));

                    RPN.Node division = new RPN.Node(new[] { denominator, bodyDerive },
                        new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("arccsc"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ arccsc({expr}) ] -> -1 * d/d{v}[ {expr} ]/( {expr} * sqrt({expr}^2 - 1 ) )");
                    }
                    else
                    {
                        Write("\td/dx[ arccsc(g(x)) ] -> -1 * d/dx[ g(x) ]/( g(x) * sqrt(g(x)^2 - 1 ) )");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node subtraction = new RPN.Node(new[] { new RPN.Node(1), exponent },
                        new RPN.Token("-", 2, RPN.Type.Operator));
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node denominator =
                        new RPN.Node(new[] { sqrt, Clone(body) }, new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node multiplication = new RPN.Node(new[] { new RPN.Node(-1), bodyDerive },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node division = new RPN.Node(new[] { denominator, multiplication },
                        new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }

                #endregion

                else if (node.Children[0].IsSqrt())
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\tsqrt({expr}) -> {expr}^0.5");
                    }
                    else
                    {
                        Write("\tsqrt(g(x)) -> g(x)^0.5");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(.5), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    node.Replace(node.Children[0], exponent);
                    stack.Push(node);
                }
                else if (node.Children[0].IsLn())
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\td/d{v}[ ln({expr}) ] -> d/d{v}[ {expr} ]/{expr}");
                    }
                    else
                    {
                        Write("\td/dx[ ln(g(x)) ] -> d/dx[ g(x) ]/g(x)");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node division =
                        new RPN.Node(new[] { body, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsLog())
                {
                    RPN.Token ln = new RPN.Token("ln", 1, RPN.Type.Function);

                    RPN.Node power = node.Children[0].Children[1];
                    RPN.Node body = node.Children[0].Children[0];

                    if (debug)
                    {
                        string b = body.ToInfix();
                        string p = power.ToInfix();
                        Write($"\td/d{v}[ log({b},{p}) ] -> d/d{v}[ {p} ]/({p} * ln({b}))");
                    }
                    else
                    {
                        Write("\td/dx[ log(b,g(x)) ] -> d/dx[ g(x) ]/(g(x) * ln(b))");
                    }

                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node multiply = new RPN.Node(new[] { body, new RPN.Node(new[] { power }, ln) },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node division = new RPN.Node(new[] { multiply, bodyDerive },
                        new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Replace(node.Children[0], division);
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsAbs())
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix();
                        Write($"\tabs({expr}) -> sqrt( {expr}^2 )");
                    }
                    else
                    {
                        Write("\tabs(g(x)) -> sqrt( g(x)^2 )");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node exponent = new RPN.Node(new[] { new RPN.Node(2), body },
                        new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node sqrt = new RPN.Node(new[] { exponent }, new RPN.Token("sqrt", 1, RPN.Type.Function));

                    node.Replace(node.Children[0], sqrt);
                    stack.Push(node);
                }
                else if (node.Children[0].IsFunction("sum"))
                {
                    Write("\tExploding sum");
                    explode(node.Children[0]);
                    stack.Push(node);
                    stack.Push(node);
                }
                else if (node.Children[0].IsFunction("avg"))
                {
                    Write("\tExploding avg");
                    explode(node.Children[0]);
                    stack.Push(node);
                }
                else
                {
                    throw new NotImplementedException(
                        $"Derivative of {node.Children[0].ToInfix()} not known at this time.");
                }
            }
        }

        private void Algebra(RPN.Node node, ref RPN.Node equality)
        {
            if (!node.IsFunction("solve")) return;

            string hash = string.Empty;

            while (hash != node.GetHash()) {
                hash = node.GetHash();

                if (!node[0].IsNumberOrConstant() && node[0].IsSolveable())
                {
                    Write("\tSolving for one Side.");
                    Solve(node[0]);
                }

                if (!node[1].IsNumberOrConstant() && node[1].IsSolveable())
                {
                    Write("\tSolving for one Side.");
                    Solve(node[1]);
                }

                //TODO: Quadratic Formula


                if (node[0].IsNumberOrConstant() || node[0].IsSolveable())
                {
                    if (node[1].IsAddition())
                    {
                        RPN.Node temp = null;

                        if (node[1, 0].IsNumberOrConstant())
                        {
                            Write("\tf(x) + c = c_1 -> f(x) = c_1 - c");
                            temp = node[1, 0];
                        }
                        else if (node[1, 1].IsNumberOrConstant())
                        {
                            Write("\tc + f(x) = c_1 -> f(x) = c_1 - c");
                            temp = node[1, 1];
                        }

                        if (temp is null)
                        {
                            return;
                        }

                        node[1].Replace(temp, new RPN.Node(0));
                        RPN.Node subtraction = new RPN.Node(new[] { temp, node[0] }, new RPN.Token("-", 2, RPN.Type.Operator));
                        node.Replace(node[0], subtraction);

                        Simplify(node[1], SimplificationMode.Addition);
                    }
                    else if (node[1].IsSubtraction())
                    {
                        RPN.Node temp = null;

                        if (node[1, 0].IsNumberOrConstant())
                        {
                            Write("\t[f(x) - c = c_1] -> [f(x) = c_1 - c]");
                            temp = node[1, 0];

                            node[1].Replace(temp, new RPN.Node(0));
                            RPN.Node addition = new RPN.Node(new[] { temp, node[0] }, new RPN.Token("+", 2, RPN.Type.Operator));
                            node.Replace(node[0], addition);
                        }
                        else if (node[1, 1].IsNumberOrConstant())
                        {
                            Write("\t[c - f(x) = c_1] -> [- f(x) = c_1 - c]");
                            temp = node[1, 1];

                            node[1].Replace(temp, new RPN.Node(0));
                            RPN.Node subtraction = new RPN.Node(new[] { temp, node[0] }, new RPN.Token("-", 2, RPN.Type.Operator));
                            node.Replace(node[0], subtraction);
                        }

                        Simplify(node, SimplificationMode.Subtraction);
                    }
                    else if (node[1].IsMultiplication())
                    {
                        RPN.Node temp = null;
                        if (node[1, 0].IsNumberOrConstant())
                        {
                            temp = node[1, 0];
                            Write("\tf(x)c = g(x) -> f(x) = g(x)/c");
                        }
                        else if (node[1, 1].IsNumberOrConstant())
                        {
                            temp = node[1, 1];
                            Write("\tcf(x) = g(x) -> f(x) = g(x)/c");
                            ; }

                        if (temp != null)
                        {
                            node[1].Replace(temp, new RPN.Node(1));
                            RPN.Node division = new RPN.Node(new[] { temp, node[0] }, new RPN.Token("/", 2, RPN.Type.Operator));
                            node.Replace(node[0], division);
                        }
                    }
                    else if (node[1].IsDivision())
                    {
                        //TODO: f(x)/c = g(x) -> f(x) = c * g(x)
                        if (node[1, 0].IsNumberOrConstant())
                        {
                            RPN.Node temp = node[1, 0];

                            node[1].Replace(temp, new RPN.Node(1));
                            RPN.Node multiplication = new RPN.Node(new[] { temp, node[0] }, new RPN.Token("*", 2, RPN.Type.Operator));
                            node.Replace(node[0], multiplication);
                            Simplify(node, SimplificationMode.Division);
                        }
                    }
                }

                //TODO: Make this equality 
                //This might mean swapping after the equality is completed! 

                //f(x)^2 = g(x)
                if (node[1].IsExponent() && node[1, 0].IsNumber(2))
                {
                    Write("\tf(x)^2 = g(x) -> abs(f(x)) = sqrt(g(x))");

                    RPN.Node abs = new RPN.Node(new[] { node[1, 1] }, new RPN.Token("abs", 1, RPN.Type.Function));
                    RPN.Node sqrt = new RPN.Node(new[] { node[0] }, new RPN.Token("sqrt", 1, RPN.Type.Function));

                    node.Replace(node[1], abs);
                    node.Replace(node[0], sqrt);
                }

                //TODO: ln(f(x)) = g(x) -> e^ln(f(x)) = e^g(x) -> f(x) = e^g(x)
            }
        }


        /// <summary>
        /// converts a vardiac function into a simpler AST
        /// </summary>
        /// <param name="node"></param>
        private void explode(RPN.Node node)
        {
            RPN.Token add = new RPN.Token("+", 2, RPN.Type.Operator);
            RPN.Token division = new RPN.Token("/", 2, RPN.Type.Operator);
            RPN.Token multiplication = new RPN.Token("*", 2, RPN.Type.Operator);

            //convert a sum to a series of additions
            if (node.IsFunction("internal_sum") || node.IsFunction("sum"))
            {
                if (node.Children.Count == 2)
                {
                    node.Replace(new RPN.Token("+", 2, RPN.Type.Operator));
                    node.Children.Reverse();
                }
                else
                {
                    Assign(node, gen(add));
                }
            }
            else if (node.IsFunction("internal_product"))
            {
                if (node.Children.Count == 2)
                {
                    node.Replace(new RPN.Token("*", 2, RPN.Type.Operator));
                    node.Children.Reverse();
                }
                else
                {
                    Assign(node, gen(multiplication));
                }
            }
            //convert an avg to a series of additions and a division
            else if (node.IsFunction("avg"))
            {
                if (node.Children.Count == 1)
                {
                    node.Remove();
                    return;
                }

                List<RPN.Token> results = new List<RPN.Token>(node.Children.Count);

                for (int i = 0; i < node.Children.Count; i++)
                {
                    results.AddRange(node.Children[i].ToPostFix());
                }

                results.Add(new RPN.Token("sum", node.Children.Count, RPN.Type.Function));
                results.Add(new RPN.Token(node.Children.Count));
                results.Add(division);
                RPN.Node temp = Generate(results.ToArray());
                explode(temp.Children[1]);
                Assign(node, temp);
            }

            RPN.Node gen(RPN.Token token)
            {
                if (node.Children.Count == 1)
                {
                    node.Remove();
                    return null;
                }

                //TODO: Convert this from using ToPostFix to automatically generating a new correct tree!

                //Prep stage
                Queue<RPN.Node> additions = new Queue<RPN.Node>(node.Children.Count);
                additions.Enqueue(new RPN.Node(new[] { node[0], node[1] }, token));
                for (int i = 2; i + 1 < node.Children.Count; i += 2)
                {
                    additions.Enqueue(new RPN.Node(new[] { node[i], node[i + 1] }, token));
                }

                if (node.Children.Count % 2 == 1 && node.Children.Count > 2)
                {
                    additions.Enqueue(node.Children[node.Children.Count - 1]);
                }

                while (additions.Count > 1)
                {

                    RPN.Node[] temp = new[] { additions.Dequeue(), additions.Dequeue() };
                    temp.Reverse();
                    additions.Enqueue(new RPN.Node(temp, token));
                }

                //This should nearly always result in a return 
                if (additions.Count == 1)
                {
                    additions.Peek().Children.Reverse();
                    Swap(additions.Peek());
                    return additions.Dequeue();
                }

                //This is fall back code! 
                List<RPN.Token> results = new List<RPN.Token>(node.Children.Count);
                results.AddRange(node.Children[0].ToPostFix());
                results.AddRange(node.Children[1].ToPostFix());
                results.Add(token);

                for (int i = 2; i < node.Children.Count; i += 2)
                {
                    results.AddRange(node.Children[i].ToPostFix());
                    if ((i + 1) < node.Children.Count)
                    {
                        results.AddRange(node.Children[i + 1].ToPostFix());
                        results.Add(token);
                    }
                    results.Add(token);
                }

                return Generate(results.ToArray());

            }
        }


        /// <summary>
        /// Converts a series of multiplications, additions, or subtractions 
        /// into a new node to see if there are additional simplifications that can be made
        /// </summary>
        /// <param name="node"></param>
        private void expand(RPN.Node node)
        {
            //TODO:
            Queue<RPN.Node> unvisited = new Queue<RPN.Node>();
            unvisited.Enqueue(node);

            while (unvisited.Count > 0)
            {
                node = unvisited.Dequeue();

                //Propagate
                for (int i = 0; i < node.Children.Count; i++)
                {
                    unvisited.Enqueue(node.Children[i]);
                }

                if (node.IsAddition())
                {
                    if (node.isRoot || !node.Parent.IsFunction("internal_sum"))
                    {
                        //This prevents a stupid allocation and expansion and compression cycle
                        if (node[0].IsAddition() || node[1].IsAddition())
                        {
                            RPN.Node sum = new RPN.Node(node.Children.ToArray(),
                                new RPN.Token("internal_sum", node.Children.Count, RPN.Type.Function));
                            Assign(node, sum);
                        }
                    }
                    else if (node.Parent.IsFunction("internal_sum"))
                    {
                        node.Parent.RemoveChild(node);
                        node.Parent.AddChild(node.Children[0]);
                        node.Parent.AddChild(node.Children[1]);
                    }
                }
                else if (node.IsMultiplication())
                {
                    if (node.isRoot || !node.Parent.IsFunction("internal_product"))
                    {
                        //This prevents a stupid allocation and expansion and compression cycle
                        if (node[0].IsMultiplication() || node[1].IsMultiplication())
                        {
                            RPN.Node product = new RPN.Node(node.Children.ToArray(),
                                new RPN.Token("internal_product", node.Children.Count, RPN.Type.Function));
                            Assign(node, product);
                        }
                    }
                    else if (node.Parent.IsFunction("internal_product"))
                    {
                        node.Parent.RemoveChild(node);
                        node.Parent.AddChild(node.Children[0]);
                        node.Parent.AddChild(node.Children[1]);
                    }
                }
                //Convert a subtraction into an addition with multiplication by negative one ????
                //We would also need to add a corelating thing in the simplify method
            }
        }

        private void compress(RPN.Node node)
        {
            Queue<RPN.Node> unvisited = new Queue<RPN.Node>();
            unvisited.Enqueue(node);

            while (unvisited.Count > 0)
            {
                node = unvisited.Dequeue();
                for (int i = 0; i < node.Children.Count; i++)
                {
                    unvisited.Enqueue(node.Children[i]);
                }

                if (node.IsFunction("internal_sum") || node.IsFunction("internal_product"))
                {
                    explode(node);
                }
            }
        }

        private void GenerateDerivativeAndReplace(RPN.Node child)
        {
            RPN.Node temp = new RPN.Node(new[] { Clone(child) }, _derive);
            temp.Parent = child.Parent;

            child.Parent?.Replace(child, temp);

            child.Parent = temp;
        }

        private RPN.Node Clone(RPN.Node node)
        {
            return node.Clone();
        }

        /// <summary>
        /// This is the preferred method of setting the Root
        /// when you are simplifying and not creating a NaN root. 
        /// </summary>
        /// <param name="node"></param>
        private void SetRoot(RPN.Node node)
        {
            node.Parent = null;
            Root = node;
        }

        private void Assign(RPN.Node node, RPN.Node assign)
        {
            try
            {
                if (node is null || assign is null)
                {
                    return;
                }

                if (node.isRoot)
                {
                    SetRoot(assign);
                    return;
                }

                node.Parent.Replace(node, assign);
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new Exception($"node: {node.ToInfix()}, assign: {assign.ToInfix()}", ex);
            }
        }

        private void Write(string message)
        {
            Logger?.Invoke(this, message);
        }

        private void stdout(string message)
        {
            Output?.Invoke(this, message);
        }
    }


    public struct OptimizationTracker
    {
        public string Hash;
        public int count;
    }

    public class OptimizerRuleSet
    {
        public static Dictionary<AST.SimplificationMode, List<Rule>> ruleSet { get; private set; }
        public static Dictionary<AST.SimplificationMode, Rule> setRule { get; private set; }

        private static HashSet<Rule> contains;
        public event EventHandler<string> Logger;
        
        public OptimizerRuleSet()
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
            ruleSet.Add(mode, new List<Rule> {rule});
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
            int length = rules.Count;

            for (int i = 0; i < length; i++)
            {
                Rule rule = rules[i];
                if (rule.CanExecute(node))
                {
                    return rule.Execute(node);
                }
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

        private void Write(object obj, string message)
        {
            Write(message);
        }

        private void Write(string message)
        {
            Logger?.Invoke(this, message);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var KV in ruleSet)
            {
                sb.Append($"{KV.Key} has {KV.Value.Count} rules");
                if (setRule.ContainsKey(KV.Key))
                {
                    sb.Append(" and has a set rule");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

}