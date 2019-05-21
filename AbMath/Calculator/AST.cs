using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public class AST
    {
        private RPN.DataStore _data;
        private Stack<RPN.Node> _stack;

        public event EventHandler<string> Logger;

        public AST(RPN rpn)
        {
            _data = rpn.Data;
            _stack = new Stack<RPN.Node>(5);
        }

        public RPN.Node Generate()
        {
            RPN.Token[] input = _data.Polish;

            //Convert all the PostFix information to Nodes[]
            RPN.Node[] nodes = new RPN.Node[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                nodes[i] = new RPN.Node()
                {
                    Children = null,
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
                            children.Add( _stack.Pop() );
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

            return _stack.Pop();
        }

        private void Write(string message)
        {
            Logger?.Invoke(this, message);
        }
    }
}
