﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AbMath.Calculator
{
    public class AST
    {
        public RPN.Node Root { get; private set; }

        private enum SimplificationMode
        {
            Imaginary, Division, Subtraction, Addition, Multiplication, Swap, Exponent, Trig
        }

        private RPN _rpn;
        private RPN.DataStore _data;
        private Stack<RPN.Node> _stack;
        private int count = -1;

        private readonly RPN.Token _derive = new RPN.Token("derive", 1, RPN.Type.Function);

        public event EventHandler<string> Logger;
        public event EventHandler<string> Output;

        public AST(RPN rpn)
        {
            _rpn = rpn;
            _data = rpn.Data;
            _stack = new Stack<RPN.Node>(5);
        }

        public RPN.Node Generate(RPN.Token[] input)
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();

            //Convert all the PostFix information to Nodes[]
            RPN.Node[] nodes = new RPN.Node[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                nodes[i] = new RPN.Node()
                {
                    Children = new RPN.Node[0],
                    ID = GenerateNextID(),
                    Parent = null,
                    Token = input[i]
                };
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                switch (nodes[i].Token.Type)
                {
                    //When an operator or function is encountered 
                    case RPN.Type.Function:
                    case RPN.Type.Operator:
                        nodes[i].Children = new RPN.Node[nodes[i].Token.Arguments];
                        for (int j = 0; j < nodes[i].Token.Arguments; j++)
                        {
                            RPN.Node temp = _stack.Pop();
                            temp.Parent = nodes[i];
                            nodes[i].Children[j] =(temp);
                        }

                        _stack.Push(nodes[i]); //Push new tree into the stack 
                        break;
                    //When an operand is encountered push into stack
                    default:
                        _stack.Push(nodes[i]);
                        break;
                }
            }

            //This prevents the reassignment of the root node
            if (Root is null)
            {
                Root = _stack.Peek();
            }

            SW.Stop();
            _rpn.Data.AddTimeRecord("AST Generate", SW);
            return _stack.Pop();
        }

        /// <summary>
        /// Simplifies the current tree.
        /// </summary>
        /// <returns></returns>
        public AST Simplify()
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();

            int pass = 0;
            string hash = string.Empty;

            if (_data.DebugMode)
            {
                Write("");
            }

            while (hash != Root.GetHash())
            {

                hash = Root.GetHash();
                Simplify(Root);
                pass++;
                if (_data.DebugMode)
                {
                    Write($"Pass: {pass}");
                    Write($"{this.Root.Print()}");
                    Write($"{this.Root.ToInfix()}");
                    Write($"");
                }
            }

            if (_data.DebugMode)
            {
                Write("");
            }
            SW.Stop();
            _data.AddTimeRecord("AST Simplify", SW);
            return this;
        }

        private void Simplify(RPN.Node node)
        {
            Swap(node);
            Simplify(node, SimplificationMode.Imaginary);
            Simplify(node, SimplificationMode.Division);
            Simplify(node, SimplificationMode.Exponent);

            Simplify(node, SimplificationMode.Subtraction);
            Simplify(node, SimplificationMode.Addition);
            Simplify(node, SimplificationMode.Multiplication);
            Simplify(node, SimplificationMode.Swap);

            Simplify(node, SimplificationMode.Trig);
        }

        private void Simplify(RPN.Node node, SimplificationMode mode)
        {
            //If Root is a number abort. 
            if (Root.Token.IsNumber())
            {
                return;
            }

            //Imaginary
            if (mode == SimplificationMode.Imaginary && node.Token.Value == "sqrt")
            {
                //Any sqrt function with a negative number -> Imaginary number to the root node
                //An imaginary number propagates anyways
                if (node.Children[0].Token.IsNumber() && double.Parse(node.Children[0].Token.Value) < 0)
                {
                    Root = new RPN.Node(GenerateNextID(), double.NaN);
                    Write("\tSqrt Imaginary Number -> Root.");
                }
                //MAYBE: Any sqrt function with any non-positive number -> Cannot simplify further??
            }
            //Division
            else if (mode == SimplificationMode.Division && node.Token.IsDivision())
            {
                //if there are any divide by zero exceptions -> NaN to the root node
                //NaN propagate anyways
                if (node.Children[0].Token.Value == "0")
                {
                    Root = new RPN.Node(GenerateNextID(), double.NaN);
                    Write("\tDivision by zero -> Root");
                }
                //gcd if the leafs are both numbers since the values of the leafs themselves are changed
                //we don't have to worry about if the node is the root or not
                else if (node.Children[0].Token.IsNumber() && node.Children[1].Token.IsNumber())
                {
                    double num1 = double.Parse(node.Children[0].Token.Value);
                    double num2 = double.Parse(node.Children[1].Token.Value);
                    double gcd = RPN.DoFunctions.Gcd(new double[] { num1, num2 });

                    node.Children[0].Token.Value = (num1 / gcd).ToString();
                    node.Children[1].Token.Value = (num2 / gcd).ToString();
                    Write("\tDivision GCD.");
                }
                else if (node.Children[0].Token.IsVariable() && node.Children[1].Token.IsNumber())
                {
                    Write("Division -> Multiplication and exponentiation.");
                    RPN.Node negativeOne = new RPN.Node(GenerateNextID(), -1);
                    RPN.Node exponent = new RPN.Node()
                    {
                        Children = new RPN.Node[]{negativeOne, node.Children[0]},
                        ID = GenerateNextID(),
                        Parent = null,
                        Token = new RPN.Token
                        {
                            Arguments = 2,
                            Type = RPN.Type.Operator,
                            Value = "^"
                        }
                    };
                    negativeOne.Parent = exponent;

                    node.Token.Value = "*";
                    node.Replace(node.Children[0], exponent);
                }
            }
            //Subtraction
            else if (mode == SimplificationMode.Subtraction && node.Token.IsSubtraction())
            {
                //3sin(x) - 3sin(x)
                if ( node.ChildrenAreIdentical())
                {
                    if (node.isRoot)
                    {
                        Root = new RPN.Node(GenerateNextID(), 0);
                        Write("\tSimplification: Subtraction. Children are identical. Root.");
                    }
                    else if (!node.isRoot)
                    {
                        node.Parent.Replace(node.ID, new RPN.Node(GenerateNextID(), 0)
                        {
                            Parent = node.Parent,
                        });
                        Write("\tSimplification: Subtraction");
                    }
                }
                //3sin(x) - 2sin(x)
                else if (node.Children[0].Token.IsMultiplication() && node.Children[1].Token.IsMultiplication())
                {
                    if (node.Children[0].Children[0].GetHash() == node.Children[1].Children[0].GetHash() && node.Children[0].Children[1].Token.IsNumber() && node.Children[1].Children[1].Token.IsNumber())
                    {
                        Write("\tSimplification: Subtraction Dual Node");
                        double coefficient = double.Parse(node.Children[1].Children[1].Token.Value) -
                                             double.Parse(node.Children[0].Children[1].Token.Value);

                        node.Children[0].Children[1].Token.Value = "0";
                        node.Children[1].Children[1].Token.Value = coefficient.ToString();
                    }
                }
                //3sin(x) - sin(x)
                else if (node.Children[1].Token.IsMultiplication() && node.Children[1].Children[1].Token.IsNumber() && node.Children[1].Children[0].GetHash() == node.Children[0].GetHash())
                {
                    Write("\tSimplification: Subtraction: Dual Node: Sub one.");
                    RPN.Node temp = new RPN.Node(GenerateNextID(), 0)
                    {
                        Parent = node,
                    };
                    node.Replace( node.Children[0].ID, temp );
                    node.Children[1].Children[1].Token.Value = (double.Parse(node.Children[1].Children[1].Token.Value) - 1).ToString();
                }
                //3sin(x) - 0
                else if (node.Children[0].Token.Value == "0")
                {
                    //Root case
                    if (node.isRoot)
                    {
                        SetRoot(node.Children[1]);
                    }
                    //Non-root case
                    else if (!node.isRoot)
                    {
                        node.Parent.Replace(node.ID, node.Children[1]);
                    }

                    Write("\tSubtraction by zero.");
                }
                //0 - 3sin(x)
                else if (node.Children[1].Token.Value == "0")
                {
                    //Root case
                    if (node.isRoot)
                    {
                        SetRoot( node.Children[0] );
                    }
                    //Non-root case
                    else if (!node.isRoot)
                    {
                        node.Parent.Replace(node.ID, node.Children[0]);
                    }
                    Write("\tSubtraction by zero. Case 2.");
                }
            }
            //Addition
            else if (mode == SimplificationMode.Addition && node.Token.IsAddition())
            {
                //Is root and leafs have the same hash
                if (node.ChildrenAreIdentical())
                {
                    //TODO: Replace
                    node.Children[0].Children = new RPN.Node[0];
                    node.Children[0].Token.Value = "2";
                    node.Children[0].Token.Type = RPN.Type.Number;
                    node.Token.Value = "*";
                    Write("\tSimplification: Addition -> Multiplication");
                }
                //Both nodes are multiplications with 
                //the parent node being addition
                //Case: 2sin(x) + 3sin(x)
                else if (node.Children[0].Token.Value == node.Children[1].Token.Value && node.Children[0].Token.IsMultiplication())
                {
                    if (node.Children[0].Children[0].GetHash() == node.Children[1].Children[0].GetHash() &&
                        (node.Children[0].Children[1].Token.IsNumber() && node.Children[1].Children[1].Token.IsNumber() )
                        )
                    {
                        Write("\tSimplification: Addition");
                        double coef1 = double.Parse( node.Children[0].Children[1].Token.Value);
                        double coef2 = double.Parse( node.Children[1].Children[1].Token.Value);
                        string sum = (coef1 + coef2).ToString();

                        node.Children[1].Children[1].Token.Value = sum;
                        node.Children[0].Children[1].Token.Value = "0";
                    }
                }
                //Zero addition
                else if (node.Children[0].Token.IsNumber() && node.Children[0].Token.Value == "0")
                {
                    //Child 1 is the expression in this case.
                    if (!node.isRoot)
                    {
                        Write("\tZero Addition.");
                        node.Parent.Replace(node.ID, node.Children[1]);
                    }
                    else if (node.isRoot)
                    {
                        Write("\tZero Addition. Root case.");
                        SetRoot( node.Children[1] );
                    }
                }
                //Case: 0 + sin(x)
                else if (node.Children[1].Token.IsNumber() && node.Children[1].Token.Value == "0")
                {
                    //Child 1 is the expression in this case.
                    if (!node.isRoot)
                    {
                        Write("\tZero Addition. Case 2.");
                        node.Parent.Replace(node, node.Children[0]);
                    }
                    else if (node.isRoot)
                    {
                        Write("\tZero Addition. Root case. Case 2.");
                        SetRoot( node.Children[0] );
                    }
                }
                //7sin(x) + sin(x)
                //C0: Anything
                //C1:C0: Compare hash to C0.
                else if (node.Children[1].Token.IsMultiplication() &&
                         node.Children[1].Children[1].Token.IsNumber() &&
                         node.Children[1].Children[0].GetHash() == node.Children[0].GetHash()
                         )
                {
                      Write("\tSimplification Addition Dual Node.");

                     //Clears Children
                      node.Children[0].Children = new RPN.Node[0];
                     //Changes child node C0 to a zero number
                      node.Children[0].Token.Value = "0";
                      node.Children[0].Token.Type = RPN.Type.Number;

                      //Changes child node c1:c1 by incrementing it by one.
                      node.Children[1].Children[1].Token.Value =
                        (double.Parse(node.Children[1].Children[1].Token.Value) + 1).ToString();
                }

            }
             
            else if (mode == SimplificationMode.Multiplication && node.Token.IsMultiplication())
            {
                //TODO: If one of the leafs is a division and the other a number or variable
                if (node.ChildrenAreIdentical())
                {
                    RPN.Node temp = node.Children[0];

                    RPN.Node two = new RPN.Node(GenerateNextID(), 2)
                    {
                        Parent = node,
                    };

                    RPN.Node head = new RPN.Node()
                    {
                        Children = new RPN.Node[] {two, temp},
                        ID = GenerateNextID(),
                        Parent = node.Parent,
                        Token = new RPN.Token()
                        {
                            Arguments = 2,
                            Type = RPN.Type.Operator,
                            Value = "^"
                        }
                    };

                    //Is not the root
                    if (!node.isRoot)
                    {
                        node.Parent.Replace( node.ID, head );
                    }
                    else
                    {
                        SetRoot( head );
                    }

                    Write("\tSimplification: Multiplication -> Exponent\n");
                }
                else if (node.Children[1].Token.IsNumber() && node.Children[0].Token.IsMultiplication()) 
                {
                    if (node.Children[0].Children[1].Token.IsNumber() && !node.Children[0].Children[0].Token.IsNumber())
                    {
                        double num1 = double.Parse(node.Children[0].Children[1].Token.Value);
                        double num2 = double.Parse(node.Children[1].Token.Value);
                        double result = num1 * num2;
                        //TODO: Replace
                        node.Children[0].Children[1].Token.Value = "1";
                        node.Children[1].Token.Value = result.ToString();
                        Write("\tDual Node Multiplication.");
                    }
                }
                else if (node.Children[1].Token.IsNumber() && node.Children[1].Token.Value == "1")
                {
                    RPN.Node temp = node.Children[0];
                    if (!node.isRoot)
                    {
                        Write("\tMultiplication by one simplification.");
                        node.Parent.Replace(node.ID, temp);
                    }
                    else if (node.isRoot)
                    {
                        Write("\tMultiplication by one simplification. Root type.");
                        SetRoot( temp );
                    }
                }
                else if (node.Children[1].Token.IsNumber() && node.Children[1].Token.Value == "0")
                {
                    RPN.Node temp = node.Children[1];
                    if (!node.isRoot)
                    {
                        Write("\tMultiplication by zero simplification.");
                        node.Parent.Replace(node.ID, temp);
                    }
                    else if (node.isRoot)
                    {
                        Write("\tMultiplication by zero simplification. Root type.");
                        SetRoot( temp );
                    }
                }
                //sin(x)sin(x)sin(x) -> sin(x)^3
                else if (node.Children[1].Token.IsExponent() && node.Children[1].Children[0].Token.IsNumber() && node.Children[0].GetHash() == node.Children[1].Children[1].GetHash())
                {
                    Write("\tIncrease Exponent");
                    RPN.Node one = new RPN.Node(GenerateNextID(),1)
                    {
                        Parent = node,
                    };

                    node.Replace( node.Children[0].ID, one );
                    node.Children[1].Children[0].Token.Value = (double.Parse(node.Children[1].Children[0].Token.Value) + 1).ToString();
                }
            }
            else if (mode == SimplificationMode.Swap)
            {
                //We can do complex swapping in here
                if (node.Token.IsMultiplication() && node.Children[0].Token.IsMultiplication() && node.Children[0].Children[0].GetHash() == node.Children[1].GetHash())
                {
                    Write($"\tComplex Swap: Dual Node Multiplication Swapping");
                    RPN.Node temp = node.Children[0].Children[1];

                    node.Children[0].Children[1] = node.Children[1];
                    node.Children[1] = temp;
                }
            }
            else if (mode == SimplificationMode.Exponent && node.Token.IsExponent())
            {
                RPN.Node baseNode = node.Children[1];
                RPN.Node power = node.Children[0];
                if (power.Token.IsNumber() && double.Parse(power.Token.Value) == 1)
                {
                    Write("f(x)^1 -> f(x)");
                    if (node.isRoot)
                    {
                        SetRoot(baseNode);
                    }
                    else if (!node.isRoot)
                    {
                        node.Parent.Replace(node, baseNode);
                    }
                    Delete(power);
                    Delete(node);
                }
                //f(x)^1 -> f(x)

                //f(x)^0
            }
            else if (mode == SimplificationMode.Trig)
            {
                //sin^2(x) + cos^2(x) -> 1
                /*
                 * + [10: bc036401aa0ff2d9c7cf30d8699c2295:2]
                   ├─^ [14: 311d333b0c00c30f7b3386eaafcefc18:2] - Y
                   │  ├─2 [13: c81e728d9d4c2f636f067f89cc14862c:0] - Y
                   │  └─cos [8: ec3c3157b2c02e355b18cb95ff7434aa:1]
                   │     └─x [7: 9dd4e461268c8034f5c8564e155c67a6:0]
                   └─^ [12: 5f4ae9897c0b3167a1328e853f71afb3:2] - Y
                     ├─2 [11: c81e728d9d4c2f636f067f89cc14862c:0] - Y
                     └─sin [3: 96e8dfa5386fa00a9ea20f0daea9b0dd:1]
                        └─x [2: 9dd4e461268c8034f5c8564e155c67a6:0]
                 */
                if (node.Token.IsAddition() &&
                    node.Children[0].Token.IsExponent() &&
                    node.Children[1].Token.IsExponent() &&
                    node.Children[0].Children[0].Token.IsNumber() &&
                    node.Children[1].Children[0].Token.IsNumber() &&

                    node.Children[0].Children[0].Token.Value == "2" &&
                    node.Children[1].Children[0].Token.Value == "2" &&
                    node.Children[0].Children[1].Children[0].GetHash() ==
                    node.Children[1].Children[1].Children[0].GetHash() &&
                    (
                        (node.Children[0].Children[1].Token.Value == "cos" && node.Children[1].Children[1].Token.Value == "sin") || 
                        (node.Children[0].Children[1].Token.Value == "sin" && node.Children[1].Children[1].Token.Value == "cos") 
                    )
                )
                {
                    RPN.Node head = new RPN.Node(GenerateNextID(), 1);

                    if (node.isRoot)
                    {
                        SetRoot( head );
                        Write("\tsin²(x) + cos²(x) -> 1. Root case.");
                    }
                    else
                    {
                        head.Parent = node.Parent;
                        node.Parent.Replace(node.ID, head);
                        Write("\tsin²(x) + cos²(x) -> 1");
                    }
                }

            }

            //Propagate down the tree IF there is a root 
            //which value is not NaN or a number
            if (Root == null || Root.Token.Value == "NaN" || Root.Token.IsNumber() )
            {
                return;
            }

            //Propagate down the tree
            for (int i = (node.Children.Length - 1); i >= 0; i--)
            {
                Simplify(node.Children[i], mode);
            }
        }

        private void Swap()
        {
            Swap(Root);
        }

        private void Swap(RPN.Node node)
        {
            //Addition operator
            if (node.Token.IsAddition())
            {
                //Two numbers

                //Number and expression
                if (node.Children[0].Token.IsNumber() && !(node.Children[1].Token.IsNumber() || node.Children[1].Token.IsVariable()))
                {
                    Write("\tNode flip possible: Add");
                }
                //Number and a variable
                else if ( node.Children[0].Token.IsNumber() && !node.Children[1].Token.IsNumber())
                {
                    node.Children.Swap(1, 0);
                    Write("\tNode flip : Add : Number and a variable");
                }
            }
            //Multiplication operator
            else if (node.Token.IsMultiplication())
            {
                //a number and a expression
                if (node.Children[0].Token.IsNumber() && !(node.Children[1].Token.IsNumber() || node.Children[1].Token.IsVariable()))
                {
                    Write($"\tMultiplication Swap");
                    node.Children.Swap(1, 0);
                }
            }

            //Propagate down the tree
            for (int i = (node.Children.Length - 1); i >= 0; i--)
            {
                Swap(node.Children[i]);
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
            MetaFunctions(Root);
            SW.Stop();

            _data.AddTimeRecord("AST MetaFunctions", SW);
            Simplify();

            return this;
        }

        private void MetaFunctions(RPN.Node node)
        {
            //Propagate down the tree
            for (int i = 0; i < node.Children.Length; i++)
            {
                MetaFunctions(node.Children[i]);
            }

            if (node.Token.IsFunction() && _data.MetaFunctions.Contains(node.Token.Value))
            {
                if (node.Token.Value == "integrate")
                {
                    double answer = double.NaN;
                    if (node.Children.Length == 5)
                    {
                        answer = MetaCommands.Integrate(_rpn,
                            node.Children[4],
                            node.Children[3],
                            node.Children[2],
                            node.Children[1],
                            node.Children[0]);
                    }
                    else if (node.Children.Length == 4)
                    {
                        answer = MetaCommands.Integrate(_rpn,
                            node.Children[3],
                            node.Children[2],
                            node.Children[1],
                            node.Children[0],
                            new RPN.Node(GenerateNextID(), 0.001));
                    }

                    RPN.Node temp = new RPN.Node(GenerateNextID(), answer);

                    if (node.isRoot)
                    {
                        SetRoot(temp);
                    }
                    else
                    {
                        node.Parent.Replace(node.ID, temp);
                    }
                }
                else if (node.Token.Value == "table")
                {
                    string table = string.Empty;
                    if (node.Children.Length == 5)
                    {
                        table = MetaCommands.Table(_rpn,
                            node.Children[4],
                            node.Children[3],
                            node.Children[2],
                            node.Children[1],
                            node.Children[0]);
                    }
                    else
                    {
                        table = MetaCommands.Table(_rpn,
                            node.Children[3],
                            node.Children[2],
                            node.Children[1],
                            node.Children[0],
                            new RPN.Node(GenerateNextID(), 0.001));
                    }
                    //Write("Table Write: " + table);
                    stdout(table);
                    SetRoot(new RPN.Node(GenerateNextID(), double.NaN));
                }
                else if (node.Token.Value == "derivative")
                {
                    //We solve first in an attempt to make constants easier to find
                    //for the derivative algorithm.
                    Solve(Root);
                    GenerateDerivativeAndReplace(node.Children[1]);
                    Derive(node.Children[0]); 
                    if (node.isRoot)
                    {
                        SetRoot(node.Children[1]);
                    }
                    else if (!node.isRoot)
                    {
                        node.Parent.Replace(node.ID, node.Children[1]);
                    }
                    Delete(node);

                    if (_data.DebugMode)
                    {
                        Write($"Derived graph:");
                        Write($"{Root.Print()}");
                    }
                    Solve(Root);
                }
            }
        }

        private void Solve(RPN.Node node)
        {
            if (node == null)
            {
                return;
            }

            //All children of a node must be either numbers of constant functions
            bool isSolveable = node.Children.All(t => t.Token.Type == RPN.Type.Number || t.Token.IsConstant());

            //Functions that are not constants and/or meta functions 
            if ( (node.Token.IsFunction() && ! (node.Token.IsConstant() || _data.MetaFunctions.Contains(node.Token.Value)) || node.Token.IsOperator()) && isSolveable)
            {
                PostFix math = new PostFix(_rpn);
                double answer = math.Compute(node.ToPostFix().ToArray());
                if (node.isRoot)
                {
                    SetRoot(new RPN.Node(GenerateNextID(), answer));
                }
                else if (!node.isRoot)
                {
                    node.Parent.Replace(node, new RPN.Node(GenerateNextID(), answer));
                }
                //Since we solved something lower in the tree we may be now able 
                //to solve something higher up in the tree!
                Solve(node.Parent);
            }

            //Propagate down the tree
            for (int i = 0; i < node.Children.Length; i++)
            {
                Solve(node.Children[i]);
            }
        }


        private AST Derive(RPN.Node variable)
        {
            Write("Starting to derive ROOT.");
            Derive(Root, variable);
            return this;
        }

        private void Derive(RPN.Node node, RPN.Node variable)
        {
            if (node.Token.Value == "derive")
            {
                if (node.Children[0].Token.IsAddition() || node.Children[0].Token.IsSubtraction())
                {
                    Write("DERIVE: Add/Sub Prorogation");
                    GenerateDerivativeAndReplace(node.Children[0].Children[0]);
                    GenerateDerivativeAndReplace(node.Children[0].Children[1]);
                    //Recurse explicitly down these branches
                    Derive(node.Children[0].Children[0], variable);
                    Derive(node.Children[0].Children[1], variable);
                    //Delete myself from the tree
                    node.Parent.Replace(node.ID, node.Children[0]);
                    Delete(node);
                }
                //Constant Rule -> 0
                else if (node.Children[0].Token.IsNumber() || node.Children[0].Token.IsConstant() || (node.Children[0].Token.IsVariable() && node.Children[0].Token.Value != variable.Token.Value))
                {
                    Write("DERIVE: Constant Rule");
                    node.Children[0].Parent = null;
                    RPN.Node temp = new RPN.Node(GenerateNextID(), 0);
                    //Remove myself from the tree
                    node.Parent.Replace(node.ID, temp);
                    Delete(node);
                }
                //Variable -> 1
                else if (node.Children[0].Token.IsVariable() && node.Children[0].Token.Value == variable.Token.Value)
                {
                    Write("DERIVE: Variable");
                    node.Children[0].Parent = null;
                    RPN.Node temp = new RPN.Node(GenerateNextID(), 1);
                    //Remove myself from the tree
                    node.Parent.Replace(node.ID, temp);
                    Delete(node);
                }
                else if (node.Children[0].Token.IsMultiplication())
                {
                    //Both numbers
                    if ((node.Children[0].Children[0].Token.IsNumber() || node.Children[0].Children[0].Token.IsConstant()) && (node.Children[0].Children[1].Token.IsNumber() || node.Children[0].Children[1].Token.IsConstant()))
                    {
                        RPN.Node temp = new RPN.Node(GenerateNextID(), 0);
                        //Remove myself from the tree
                        node.Parent.Replace(node.ID, temp);
                        Delete(node);
                    }
                    //Constant multiplication - 0
                    else if ( (node.Children[0].Children[0].Token.IsNumber() || node.Children[0].Children[0].Token.IsConstant()) && IsExpression(node.Children[1]))
                    {
                        Write("DERIVE: Constant multiplication - 0");
                        GenerateDerivativeAndReplace(node.Children[0].Children[1]);
                        //Recurse explicitly down these branches
                        Derive(node.Children[0].Children[1], variable);
                        //Remove myself from the tree
                        node.Parent.Replace(node.ID, node.Children[0]);
                        Delete(node);
                    }
                    //Constant multiplication - 1
                    else if ((node.Children[0].Children[1].Token.IsNumber() || node.Children[0].Children[1].Token.IsConstant()) && IsExpression(node.Children[0]))
                    {
                        Write("DERIVE: Constant multiplication - 1");
                        GenerateDerivativeAndReplace(node.Children[0].Children[0]);
                        //Recurse explicitly down these branches
                        Derive(node.Children[0].Children[0], variable);

                        //Remove myself from the tree
                        node.Parent.Replace(node.ID, node.Children[0]);
                        Delete(node);
                    }
                    //Product Rule [Two expressions] 
                    else
                    {
                        Write($"DERIVE: Product Rule");

                        RPN.Token multiply = new RPN.Token("*",2,RPN.Type.Operator); 

                        RPN.Node fNode = node.Children[0].Children[0];
                        RPN.Node gNode = node.Children[0].Children[1];

                        RPN.Node fDerivative = new RPN.Node(GenerateNextID(), new[] {Clone(fNode)}, _derive);
                        RPN.Node gDerivative = new RPN.Node(GenerateNextID(), new[] {Clone(gNode)}, _derive);

                        RPN.Node multiply1 = new RPN.Node(GenerateNextID(), new[] {gDerivative, fNode}, multiply);
                        RPN.Node multiply2 = new RPN.Node(GenerateNextID(), new[] {fDerivative, gNode}, multiply);

                        RPN.Node add = new RPN.Node(GenerateNextID(), new []{multiply1, multiply2}, new RPN.Token("+",2,RPN.Type.Operator));

                        //Remove myself from the tree
                        node.Parent.Replace(node.ID, add);
                        Delete(node);

                        //Explicit recursion
                        Derive(fDerivative, variable);
                        Derive(gDerivative, variable);
                    }
                }
                else if (node.Children[0].Token.IsDivision())
                {
                    //Quotient Rule
                    Write("DERIVE: Quotient Rule");
                    RPN.Token multiply = new RPN.Token("*", 2, RPN.Type.Operator);

                    RPN.Node numerator = node.Children[0].Children[1];
                    RPN.Node denominator = node.Children[0].Children[0];

                    RPN.Node numeratorDerivative   = new RPN.Node(GenerateNextID(), new []{Clone(numerator)}, _derive); 
                    RPN.Node denominatorDerivative = new RPN.Node(GenerateNextID(), new []{Clone(denominator)}, _derive); 

                    RPN.Node multiplicationOne = new RPN.Node(GenerateNextID(), new []{numeratorDerivative, denominator}, multiply);
                    RPN.Node multiplicationTwo = new RPN.Node(GenerateNextID(), new []{denominatorDerivative, numerator}, multiply);

                    RPN.Node subtraction = new RPN.Node(GenerateNextID(), new []{multiplicationTwo, multiplicationOne}, new RPN.Token("-",2,RPN.Type.Operator));

                    RPN.Node denominatorSquared = new RPN.Node(GenerateNextID(), new []{ new RPN.Node(GenerateNextID(), 2), Clone(denominator)}, new RPN.Token("^",2,RPN.Type.Operator));

                    //Replace in tree
                    node.Children[0].Replace(numerator, subtraction);
                    node.Children[0].Replace(denominator, denominatorSquared);
                    //Delete myself from the tree
                    node.Parent.Replace(node, node.Children[0]);
                    Delete(node);
                    //Explicitly recurse down these branches
                    Derive(subtraction, variable);
                }
                //Exponents! 
                else if (node.Children[0].Token.IsExponent())
                {
                    //C0: 3 C1:2
                    Write($"Exponent: C0: {node.Children[0].Children[0].Token.Value} C1:{node.Children[0].Children[1].Token.Value}");
                    RPN.Node baseNode = node.Children[0].Children[1];
                    RPN.Node power = node.Children[0].Children[0];

                    //x^n -> n * x^(n - 1)
                    if ( (baseNode.Token.IsVariable() || baseNode.Token.IsFunction()) && (power.Token.IsConstant() || power.Token.IsNumber()))
                    {
                        if (baseNode.Token.Value == variable.Token.Value )
                        {
                            Write("DERIVE: Power Rule");

                            RPN.Node powerClone = Clone(power);
                            RPN.Node one = new RPN.Node(GenerateNextID(), 1);

                            RPN.Node subtraction = new RPN.Node(GenerateNextID(), new[] {one, powerClone},
                                new RPN.Token("-", 2, RPN.Type.Operator));

                            //Replace n with (n - 1) 
                            RPN.Node exponent = new RPN.Node(GenerateNextID(), new RPN.Node[] {subtraction, baseNode},
                                new RPN.Token("^", 2, RPN.Type.Operator));

                            RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] {exponent, power},
                                new RPN.Token("*", 2, RPN.Type.Operator));

                            node.Replace(node.Children[0], multiplication);

                            //Delete self from the tree
                            node.Parent.Replace(node, node.Children[0]);
                            Delete(node);
                        }
                        else if (baseNode.Token.IsFunction())
                        {
                            //TODO: Implement Power Chain Rule
                            Write("DERIVE: Power Chain Rule");
                        }
                        else
                        {
                            Write("DERIVE: Power Rule edge case.");
                            node.Children[0].Parent = null;
                            RPN.Node temp = new RPN.Node(GenerateNextID(), 0);
                            //Remove myself from the tree
                            node.Parent.Replace(node.ID, temp);
                            Delete(node);
                        }
                    }
                    else if (baseNode.Token.IsConstant() && baseNode.Token.Value == "e")
                    {
                        Write("e^g(x) -> g'(x)e^g(x)");
                        RPN.Node exponent = baseNode.Parent;
                        RPN.Node powerDerivative = new RPN.Node(GenerateNextID(), new []{Clone(power)}, _derive);
                        RPN.Node multiply = new RPN.Node(GenerateNextID(), new []{powerDerivative, exponent}, new RPN.Token("*", 2, RPN.Type.Operator));
                        node.Replace(power.Parent, multiply);
                        //Delete self from the tree
                        node.Parent.Replace(node, node.Children[0]);
                        Delete(node);

                        Derive(powerDerivative, variable);
                    }
                    //TODO: b^x
                    else if (baseNode.Token.IsConstant() || baseNode.Token.IsNumber() && IsExpression(power))
                    {
                        
                    }
                    //TODO: x^x
                    else if (IsExpression(baseNode) && IsExpression(power))
                    {

                    }
                    else
                    {
                        Write($"Derivative of {node.Children[0].ToInfix()} not known at this time. ");
                    }
                }
                #region Trig
                else if (node.Children[0].Token.Value == "sin")
                {
                    //sin(x) -> cos(x)derive(x)
                    Write("DERIVE: sin(g(x)) -> cos(g(x))g'(x)");
                    RPN.Node body = node.Children[0].Children[0];

                    RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new []{Clone(body)}, _derive);

                    RPN.Node cos = new RPN.Node(GenerateNextID(), new []{body}, new RPN.Token("cos",1,RPN.Type.Function));

                    RPN.Node multiply = new RPN.Node(GenerateNextID(), new []{cos, bodyDerive}, new RPN.Token("*",2,RPN.Type.Operator));

                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Parent.Replace(node, node.Children[0]);
                    Delete(node);
                    //Chain Rule
                    Derive(bodyDerive, variable);
                }
                else if (node.Children[0].Token.Value == "cos")
                {
                    //cos(g(x)) -> - sin(g(x)) g'(x)
                    Write("DERIVE: cos(g(x)) -> -sin(g(x))g'(x)");
                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new []{Clone(body)}, _derive);

                    RPN.Node sin = new RPN.Node(GenerateNextID(),new []{body}, new RPN.Token("sin",1,RPN.Type.Function));
                    RPN.Node negativeOneMultiply = new RPN.Node(GenerateNextID(), new [] {new RPN.Node(GenerateNextID(), -1), sin } , new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node multiply = new RPN.Node(GenerateNextID(), new []{negativeOneMultiply, bodyDerive}, new RPN.Token("*",2,RPN.Type.Operator));

                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Parent.Replace(node, node.Children[0]);
                    Delete(node);
                    //Chain Rule
                    Derive(bodyDerive, variable);
                }
                else if (node.Children[0].Token.Value == "tan")
                {
                    //tan(g(x)) -> sec(g(x))^2 g'(x)
                    Write("tan(g(x)) -> sec(g(x))^2 g'(x)");
                    RPN.Node body = node.Children[0].Children[0];
                    RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new []{Clone(body)}, _derive);

                    RPN.Node sec = new RPN.Node(GenerateNextID(), new [] {body}, new RPN.Token("sec",1,RPN.Type.Function));
                    RPN.Node exponent = new RPN.Node(GenerateNextID(), new []{new RPN.Node(GenerateNextID(),2), sec}, new RPN.Token("^",2,RPN.Type.Operator) );

                    RPN.Node multiply = new RPN.Node(GenerateNextID(), new []{exponent, bodyDerive},new RPN.Token("*",2,RPN.Type.Operator));
                    node.Replace(node.Children[0], multiply);
                    //Delete self from the tree
                    node.Parent.Replace(node, node.Children[0]);
                    Delete(node);
                    //Chain Rule
                    Derive(bodyDerive, variable);
                }
                #endregion
                else
                {
                    Write($"Derivative of {node.Children[0].ToInfix()} not known at this time. ");
                }
                //TODO:
                //All of this stuff requires chain rule! 
                //Trig
                    //sec
                    //csc
                    //cot
                //Inverse Trig
                    //arcsin
                    //arccos
                    //arcsec
                    //arccsc
                    //acccot
                //sqrt
                //abs
                //ln
                //log
            }

            //Propagate down the tree
            for (int i = (node.Children.Length - 1); i >= 0; i--)
            {
                Derive(node.Children[i], variable);
            }
        }

        private static void Delete(RPN.Node node)
        {
            node.Parent = null;
            node.Children = new RPN.Node[0];
        }

        private int GenerateNextID()
        {
            count++;
            return count;
        }

        private void GenerateDerivativeAndReplace(RPN.Node child)
        {
            RPN.Node temp = new RPN.Node()
            {
                Children = new RPN.Node[] {child},
                ID = GenerateNextID(),
                Parent = child.Parent,
                Token = new RPN.Token
                {
                    Arguments = 1,
                    Type = RPN.Type.Function,
                    Value = "derive"
                }
            };

            child.Parent?.Replace(child.ID, temp);

            child.Parent = temp;
        }

        private RPN.Node Clone(RPN.Node node)
        {
            return Generate(node.ToPostFix().ToArray());
        }

        private bool IsExpression(RPN.Node node)
        {
            return !(node.Token.IsNumber() || node.Token.IsVariable() || node.Token.IsConstant());
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

        private void Write(string message)
        {
            Logger?.Invoke(this, message);
        }

        private void stdout(string message)
        {
            Output?.Invoke(this, message);
        }
    }
}