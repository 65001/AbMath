using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AbMath.Calculator
{
    public class AST
    {
        public RPN.Node Root { get; private set; }

        private enum SimplificationMode
        {
            Imaginary, Division, Subtraction, Addition, Multiplication, Trig
        }

        private RPN _rpn;
        private RPN.DataStore _data;
        private Stack<RPN.Node> _stack;
        private int count = -1;

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
            //Convert all the PostFix information to Nodes[]
            RPN.Node[] nodes = new RPN.Node[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                nodes[i] = new RPN.Node()
                {
                    Children = new RPN.Node[0],
                    ID = i,
                    Parent = null,
                    Token = input[i]
                };
            }

            count = input.Length - 1;
            Write($"Count:{count}");

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

            return _stack.Pop();
        }

        /// <summary>
        /// Simplifies the current tree.
        /// </summary>
        /// <returns></returns>
        public AST Simplify()
        {
            int pass = 0;
            string hash = string.Empty;

            Write("");

            while (hash != Root.GetHash())
            {
                hash = Root.GetHash();
                Write($"Pass: {pass}\n\tHash: {hash}");
                Swap(Root);
                Simplify(Root, SimplificationMode.Imaginary);
                Simplify(Root, SimplificationMode.Division);
                Simplify(Root, SimplificationMode.Subtraction);
                Simplify(Root, SimplificationMode.Addition);
                Simplify(Root, SimplificationMode.Multiplication);
                Simplify(Root, SimplificationMode.Trig);
                pass++;

                Write($"{this.Root.Print()}");
                Write($"{this.Root.ToInfix()}");
                Write($"");
            }

            if (_data.DebugMode)
            {
                Write("");
            }

            return this;
        }

        private void Simplify(RPN.Node node, SimplificationMode mode)
        {
            //Imaginary
            if (mode == SimplificationMode.Imaginary && node.Token.Value == "sqrt")
            {
                //Any sqrt function with a negative number -> Imaginary number to the root node
                //An imaginary number propagates anyways
                if (node.Children[0].Token.IsNumber() && double.Parse(node.Children[0].Token.Value) < 0)
                {
                    Root = new RPN.Node
                    {
                        Children = new RPN.Node[0],
                        ID = GenerateNextID(),
                        Parent = null,
                        Token = new RPN.Token
                        {
                            Arguments = 0,
                            Type = RPN.Type.Number,
                            Value = "NaN"
                        }
                    };
                    Write("\tSqrt Imaginary Number -> Root.");
                }
                //MAYBE: Any sqrt function with any non-positive number -> Cannot simplify further??
            }
            //Division
            else if (mode == SimplificationMode.Division && node.Token.Value == "/")
            {
                //if there are any divide by zero exceptions -> NaN to the root node
                //NaN propagate anyways
                if (node.Children[0].Token.Value == "0")
                {
                    Root = new RPN.Node
                    {
                        Children = new RPN.Node[0],
                        ID = GenerateNextID(),
                        Parent = null,
                        Token = new RPN.Token
                        {
                            Arguments = 0,
                            Type = RPN.Type.Number,
                            Value = "NaN"
                        }
                    };
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
            }
            //Subtraction
            else if (mode == SimplificationMode.Subtraction && node.Token.IsSubtraction())
            {
                //3sin(x) - 3sin(x)
                if ( node.ChildrenAreIdentical())
                {
                    if (node.isRoot)
                    {
                        Root = new RPN.Node
                        {
                            Children = new RPN.Node[0],
                            ID = GenerateNextID(),
                            Parent = null,
                            Token = new RPN.Token
                            {
                                Arguments = 0,
                                Type = RPN.Type.Number,
                                Value = "0"
                            }
                        };
                        Write("\tSimplification: Subtraction. Children are identical. Root.");
                    }
                    else if (!node.isRoot)
                    {
                        node.Parent.Replace(node.ID, new RPN.Node
                        {
                            Children = new RPN.Node[0],
                            ID = GenerateNextID(),
                            Parent = node.Parent,
                            Token = new RPN.Token
                            {
                                Arguments = 0,
                                Type = RPN.Type.Number,
                                Value = "0"
                            }
                        });
                        Write("\tSimplification: Subtraction");
                    }
                }
                //3sin(x) - 2sin(x)
                else if (node.Children[0].Token.IsMultiplication() && node.Children[1].Token.IsMultiplication())
                {
                    if (node.Children[0].Children[0].GetHash() == node.Children[1].Children[0].GetHash() &&
                        node.Children[0].Children[1].Token.IsNumber() && node.Children[1].Children[1].Token.IsNumber())
                    {
                        Write("\tSimplification: Subtraction Dual Node");
                        double coefficient = double.Parse(node.Children[1].Children[1].Token.Value) -
                                             double.Parse(node.Children[0].Children[1].Token.Value);

                        node.Children[0].Children[1].Token.Value = "0";
                        node.Children[1].Children[1].Token.Value = coefficient.ToString();
                    }
                }
                //3sin(x) - sin(x)
                else if (node.Children[1].Token.IsMultiplication() && 
                         node.Children[1].Children[1].Token.IsNumber() &&
                         node.Children[1].Children[0].GetHash() == node.Children[0].GetHash())
                {
                      Write("\tSimplification: Subtraction: Dual Node: Sub one.");
                     
                    RPN.Node temp = new RPN.Node
                    {
                        Children = new RPN.Node[0],
                        ID = GenerateNextID(),
                        Parent = node,
                        Token = new RPN.Token
                        {
                            Arguments = 0,
                            Type = RPN.Type.Number,
                            Value = "0"
                        }
                    };

                     node.Replace( node.Children[0].ID, temp );

                     node.Children[1].Children[1].Token.Value =
                        (double.Parse(node.Children[1].Children[1].Token.Value) - 1).ToString();
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
                        node.Parent.Replace(node.ID, node.Children[0]);
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
                    Write($"\tThe current node is {node.GetHash()}");
                    RPN.Node temp = node.Children[0];

                    RPN.Node two = new RPN.Node()
                    {
                        Children = new RPN.Node[0],
                        ID = GenerateNextID(),
                        Parent = node,
                        Token = new RPN.Token
                        {
                            Arguments = 0,
                            Type = RPN.Type.Number,
                            Value = "2"
                        }
                    };

                    RPN.Node head = new RPN.Node()
                    {
                        Children = new RPN.Node[2] {two, temp},
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
                    RPN.Node one = new RPN.Node()
                    {
                        Children = new RPN.Node[0],
                        ID = GenerateNextID(),
                        Parent = node,
                        Token = new RPN.Token
                        {
                            Arguments = 0,
                            Type = RPN.Type.Number,
                            Value = "1"
                        }
                    };

                    node.Replace( node.Children[0].ID, one );
                    node.Children[1].Children[0].Token.Value = (double.Parse(node.Children[1].Children[0].Token.Value) + 1).ToString();
                }
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
                    
                    RPN.Token one = new RPN.Token
                    {
                        Arguments = 0,
                        Type = RPN.Type.Number,
                        Value = "1"
                    };

                    RPN.Node head = new RPN.Node()
                    {
                        Children = new RPN.Node[0],
                        ID = GenerateNextID(),
                        Parent = null,
                        Token = one
                    };

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
                    Write("\tNode flip possible: Add : Number and a variable");
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
        public void MetaFunctions()
        {
            MetaFunctions(Root);
        }

        private void MetaFunctions(RPN.Node node)
        {
            if (node.Token.IsFunction() && _data.MetaFunctions.Contains(node.Token.Value))
            {
                if (node.Token.Value == "integrate")
                {
                    double answer = MetaCommands.Integrate(_rpn,
                        node.Children[0],
                        node.Children[1],
                        node.Children[2],
                        node.Children[3],
                        node.Children[4]);
                }
            }
        }

        private int GenerateNextID()
        {
            count++;
            return count;
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