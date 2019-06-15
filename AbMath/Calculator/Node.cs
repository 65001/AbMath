﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public class Node
        {
            public int ID;
            public Token Token;
            public Node Parent;
            public Node[] Children;

            public Node()
            {
            }

            public Node(int ID, Node[] children, Token token)
            {
                AssignChildren(children);
                this.ID = ID;
                Parent = null;
                Token = token;
            }

            public Node(int ID, double number)
            {
                Children = new RPN.Node[0];
                this.ID = ID;
                Parent = null;
                Token = new RPN.Token(number);
            }

            public Node(int ID, Token token)
            {
                Children = new RPN.Node[0];
                this.ID = ID;
                Parent = null;
                Token = token;
            }

            public void Replace(Node node, Node replacement)
            {
                Replace(node.ID, replacement);
            }

            public void Replace(int identification, Node node)
            {
                for (int i = 0; i < Children.Length; i++)
                {
                    if (Children[i].ID == identification)
                    {
                        node.Parent = this;
                        Children[i] = node;
                        return;
                    }
                }

                //Propagate down the tree
                for (int i = 0; i < Children.Length; i++)
                {
                    Children[i].Replace(identification, node);
                }
            }

            public void Remove()
            {
                if (Children.Length > 0)
                {
                    throw new InvalidOperationException("This node has more than one children.");
                }
                Remove(Children[0]);
            }

            public void Remove(Node replacement)
            {
                this.Parent.Replace(this, replacement);
                this.Delete();
            }

            public void Delete()
            {
                Parent = null;
                Children = new RPN.Node[0];
            }

            public override string ToString()
            {
                return Token.Value;
            }

            public string GetHash()
            {
                return MD5(this.ToPostFix().Print());
            }

            public bool isLeaf => Children.Length == 0;
            public bool isRoot => Parent is null;

            public bool ChildrenAreIdentical()
            {
                if (Children.Length <= 1)
                {
                    return true;
                }

                for (int i = 0; i < (Children.Length - 1); i++)
                {
                    if (Children[i + 1].GetHash() == Children[i].GetHash())
                    {

                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }

            private string MD5(string input)
            {
                // step 1, calculate MD5 hash from input
                MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);

                // step 2, convert byte array to hex string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }

                return sb.ToString();
            }

            private void AssignChildren(Node[] child)
            {
                if (this.Children == null || this.Children.Length < child.Length)
                {
                    Children = new Node[child.Length];
                }

                Children = child;

                //Ensures that all children understand that the current node is their parent
                for (int i = (child.Length - 1); i >= 0; i--)
                {
                    Children[i].Parent = this;
                }
            }

            public bool IsNumber()
            {
                return Token.IsNumber();
            }

            public bool IsFunction()
            {
                return Token.IsFunction();
            }

            public bool IsConstant()
            {
                return Token.IsConstant();
            }

            public bool IsOperator()
            {
                return Token.IsOperator();
            }

            public bool IsVariable()
            {
                return Token.IsVariable();
            }

            public bool IsAddition()
            {
                return Token.IsAddition();
            }

            public bool IsSubtraction()
            {
                return Token.IsSubtraction();
            }

            public bool IsDivision()
            {
                return Token.IsDivision();
            }

            public bool IsMultiplication()
            {
                return Token.IsMultiplication();
            }

            public bool IsExponent()
            {
                return Token.IsExponent();
            }

            /// <summary>
            /// A node is an expression if it is not 
            /// a variable, number, or constant. 
            /// </summary>
            /// <param name="node">node to test.</param>
            /// <returns></returns>
            public bool IsExpression()
            {
                return !(IsNumber() || IsVariable() || IsConstant());
            }

            /// <summary>
            /// If a node that is an operator or function is solveable 
            /// that means it is in effect a constant!
            /// </summary>
            /// <param name="node">node to test.</param>
            /// <returns></returns>
            public bool IsSolveable()
            {
                return this.Children.All(t => t.Token.Type == RPN.Type.Number || t.Token.IsConstant());
            }

            /// <summary>
            /// Returns the postfix representation
            /// of the entire abstract syntax tree.
            /// </summary>
            /// <returns></returns>
            public List<RPN.Token> ToPostFix()
            {
                return ToPostFix(this);
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

            public string ToInfix()
            {
                return ToInfix(this);
            }

            public string ToInfix(RPN.Node node)
            {
                StringBuilder infix = new StringBuilder();
                Infix(node, infix);
                return infix.ToString();
            }

            private void PostFix(RPN.Node node, List<RPN.Token> polish)
            {
                if (node is null)
                {
                    return;
                }

                //Operators with left and right
                if (node.Children.Length == 2 && node.Token.IsOperator())
                {
                    PostFix(node.Children[1], polish);
                    PostFix(node.Children[0], polish);
                    polish.Add(node.Token);
                    return;
                }

                //Operators that only have one child
                if (node.Children.Length == 1 && node.Token.IsOperator())
                {
                    PostFix(node.Children[0], polish);
                    polish.Add(node.Token);
                    return;
                }

                //Functions
                if (node.Children.Length > 0 && node.Token.IsFunction())
                {
                    for (int i = (node.Children.Length - 1); i >= 0; i--)
                    {
                        PostFix(node.Children[i], polish);
                    }

                    polish.Add(node.Token);
                    return;
                }

                //Number, Variable, or constant function
                polish.Add(node.Token);
            }

            private void Infix(RPN.Node node, StringBuilder infix)
            {
                if (node is null)
                {
                    return;
                }

                //Operators with left and right
                if (node.Children.Length == 2 && node.Token.IsOperator())
                {
                    infix.Append("(");
                    Infix(node.Children[1], infix);
                    infix.Append(node.Token);
                    Infix(node.Children[0], infix);
                    infix.Append(")");
                    return;
                }

                //Operators that only have one child
                if (node.Children.Length == 1 && node.Token.IsOperator())
                {
                    infix.Append(node.Token);
                    Infix(node.Children[0], infix);
                    return;
                }

                //Functions
                //Functions
                if (node.Children.Length > 0 && node.Token.IsFunction())
                {
                    infix.Append(node.Token.Value);
                    infix.Append("(");
                    for (int i = (node.Children.Length - 1); i >= 0; i--)
                    {
                        Infix(node.Children[i], infix);
                        if (i > 0)
                        {
                            infix.Append(",");
                        }
                    }

                    infix.Append(")");

                    return;
                }

                //Number, Variable, or constant function
                infix.Append(node.Token.Value);
            }
        }
    }
}