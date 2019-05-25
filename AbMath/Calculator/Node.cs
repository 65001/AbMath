using System;
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
            public List<Node> Children;

            public void Replace(int identification, Node node)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    if (Children[i].ID == identification)
                    {
                        Children.RemoveAt(i);
                        Children.Insert(i, node);
                        return;
                    }
                }
            }

            public override string ToString()
            {
                return Token.Value;
            }

            public string GetHash()
            {
                return MD5(this.ToPostFix().Print());
            }

            public bool isLeaf => Children.Count == 0;
            public bool isRoot => Parent is null;

            public bool ChildrenAreIdentical()
            {
                if (Children.Count <= 1)
                {
                    return true;
                }

                for (int i = 1; i < Children.Count; i++)
                {
                    if (Children[i - 1].GetHash() != Children[i].GetHash())
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
                    sb.Append(hash[i].ToString("X2"));
                }

                return sb.ToString();
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
                if (node.Children.Count == 2 && node.Token.IsOperator())
                {
                    PostFix(node.Children[1], polish);
                    PostFix(node.Children[0], polish);
                    polish.Add(node.Token);
                    return;
                }

                //Operators that only have one child
                if (node.Children.Count == 1 && node.Token.IsOperator())
                {
                    PostFix(node.Children[0], polish);
                    polish.Add(node.Token);
                    return;
                }

                //Functions
                if (node.Children.Count > 0 && node.Token.IsFunction())
                {
                    for (int i = (node.Children.Count - 1); i >= 0; i--)
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
                if (node.Children.Count == 2 && node.Token.IsOperator())
                {
                    Infix(node.Children[1], infix);
                    infix.Append(node.Token);
                    Infix(node.Children[0], infix);
                    return;
                }

                //Operators that only have one child
                if (node.Children.Count == 1 && node.Token.IsOperator())
                {
                    infix.Append(node.Token);
                    Infix(node.Children[0], infix);
                    return;
                }

                //Functions
                //Functions
                if (node.Children.Count > 0 && node.Token.IsFunction())
                {
                    infix.Append(node.Token.Value);
                    infix.Append("(");
                    for (int i = (node.Children.Count - 1); i >= 0; i--)
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
