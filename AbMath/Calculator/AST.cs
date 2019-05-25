using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public class AST
    {
        public RPN.Node Root { get; private set; }
        private enum SimplificationMode
        {
            Imaginary, Division, Subtraction, Addition, Multiplication,
            Exponent, Functions, VariableFunctions, 
            Trig
        }

        private bool CanSimplify;

        private RPN.DataStore _data;
        private Stack<RPN.Node> _stack;

        public event EventHandler<string> Logger;

        public AST(RPN rpn)
        {
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
                    Children = new List<RPN.Node>(),
                    ID = i,
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
                        //Pop the operator or function and the number or arguments needed from the stack
                        List<RPN.Node> children = new List<RPN.Node>(nodes[i].Token.Arguments);
                        for (int j = 0; j < nodes[i].Token.Arguments; j++)
                        {
                            RPN.Node temp = _stack.Pop();
                            temp.Parent = nodes[i];
                            children.Add( temp );
                        }

                        nodes[i].Children = children;
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
            CanSimplify = true;
            int pass = 0;
            string hash = string.Empty;

            Write("");

            while (hash != Root.GetHash())
            {
                Write($"Pass: {pass}\n\tHash: {hash}");
                hash = Root.GetHash();
                Swap();
                Simplify(Root, SimplificationMode.Imaginary);
                Simplify(Root, SimplificationMode.Division);
                Simplify(Root, SimplificationMode.Subtraction);
                Simplify(Root, SimplificationMode.Addition);
                Simplify(Root, SimplificationMode.Multiplication);
                pass++;
                Write($"{this.Root.Print()}");
                Write($"{this.Root.ToInfix()}");
                Write($"");
            }
            Write("");
            return this;
        }

        private void Simplify(RPN.Node node, SimplificationMode mode)
        {
            //If we cannot simplify we must abort early.
            if (!CanSimplify)
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
                    Root = new RPN.Node
                    {
                        Children = new List<RPN.Node>(0),
                        ID = -1,
                        Parent = null,
                        Token = new RPN.Token
                        {
                            Arguments = 0,
                            Type = RPN.Type.Number,
                            Value = "NaN"
                        }
                    };
                }
                //MAYBE: Any sqrt function with any non-positive number -> Cannot simplify further??
            }
            //Division
            if (mode == SimplificationMode.Division && node.Token.Value == "/")
            {
                //if there are any divide by zero exceptions -> NaN to the root node
                //NaN propagate anyways
                if (node.Children[0].Token.Value == "0")
                {
                    Root = new RPN.Node
                    {
                        Children = new List<RPN.Node>(0),
                        ID = -1,
                        Parent = null,
                        Token = new RPN.Token
                        {
                            Arguments = 0,
                            Type = RPN.Type.Number,
                            Value = "NaN"
                        }
                    };
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
                }
            }
            //Subtraction
            else if (mode == SimplificationMode.Subtraction && node.Token.IsSubtraction())
            {
                //IF the node has no parent function then
                //we know by definition that it is the root
                //therefore we can replace the root with 0
                if (node.isRoot && node.ChildrenAreIdentical())
                {
                    Root = new RPN.Node
                    {
                        Children = new List<RPN.Node>(0),
                        ID = -1,
                        Parent = null,
                        Token = new RPN.Token
                        {
                            Arguments = 0,
                            Type = RPN.Type.Number,
                            Value = "0"
                        }
                    };
                    Write("\tSimplification: Subtraction");
                }
                //otherwise we need to replace the current 
                //token in the leaf with another node.
                else if (!node.isRoot && node.ChildrenAreIdentical())
                {
                    node.Parent.Replace( node.ID, new RPN.Node
                    {
                        Children = new List<RPN.Node>(),
                        ID = Root.ID + 1,
                        Parent = node.Parent,
                        Token = new RPN.Token
                        {
                            Arguments = 0,
                            Type = RPN.Type.Number,
                            Value = "0"
                        }
                    } );
                    Write("\tSimplification: Subtraction");
                }
            }
            //Addition
            else if (mode == SimplificationMode.Addition && node.Token.IsAddition())
            {
                //Is root and leafs have the same hash
                if (node.ChildrenAreIdentical())
                {
                    node.Children[0].Children.Clear();
                    node.Children[0].Token.Value = "2";
                    node.Children[0].Token.Type = RPN.Type.Number;
                    node.Token.Value = "*";
                    Write("\tSimplification: Addition");
                }
            }
            //Multiplication
            else if (mode == SimplificationMode.Multiplication && node.Token.Value == "*")
            {
                //TODO: If one of the leafs is a division and the other a number or variable
                //else
                if (node.ChildrenAreIdentical())
                {
                    node.Children[0].Children.Clear();
                    node.Children[0].Token.Value = "2";
                    node.Children[0].Token.Type = RPN.Type.Number;
                    node.Token.Value = "^";
                    Write("\tSimplification: Multiplication");
                }
                else if (node.Children[1].Token.IsNumber() && node.Children[0].Token.Value == "*" ) 
                {
                    if (node.Children[0].Children[1].Token.IsNumber() && !node.Children[0].Children[0].Token.IsNumber())
                    {
                        double num1 = double.Parse(node.Children[0].Children[1].Token.Value);
                        double num2 = double.Parse(node.Children[1].Token.Value);
                        double result = num1 * num2;
                        node.Children[0].Children[1].Token.Value = "1";
                        node.Children[1].Token.Value = result.ToString();
                    }
                }
                else if (node.Children[1].Token.IsNumber() && node.Children[1].Token.Value == "1")
                {
                    RPN.Node temp = node.Children[0];
                    node.Parent.Replace( node.ID, temp );
                }
            }

            //Propagate down the tree IF there is a root 
            //which value is not NaN or a number
            if (Root is null || Root.Token.Value == "NaN" || Root.Token.IsNumber() )
            {
                return;
            }

            //Propagate down the tree
            for (int i = (node.Children.Count - 1); i >= 0; i--)
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
            }
            //Multiplication operator
            else if (node.Token.IsMultiplication())
            {

            }

            //Propagate down the tree
            for (int i = (node.Children.Count - 1); i >= 0; i--)
            {
                Swap(node.Children[i]);
            }
        }

        private void Explode()
        {

        }

        private void Explode(RPN.Node node)
        {

        }

        private void Write(string message)
        {
            Logger?.Invoke(this, message);
        }
    }
}