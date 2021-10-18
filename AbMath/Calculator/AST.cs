using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AbMath.Calculator.Functions;
using AbMath.Calculator.Operators;
using AbMath.Calculator.Simplifications;
using AbMath.Utilities;

namespace AbMath.Calculator
{
    public class AST
    {
        public RPN.Node Root { get; private set; }

        /// <summary>
        ///  Sqrt -
        ///  Log -
        ///  Division -
        ///  Exponent -
        ///  Subtraction -
        ///  Addition -
        ///  Multiplication -
        ///  Swap - 
        ///  Trig - All other trig conversions. [Currently encomposes all the below trig rules]
        ///  Trig Half Angle - Converts fractions to trig functions when appropriate
        ///  Trig Half Angle Expansion - Converts trig functions to fractions
        ///  Power Reduction -  Converts trig functions to fractions
        ///  Power Expansion - Converts fractions to trig functions
        ///  Double Angle - Converts double angles to trig functions
        ///  Double Angle Reduction - Converts trig functions to double angles
        ///  Sum - deals with all sum functions etc. 
        ///  Constants - 
        /// </summary>
        public enum SimplificationMode
        {
            Sqrt, Log, Division, Exponent, Subtraction, Addition, Multiplication, Swap,
            Trig, TrigHalfAngle, TrigHalfAngleExpansion,
            TrigPowerReduction, TrigPowerExpansion,
            TrigDoubleAngle, TrigDoubleAngleReduction,
            Sum,
            Integral,
            Constants,
            Misc,
            List,
            Compress, COUNT
        }

        private RPN _rpn;
        private RPN.DataStore _data;

        private bool debug => _data.DebugMode;

        private readonly RPN.Token _derive = new RPN.Token("derive", 1, RPN.Type.Function);
        private readonly RPN.Token _sum = new RPN.Token("sum", 5, RPN.Type.Function);

        public event EventHandler<string> Logger;
        public event EventHandler<string> Output;

        private OptimizerRuleEngine ruleManager;

        private Logger logger;

        public AST(RPN rpn)
        {
            _rpn = rpn;
            _data = rpn.Data;
            logger = _data.Logger;
            RPN.Node.ResetCounter();
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

            ruleManager = OptimizerRuleEngineFactory.generate(logger, debug);
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
                    Write($"Pass:{pass} {Root.ToInfix(_data)}.");
                }

                Simplify(Root);
                pass++;
            }
            sw.Stop();
            _data.AddTimeRecord("AST Simplify", sw);

            if (debug)
            {
                Write("Before being normalized the tree looks like:");
                Write(Root.ToInfix(_data));
                Write(Root.Print());
            }

            Normalize(); //This distorts the tree :(

            Write("");
            Write(ruleManager.ToString());

            return this;
        }

        private void Normalize()
        {
            //This should in theory normalize the tree
            //so that exponents etc come first etc
            Stopwatch sw = new Stopwatch();
            sw.Start();

            _rpn.Data.AddFunction("internal_product", new Function());
            _rpn.Data.AddFunction("internal_sum", new Function());

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
                Write(Root.ToInfix(_data));       
            #endif
            //We want to reduce this! 
            Simplify(node, SimplificationMode.Sqrt);
            Simplify(node, SimplificationMode.Log);
            Simplify(node, SimplificationMode.List);

            Simplify(node, SimplificationMode.Division);

            Simplify(node, SimplificationMode.Exponent); //This will make all negative exponennts into divisions
            Simplify(node, SimplificationMode.Subtraction);
            Simplify(node, SimplificationMode.Addition);
            Simplify(node, SimplificationMode.Trig);
            Simplify(node, SimplificationMode.Multiplication);

            Simplify(node, SimplificationMode.Swap);
            Simplify(node, SimplificationMode.Misc);
            Simplify(node, SimplificationMode.Sum);
            Simplify(node, SimplificationMode.Integral);

            Swap(node);
            #if DEBUG
                Write(Root.ToInfix(_data));
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
                if (ruleManager.CanRunSet(mode, node))
                {
                    RPN.Node assignment = ruleManager.Execute(mode, node);
                    if (assignment != null)
                    {
                        if (assignment.IsNumber() && assignment.Token.Value == "NaN")
                        {
                            SetRoot(assignment);
                        }
                        else
                        {
                            Assign(node, assignment);
                        }
                    }
                }

                if (mode == SimplificationMode.Swap)
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
                //Typically does not get invoked...
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
                    else if (node.IsOperator("!"))
                    {
                        if (node[0].IsNumber(0) || node[0].IsNumber(1))
                        {
                            Solve(node);
                        }
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
                    node.Children.Sort();
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

                if (node.IsFunction("internal_sum") || node.IsFunction("total"))
                {
                    node.Children.Sort();
                    InternalSimplification(node, new RPN.Token("*", 2, RPN.Type.Operator), new RPN.Node(0));

                    Write($"After Auto Sort: {node.ToInfix(_data)}");
                }
                else if (node.IsFunction("internal_product") || node.IsFunction("product"))
                {
                    node.Children.Reverse();

                    //Simplification
                    InternalSimplification(node, new RPN.Token("^",2, RPN.Type.Operator), new RPN.Node(1) );
                    //Sort order for multiplication
                    //1) Numbers or Constants
                    //2) Exponents of constants
                    //3) Exponents of variables
                    //4) Variables
                    //5) Functions (sorted alphabetically)
                    //6) Expressions (Everything else)

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
                        }
                    }



                    Write(node.Print());
                }
                else if (node.IsOperator("/") && node[0].IsFunction("internal_product") && node[1].IsFunction("internal_product"))
                {
                    List<RPN.Node> denominator = node[0].Children;
                    List<RPN.Node> numerator = node[1].Children;


                    for (int i = 0; i < numerator.Count; i++)
                    {
                        RPN.Node top = node[1, i];
                        for (int y = 0; y < denominator.Count; y++)
                        {
                            RPN.Node bottom = node[0, y];

                            //We can now compare the top and bottom

                            //Factorials:
                            if (top.IsOperator("!") && bottom.IsOperator("!") && top[0].IsInteger() && bottom[0].IsInteger())
                            {
                                //Identical Factorial Cancel
                                if (top[0].GetNumber() == bottom[0].GetNumber())
                                {
                                    Write("\tIdentical Factorial Cancel");
                                    RPN.Node replacement = new RPN.Node(1);

                                    node[0].Replace(top, replacement);
                                    node[1].Replace(bottom, new RPN.Node(1));

                                    top = replacement;
                                }
                                //Radial Factorial Cancelation
                                else if (Math.Abs(top[0].GetNumber() - bottom[0].GetNumber()) == 1)
                                {
                                    Write("\tRadial Factorial Cancel");
                                    if (top[0].GetNumber() > bottom[0].GetNumber())
                                    {
                                        Write("\t\t (a + 1)!/a! - > (a + 1)");

                                        node[1, i] = top[0];
                                        top = node[1, i];

                                        node[0, y] = new RPN.Node(1);
                                        bottom = node[0, y];

                                    }
                                    else
                                    {
                                        Write("\t\t a!/(a + 1)! - > 1/(a + 1)");
                                        RPN.Node replacement = bottom[0];
                                        node[0].Replace(top, new RPN.Node(1));
                                        node[1].Replace(bottom, replacement);
                                        top = replacement;
                                    }
                                }
                            }
                            //Power Reduction Rule
                        }
                    }

                    //The numerator is zero 
                    //and the denominator is either constant factorials or otherwise a number or constant.
                    if (numerator.Count == 1 && numerator[0].IsNumber(0) &&
                        denominator.All(n => (n.IsOperator("!") && n[0].IsInteger()) || n.IsNumberOrConstant()))
                    {
                        node[1] = new RPN.Node(1);
                    }


                }
                else if (node.IsOperator("/") && node[0].IsFunction("internal_product") && node[1].IsNumber(0))
                {
                    
                    List<RPN.Node> denominator = node[0].Children;

                    Write("\t0/(c_0 * c_1 * ... * c_n) where c is constant -> 0/1 -> 0");

                    Write(node.ToInfix());
                    if (denominator.TrueForAll(n => (n.IsOperator("!") && n[0].IsInteger()) || n.IsNumber()) ) 
                    {
                        node[0] = new RPN.Node(1);
                    }
                }
            }
        }

        void InternalSimplification(RPN.Node node, RPN.Token Operator, RPN.Node replacement)
        {
            Dictionary<string, List<RPN.Node>> hashDictionary = new Dictionary<string, List<RPN.Node>>();
            hashDictionary.Clear();
            string hash = string.Empty;
            //This tracks everything
            for (int i = 0; i < node.Children.Count; i++)
            {
                hash = node.Children[i].GetHash();
                if (!hashDictionary.ContainsKey(hash))
                {
                    List<RPN.Node> temp = new List<RPN.Node>();
                    temp.Add(node.Children[i]);
                    hashDictionary.Add(hash, temp);
                }
                else
                {
                    hashDictionary[hash].Add(node.Children[i]);
                }
            }

            //This simplifies everything
            foreach (var kv in hashDictionary)
            {
                if (kv.Value.Count > 1)
                {
                    Write("\t" + kv.Key + " with a count of " + kv.Value.Count + " and infix of " + kv.Value[0].ToInfix(_data));

                    RPN.Node exponent;
                    if (Operator.IsExponent())
                    {
                        exponent = new RPN.Node(new[] {new RPN.Node(kv.Value.Count), kv.Value[0]}, Operator);
                    }
                    else
                    {
                        exponent = new RPN.Node(new[] { kv.Value[0], new RPN.Node(kv.Value.Count) }, Operator);
                    }

                    foreach (var nv in kv.Value)
                    {
                        Write($"\t\t Replacing {nv.ID} with {replacement.ToInfix(_data)}");
                        node.Replace(nv, replacement.Clone());
                    }

                    node.AddChild(exponent);
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
                _rpn.Data.AddFunction("derive", new Function { Arguments = 1, MaxArguments = 1, MinArguments = 1 });
            }
            MetaFunctions(Root);

            if (_rpn.Data.Functions.ContainsKey("derive"))
            {
                _rpn.Data.RemoveFunction("derive");
            }

            SW.Stop();

            _data.AddTimeRecord("AST MetaFunctions", SW);


            Simplify();

            return this;
        }

        private bool MetaFunctions(RPN.Node node)
        {
            //Propagate down the tree
            bool fooBar = false;
            for (int i = 0; i < node.Children.Count; i++)
            {
                MetaFunctions(node.Children[i]);
            }

            if (!node.IsFunction() || !_data.MetaFunctions.Contains(node.Token.Value))
            {
                return false;
            }

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
                Write($"{node[1].ToInfix(_data)} {node[2].ToInfix(_data)} {node[0].ToInfix(_data)}");
                node.RemoveChild(temp);

                Write(temp.ToInfix(_data));
                Assign(node, temp);
            }
            else if (node.IsFunction("sum"))
            {
                //0 - end
                //1 - start 
                //2 - variable 
                //3 - expression
                Write($"\tSolving the sum! : {node[3].ToInfix(_data)}");
                PostFix math = new PostFix(_rpn);
                double start = math.Compute(node[1].ToPostFix().ToArray());
                double end = math.Compute(node[0].ToPostFix().ToArray());
                double DeltaX = end - start;
                int max = (int)Math.Ceiling(DeltaX);

                double PrevAnswer = 0;

                math.SetPolish(node[3].ToPostFix().ToArray());
                double sum = 0;

                for (int x = 0; x <= max; x++)
                {
                    double RealX = start + x;
                    math.SetVariable("ans", PrevAnswer);
                    math.SetVariable(node[2].Token.Value, RealX);
                    double answer = math.Compute();
                    PrevAnswer = answer;
                    sum += answer;
                    math.Reset();
                }
                Assign(node, new RPN.Node(sum));
            }

            return true;
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

            Write($"Starting to derive ROOT: {Root.ToInfix(_data)}");
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
            //is more likely to fail compared to an iterative approach.

            Stack<RPN.Node> stack = new Stack<RPN.Node>();
            stack.Push(foo);
            //Write(foo.ToInfix(_data));
            string v = variable.ToInfix(_data);
            RPN.Node node = null;
            while (stack.Count > 0)
            {
                node = stack.Pop();
                //Write($"Current Node: {node.ToInfix(_data)}");
                //Propagate down the tree
                for (int i = (node.Children.Count - 1); i >= 0; i--)
                {
                    stack.Push(node.Children[i]);
                }

                if (node.Token.Value != "derive")
                {
                    continue;
                }

                if (node.Children[0].IsAddition() || node.Children[0].IsSubtraction())
                {
                    if (debug)
                    {
                        string f_x = node.Children[0].Children[0].ToInfix(_data);
                        string g_x = node.Children[0].Children[1].ToInfix(_data);
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
                        Write($"\td/d{v}[ {node.Children[0].ToInfix(_data)} ] -> 0");
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
                        Write($"\td/d{v}[ {node.Children[0].ToInfix(_data)} ] -> 1");
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
                                $"\td/d{v}[ {node.Children[0].Children[0].ToInfix(_data)} * {node.Children[0].Children[1].ToInfix(_data)} ] -> 0");
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
                                $"\td/d{v}[ {node.Children[0].Children[1].ToInfix(_data)} * {node.Children[0].Children[0].ToInfix(_data)}] -> d/d{v}[ {node.Children[0].Children[1].ToInfix(_data)} ] * {node.Children[0].Children[0].ToInfix(_data)}");
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
                            string constant = node.Children[0].Children[1].ToInfix(_data);
                            string expr = node.Children[0].Children[0].ToInfix(_data);
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
                        RPN.Node fNode = node.Children[0].Children[0];
                        RPN.Node gNode = node.Children[0].Children[1];

                        if (debug)
                        {
                            string f = fNode.ToInfix(_data);
                            string g = gNode.ToInfix(_data);
                            Write($"\td/d{v}[ {f} * {g} ] -> {f} * d/d{v}[ {g} ] + d/d{v}[ {f} ] * {g}");
                        }
                        else
                        {
                            Write($"\td/dx[ f(x) * g(x) ] -> f(x) * d/dx[ g(x) ] + d/dx[ f(x) ] * g(x)");
                        }

                        RPN.Node fDerivative = new Derive(fNode.Clone());
                        RPN.Node gDerivative = new Derive(gNode.Clone());
                        RPN.Node add = new Add(new Mul(gNode, fDerivative), new Mul(fNode, gDerivative));

                        //Remove myself from the tree
                        node.Remove(add);

                        //Explicit recursion
                        stack.Push(fDerivative);
                        stack.Push(gDerivative);
                    }
                }
                else if (node[0].IsDivision() && node[0, 0].IsNumberOrConstant())
                {
                    if (debug)
                    {
                        string f_x = node[0, 1].ToInfix(_data);
                        string k = node[0, 0].ToInfix(_data);
                        Write($"\td/d{v}[ {f_x}/{k} ] -> d/d{v}{f_x}]/{k}");
                    }
                    else
                    {
                        Write("\td/dx[ f(x)/k ] -> d/dx[f(x)]/k");
                    }
                    GenerateDerivativeAndReplace(node[0,1]);
                    stack.Push(node[0, 1]);
                    node.Remove();
                }
                else if (node.Children[0].IsDivision())
                {
                    //Quotient Rule
                    RPN.Node numerator = node.Children[0].Children[1];
                    RPN.Node denominator = node.Children[0].Children[0];

                    RPN.Node numeratorDerivative = new RPN.Node(new[] { Clone(numerator) }, _derive);
                    RPN.Node denominatorDerivative = new RPN.Node(new[] { Clone(denominator) }, _derive);

                    RPN.Node multiplicationOne = new Mul(denominator, numeratorDerivative);
                    RPN.Node multiplicationTwo = new Mul(numerator, denominatorDerivative);
                    RPN.Node subtraction = new Sub(multiplicationOne, multiplicationTwo);
                    RPN.Node denominatorSquared = new Pow(denominator.Clone(), new RPN.Node(2));

                    if (debug)
                    {
                        string n = numerator.ToInfix(_data);
                        string d = denominator.ToInfix(_data);
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
                            string b = baseNode.ToInfix(_data);
                            string p = power.ToInfix(_data);
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
                            //x^(n - 1) 
                            exponent = new Pow(baseNode, new Sub(powerClone, one));
                        }
                        else
                        {
                            exponent = new Pow(baseNode, new RPN.Node(powerClone.GetNumber() - 1));
                        }

                        RPN.Node multiplication = new Mul(power, exponent);

                        node.Replace(node.Children[0], multiplication);

                        //Delete self from the tree
                        node.Remove();
                    }
                    else if ((baseNode.IsFunction() || baseNode.IsExpression()) && power.IsNumberOrConstant())
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix(_data);
                            string p = power.ToInfix(_data);
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
                            subtraction = new Sub(powerClone, new RPN.Node(1));
                        }
                        else
                        {
                            subtraction = new RPN.Node(power.GetNumber() - 1);
                        }

                        //Replace n with (n - 1) 
                        RPN.Node exponent = new Pow(baseNode, subtraction);
                        RPN.Node multiply = new Mul(new Mul(power, exponent), bodyDerive);

                        node.Replace(node.Children[0], multiply);

                        //Delete self from the tree
                        node.Remove();
                        stack.Push(bodyDerive);
                    }
                    else if (baseNode.IsConstant("e"))
                    {
                        if (debug)
                        {
                            string p = power.ToInfix(_data);
                            Write($"\td/d{v}[ e^{p} ] -> d/d{v}[ {p} ] * e^{p}");
                        }
                        else
                        {
                            Write("\td/dx[ e^g(x) ] -> d/dx[ g(x) ] * e^g(x)");
                        }

                        RPN.Node exponent = baseNode.Parent;
                        RPN.Node powerDerivative = new RPN.Node(new[] { Clone(power) }, _derive);
                        RPN.Node multiply = new Mul(exponent, powerDerivative);
                        node.Replace(power.Parent, multiply);
                        //Delete self from the tree
                        node.Remove();
                        stack.Push(powerDerivative);
                    }
                    else if (baseNode.IsNumberOrConstant() && (power.IsExpression() || power.IsVariable()))
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix(_data);
                            string p = power.ToInfix(_data);
                            Write($"\td/d{v}[ {b}^{p} ] -> ln({b}) * {b}^{p} * d/d{v}[ {p} ]");
                        }
                        else
                        {
                            Write($"\td/dx[ b^g(x) ] -> ln(b) * b^g(x) * d/dx[ g(x) ]");
                        }

                        RPN.Node exponent = baseNode.Parent;
                        RPN.Node ln = new RPN.Node(new[] { Clone(baseNode) }, new RPN.Token("ln", 1, RPN.Type.Function));
                        RPN.Node powerDerivative = new RPN.Node(new[] { Clone(power) }, _derive);
                        RPN.Node multiply = new Mul(powerDerivative, new Mul(ln, exponent));
                            

                        node.Replace(power.Parent, multiply);
                        //Delete self from the tree
                        node.Remove();
                        stack.Push(powerDerivative);
                    }
                    else
                    {
                        if (debug)
                        {
                            string b = baseNode.ToInfix(_data);
                            string p = power.ToInfix(_data);
                            Write($"\td/d{v}[ {b}^{p} ] -> {b}^{p} * d/d{v}[ {b} * ln( {p} ) ]");
                        }
                        else
                        {
                            Write("\td/dx[ f(x)^g(x) ] -> f(x)^g(x) * d/dx[ g(x) * ln( f(x) ) ]");
                        }

                        RPN.Node exponent = Clone(baseNode.Parent);
                        RPN.Node ln = new RPN.Node(new[] { Clone(baseNode) }, new RPN.Token("ln", 1, RPN.Type.Function));
                        RPN.Node temp = new Mul(ln, power.Clone());
                        RPN.Node derive = new RPN.Node(new[] { temp }, _derive);
                        RPN.Node multiply = new Mul(derive, exponent);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ sin({expr}) ] -> cos({expr}) * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ sin(g(x)) ] -> cos(g(x)) * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node cos = new RPN.Node(new[] { body }, new RPN.Token("cos", 1, RPN.Type.Function));
                    RPN.Node multiply = new Mul(bodyDerive, cos);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ cos({expr}) ] -> -sin({expr}) * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ cos(g(x)) ] -> -sin(g(x)) * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node sin = new RPN.Node(new[] { body }, new RPN.Token("sin", 1, RPN.Type.Function));
                    RPN.Node negativeOneMultiply = new Mul(sin, new RPN.Node(-1));
                    RPN.Node multiply = new Mul(bodyDerive, negativeOneMultiply);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ tan({expr}) ] -> sec({expr})^2 * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ tan(g(x)) ] -> sec(g(x))^2 * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node sec = new RPN.Node(new[] { body }, new RPN.Token("sec", 1, RPN.Type.Function));
                    RPN.Node exponent = new Pow(sec, new RPN.Node(2));
                    RPN.Node multiply = new Mul(bodyDerive, exponent);
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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ sec({expr}) ] -> tan({expr}) * sec({expr}) * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ sec(g(x)) ] -> tan(g(x)) * sec(g(x)) * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node sec = node.Children[0];
                    RPN.Node tan = new RPN.Node(new[] { Clone(body) }, new RPN.Token("tan", 1, RPN.Type.Function));
                    RPN.Node multiply = new Mul(new Mul(tan, sec) , bodyDerive);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ csc({expr}) ] -> - cot({expr}) * csc({expr}) * d/d{v}[ {expr} ] ");
                    }
                    else
                    {
                        Write("\td/dx[ csc(g(x)) ] -> - cot(g(x)) * csc(g(x)) * d/dx[ g(x) ] ");
                    }
                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node csc = node.Children[0];
                    RPN.Node cot = new RPN.Node(new[] { Clone(body) }, new RPN.Token("cot", 1, RPN.Type.Function));
                    RPN.Node multiply = new Mul(bodyDerive, new Mul(cot, csc));

                    node.Replace(node.Children[0], new Mul(multiply, new RPN.Node(-1)));
                    //Delete self from the tree
                    node.Remove();
                    //Chain Rule
                    stack.Push(bodyDerive);
                }
                else if (node.Children[0].IsFunction("cot"))
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ cot({expr}) ] -> -csc({expr})^2 * d/d{v}[ {expr} ]");
                    }
                    else
                    {
                        Write("\td/dx[ cot(g(x)) ] -> -csc(g(x))^2 * d/dx[ g(x) ]");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node csc = new RPN.Node(new[] { body }, new RPN.Token("csc", 1, RPN.Type.Function));
                    RPN.Node exponent = new Pow(csc, new RPN.Node(2));
                    RPN.Node temp = new Mul(exponent, new RPN.Node(-1));
                    RPN.Node multiply = new Mul(temp, bodyDerive);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ arcsin({expr}) ] -> d/d{v}[ {expr} ]/sqrt(1 - {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arcsin(g(x)) ] -> d/dx[ g(x) ]/sqrt(1 - g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new Pow(body, new RPN.Node(2));
                    RPN.Node subtraction = new Sub(new RPN.Node(1), exponent);
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node division = new Div(bodyDerive, sqrt);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ arccos({expr}) ] -> -1 * d/d{v}[ {expr} ]/sqrt(1 - {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arccos(g(x)) ] -> -1 * d/dx[ g(x) ]/sqrt(1 - g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new Pow(body, new RPN.Node(2));
                    RPN.Node subtraction = new Sub(new RPN.Node(1), exponent);
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node division = new Div(bodyDerive, sqrt);
                    RPN.Node multiplication = new Mul(division, new RPN.Node(-1));

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ arctan({expr}) ] -> d/d{v}[ {expr} ]/(1 + {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arctan(g(x)) ] -> d/dx[ g(x) ]/(1 + g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new Pow(body, new RPN.Node(2));
                    RPN.Node add = new Add(exponent, new RPN.Node(1));
                    RPN.Node division = new Div(bodyDerive, add);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ arccot({expr}) ] -> -1 * d/d{v}[ {expr} ]/(1 + {expr}^2)");
                    }
                    else
                    {
                        Write("\td/dx[ arccot(g(x)) ] -> -1 * d/dx[ g(x) ]/(1 + g(x)^2)");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new Pow(body, new RPN.Node(2));
                    RPN.Node add = new Add(exponent, new RPN.Node(1));
                    RPN.Node multiplication = new Mul(bodyDerive, new RPN.Node(-1));
                    RPN.Node division = new Div(multiplication, add);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ arcsec({expr}) ] -> d/d{v}[ {expr} ]/( abs({expr}) * sqrt({expr}^2 - 1 ) )");
                    }
                    else
                    {
                        Write("\td/dx[ arcsec(g(x)) ] -> d/dx[ g(x) ]/( abs(g(x)) * sqrt(g(x)^2 - 1 ) )");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new Pow(body, new RPN.Node(2));
                    RPN.Node subtraction = new Sub(exponent, new RPN.Node(1));
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node abs = new RPN.Node(new[] { body.Clone() }, new RPN.Token("abs", 1, RPN.Type.Function));
                    RPN.Node denominator = new Mul(abs, sqrt);

                    RPN.Node division = new Div(bodyDerive, denominator);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ arccsc({expr}) ] -> -1 * d/d{v}[ {expr} ]/( abs({expr}) * sqrt({expr}^2 - 1 ) )");
                    }
                    else
                    {
                        Write("\td/dx[ arccsc(g(x)) ] -> -1 * d/dx[ g(x) ]/( abs(g(x)) * sqrt(g(x)^2 - 1 ) )");
                    }

                    RPN.Node body = Clone(node.Children[0].Children[0]);
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);

                    RPN.Node exponent = new Pow(body, new RPN.Node(2));
                    RPN.Node subtraction = new Sub(exponent, new RPN.Node(1));
                    RPN.Node sqrt = new RPN.Node(new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    RPN.Node abs = new RPN.Node(new[] { body.Clone() }, new RPN.Token("abs", 1, RPN.Type.Function));
                    RPN.Node denominator = new Mul(abs, sqrt);
                    RPN.Node multiplication = new Mul(bodyDerive, new RPN.Node(-1));
                    RPN.Node division = new Div(multiplication, denominator);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\tsqrt({expr}) -> {expr}^0.5");
                    }
                    else
                    {
                        Write("\tsqrt(g(x)) -> g(x)^0.5");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node OneHalf = new RPN.Node(0.5);
                    RPN.Node exponent = new Pow(body, OneHalf); 
                    node.Replace(node.Children[0], exponent);
                    stack.Push(node);
                }
                else if (node.Children[0].IsLn())
                {
                    if (debug)
                    {
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\td/d{v}[ ln({expr}) ] -> d/d{v}[ {expr} ]/{expr}");
                    }
                    else
                    {
                        Write("\td/dx[ ln(g(x)) ] -> d/dx[ g(x) ]/g(x)");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node division = new Div(bodyDerive, body);

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
                        string b = body.ToInfix(_data);
                        string p = power.ToInfix(_data);
                        Write($"\td/d{v}[ log({b},{p}) ] -> d/d{v}[ {p} ]/({p} * ln({b}))");
                    }
                    else
                    {
                        Write("\td/dx[ log(b,g(x)) ] -> d/dx[ g(x) ]/(g(x) * ln(b))");
                    }

                    RPN.Node bodyDerive = new RPN.Node(new[] { Clone(body) }, _derive);
                    RPN.Node multiply = 
                        new RPN.Node(new[] { body, new RPN.Node(new[] { power }, ln) },
                        new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node division = new Div(bodyDerive, multiply);

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
                        string expr = node.Children[0].Children[0].ToInfix(_data);
                        Write($"\tabs({expr}) -> sqrt( {expr}^2 )");
                    }
                    else
                    {
                        Write("\tabs(g(x)) -> sqrt( g(x)^2 )");
                    }

                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node exponent = new Pow(body, new RPN.Node(2));
                    RPN.Node sqrt = new RPN.Node(new[] { exponent }, new RPN.Token("sqrt", 1, RPN.Type.Function));

                    node.Replace(node.Children[0], sqrt);
                    stack.Push(node);
                }
                else if (node.Children[0].IsFunction("total"))
                {
                    Write("\tExploding total");
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
                        $"Derivative of {node.Children[0].ToInfix(_data)} not known at this time.");
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
            if (node.IsFunction("internal_sum") || node.IsFunction("total"))
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

                results.Add(new RPN.Token("total", node.Children.Count, RPN.Type.Function));
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
                if (token.IsAddition())
                {
                    node.Children.Reverse();
                }

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
        /// into a new node to see if there are additional simplifications that can be made.
        /// (IE converts f(x) + g(x) + h(x) -> internal_sum(f(x),g(x),h(x)) )
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
                    //Right now an internal_product is created when:
                    //1) The current is a root multiplication or does not have the parent internal product and either of its children is a multiplication
                    //2) The current node is a leaf multiplication (therefore eligible for internal_product) and the parent node is a division
                    //3) The current node is a leaf multiplication and its parent is an internal product.
                    if (node.isRoot || !node.Parent.IsFunction("internal_product") )
                    {
                        //This prevents a stupid allocation and expansion and compression cycle
                        
                        if (node[0].IsMultiplication() || node[1].IsMultiplication() || (!node.isRoot && node.Parent.IsOperator("/")))
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

        /// <summary>
        /// Converts internal_sum and internal_product back into a series of
        /// multiplications and additions. 
        /// </summary>
        /// <param name="node"></param>
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
            RPN.Node temp = new RPN.Node(new[] { child.Clone() }, _derive);
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
                throw new Exception($"node: {node.ToInfix(_data)}, assign: {assign.ToInfix(_data)}", ex);
            }
        }

        private void Write(string message)
        {
            logger.Log(Channels.Debug, message);
        }

        private void stdout(string message)
        {
            logger.Log(Channels.Output, message);
        }
    }


    public struct OptimizationTracker
    {
        public string Hash;
        public int count;
    }

    

}