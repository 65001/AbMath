using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public class AST
    {
        public RPN.Node Root { get; private set; }

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
        /// Returns the postfix representation
        /// of the entire abstract syntax tree.
        /// </summary>
        /// <returns></returns>
        public List<RPN.Token> ToPostFix()
        {
            return ToPostFix(Root);
        }

        /// <summary>
        /// Returns the postfix representation of
        /// the initial node and its descendents.
        /// </summary>
        /// <param name="node">The initial node</param>
        /// <returns></returns>
        public List<RPN.Token> ToPostFix(RPN.Node node)
        {
            List<RPN.Token> tokens = new List<RPN.Token>();
            PostFix(node, tokens);
            return tokens;
        }

        private void PostFix(RPN.Node node, List<RPN.Token> polish)
        {
            if (node is null)
            {
                return;
            }

            //Operators with left and right
            if (node.Children.Count == 2 && node.Token.IsOperator())
            {
                PostFix(node.Children[1],  polish);
                PostFix(node.Children[0],  polish);
                polish.Add(node.Token);
                return;
            }

            //Operators that only have one child
            if (node.Children.Count == 1 && node.Token.IsOperator())
            {
                PostFix(node.Children[0],  polish);
                polish.Add(node.Token);
                return;
            }

            //Functions
            if (node.Children.Count > 0 && node.Token.IsFunction())
            {
                for (int i = (node.Children.Count - 1); i >= 0; i--)
                {
                    PostFix(node.Children[i],  polish);
                }
                polish.Add(node.Token);
                return;
            }

            //Number, Variable, or constant function
            polish.Add(node.Token);
        }

        private void Write(string message)
        {
            Logger?.Invoke(this, message);
        }
    }
}
