using System;
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

            public List<Node> Children => _children;
            private List<Node> _children;

            private MD5 _md5;
            private static int counter = 0;

            public Node(Node[] children, Token token)
            {
                this._children = new List<Node>();
                AssignChildren(children);
                this.ID = counter++;
                Parent = null;
                Token = token;
            }

            public Node(double number)
            {
                _children = new List<Node>(0);
                this.ID = counter++;
                Parent = null;
                Token = new RPN.Token(number);
            }

            public Node(Token token)
            {
                _children = new List<Node>(0);
                this.ID = counter++;
                Parent = null;
                Token = token;
            }

            public Node this[int i]
            {
                get => _children[i];
                set => _children[i] = value;
            }

            public Node this[int i, int j]
            {
                get => _children[i]._children[j];
                set => _children[i]._children[j] = value;
            }

            public Node this[int i, int j, int k]
            {
                get => _children[i]._children[j]._children[k];
                set => _children[i]._children[j]._children[k] = value;
            }


            /// <summary>
            /// Replaces in the tree the node with 
            /// the given replacement
            /// </summary>
            /// <param name="node">The node to look for</param>
            /// <param name="replacement">The replacement for the node</param>
            public void Replace(Node node, Node replacement)
            {
                Replace(node.ID, replacement);
            }

            /// <summary>
            /// Replaces in the tree the node with 
            /// the given replacement
            /// </summary>
            /// <param name="identification">The unique ID number the node posseses</param>
            /// <param name="node">The replacement</param>
            public void Replace(int identification, Node node)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    if (_children[i].ID == identification)
                    {
                        node.Parent = this;
                        _children[i] = node;
                        return;
                    }
                }


                //Propagate down the tree
                for (int i = 0; i < Children.Count; i++)
                {
                    Children[i].Replace(identification, node);
                }
            }

            /// <summary>
            /// If the current node is a number, 
            /// replaces the current number and 
            /// changes the ID of the node.
            /// Otherwise allocate a new token
            /// and change the ID of the node.
            /// </summary>
            /// <param name="number"></param>
            public void Replace(double number)
            {
                if (!this.IsNumber())
                {
                    Replace(new Token(number));
                    return;
                }

                this.ID = counter++;
                this.Token.Value = number.ToString();
            }

            /// <summary>
            /// Replaces the token of the current node 
            /// and changes its number.
            /// </summary>
            /// <param name="token"></param>
            public void Replace(RPN.Token token)
            {
                this.ID = counter++;
                this.Token = token;
            }

            /// <summary>
            /// Modifies the token value of the Node and 
            /// changes the ID of the node. 
            /// </summary>
            /// <param name="token"></param>
            public void Replace(string token)
            {
                this.ID = counter++;
                this.Token.Value = token;
            }

            public void Swap(RPN.Node index, RPN.Node index2)
            {
                _children.Swap( _children.IndexOf(index), _children.IndexOf(index2) );
            }

            public void Swap(int index, int index2)
            {
                _children.Swap(index, index2);
            }

            /// <summary>
            /// Removes the current node from the tree. 
            /// If it has only one child, that child will 
            /// replace it's position in the tree.
            /// </summary>
            public void Remove()
            {
                if (Children.Count > 1)
                {
                    throw new InvalidOperationException($"This node has {Children.Count} children.");
                }
                Remove(Children[0]);
            }

            /// <summary>
            /// Removes the current node from the tree and 
            /// replace it with the replacement node. 
            /// </summary>
            public void Remove(Node replacement)
            {
                this.Parent.Replace(this, replacement);
                this.Delete();
            }

            public void Delete()
            {
                Parent = null;
                _children.Clear();
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


            public void AddChild(RPN.Node node)
            {
                Spawn();
                _children.Add(node);
                node.Parent = this;

                if (Token.Arguments < _children.Count)
                {
                    Token.Arguments++;
                }
            }

            public void AddChild(List<RPN.Node> nodes)
            {
                Spawn();
                _children.AddRange(nodes);
                validateChildren();

                if (Token?.Arguments < _children.Count)
                {
                    Token.Arguments = _children.Count;
                }
            }

            public void AddChild(RPN.Node[] nodes)
            {
                Spawn();
                _children.AddRange(nodes);
                validateChildren();

                if (Token?.Arguments < _children.Count)
                {
                    Token.Arguments = _children.Count;
                }
            }

            public void RemoveChild(RPN.Node node)
            {
                _children.Remove(node);
            }

            public bool ChildrenAreIdentical()
            {
                if (Children.Count <= 1)
                {
                    return true;
                }

                for (int i = 0; i < (Children.Count - 1); i++)
                {
                    if(!Children[i].Matches(Children[i + 1]))
                    {
                        return false;
                    }
                }

                return true;
            }

            private string MD5(string input)
            {
                // step 1, calculate MD5 hash from input
                if (_md5 == null)
                {
                    _md5 = System.Security.Cryptography.MD5.Create();
                }

                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hash = _md5.ComputeHash(inputBytes);

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
                Spawn();
                AddChild(child);
            }

            private void Spawn()
            {
                if (_children == null || _children.Count == 0)
                {
                    _children = new List<Node>();
                }
            }

            private void validateChildren()
            {
                //Ensures that all children understand that the current node is their parent
                for (int i = (_children.Count - 1); i >= 0; i--)
                {
                    _children[i].Parent = this;
                }
            }

            /// <summary>
            /// Generates a list of all the descendants of this node
            /// and their descendants and so forth.
            /// </summary>
            /// <returns>The current node and all its descendants</returns>
            private List<Node> GetAllDescendants()
            {
                List<Node> results = new List<Node>();
                Queue<Node> unvisited = new Queue<Node>();

                //we should attempt to do this without using recursion for performance reasons
                //we first need to add the current node to the unvisited node tree
                unvisited.Enqueue(this);
                while (unvisited.Count > 0)
                {
                    Node temp = unvisited.Dequeue();
                    results.Add(temp);

                    for (int i = (temp.Children.Count - 1); i >= 0; i--)
                    {
                        unvisited.Enqueue(temp.Children[i]);
                    }
                }

                return results;
            }

            /// <summary>
            /// Returns true if two nodes are exactly alike and false otherwise.
            /// </summary>
            /// <param name="branch">The other node</param>
            public bool Matches(Node branch)
            {
                Queue<Node> home = new Queue<Node>();
                Queue<Node> foreign = new Queue<Node>();

                home.Enqueue(this);
                foreign.Enqueue(branch);

                while (home.Count > 0)
                {
                    RPN.Node peter = home.Dequeue();
                    RPN.Node pan = foreign.Dequeue();

                    //if the contents of the token 
                    //or the number of children this branch has do not match by definition they cannot be the same
                    if (peter.Token.Value != pan.Token.Value || peter.Children.Count != pan.Children.Count)
                    {
                        return false;
                    }

                    for (int i = (peter.Children.Count - 1); i >= 0; i--)
                    {
                        home.Enqueue(peter.Children[i]);
                    }

                    for (int i = (pan.Children.Count - 1); i >= 0; i--)
                    {
                        foreign.Enqueue(pan.Children[i]);
                    }
                }


                return true;
            }

            public double GetNumber()
            {
                return double.Parse(Token.Value);
            }

            public bool IsNumberOrConstant()
            {
                return Token.IsNumber() || Token.IsConstant();
            }

            public bool IsNumber()
            {
                return Token.IsNumber();
            }

            public bool IsNumber(double number)
            {
                return Token.IsNumber() && double.Parse(Token.Value) == number;
            }

            public bool IsLessThanNumber(double number)
            {
                return Token.IsNumber() && double.Parse(Token.Value) < number;
            }

            public bool IsLessThanOrEqualToNumber(double number)
            {
                return Token.IsNumber() && double.Parse(Token.Value) <= number;
            }

            public bool IsGreaterThanNumber(double number)
            {
                return Token.IsNumber() && double.Parse(Token.Value) > number;
            }

            public bool IsGreaterThanOrEqualToNumber(double number)
            {
                return Token.IsNumber() && double.Parse(Token.Value) >= number;
            }

            public bool IsFunction()
            {
                return Token.IsFunction();
            }

            public bool IsFunction(string function)
            {
                return Token.IsFunction() && Token.Value == function;
            }

            public bool IsConstant()
            {
                return Token.IsConstant();
            }

            public bool IsConstant(string constant)
            {
                return Token.IsConstant() && Token.Value == constant;
            }

            public bool IsOperator()
            {
                return Token.IsOperator();
            }

            public bool IsOperator(string op)
            {
                return Token.IsOperator() && Token.Value == op;
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

            public bool IsLog()
            {
                return Token.IsLog();
            }

            public bool IsLn()
            {
                return Token.IsLn();
            }

            public bool IsSqrt()
            {
                return Token.IsSqrt();
            }

            public bool IsAbs()
            {
                return Token.IsAbs();
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
                return this.Children.All(t => t.Token.Type == Type.Number || t.Token.IsConstant());
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
                return PostFix(node);
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

            public List<Node> ToPrefix()
            {
                return GetAllDescendants();
            }

            /// <summary>
            /// PostOrder traversal algorithim implementation
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            private List<RPN.Token> PostFix(RPN.Node node)
            {
                //https://www.geeksforgeeks.org/iterative-postorder-traversal/
                Stack<RPN.Node> first = new Stack<Node>();
                Stack<Token> second = new Stack<Token>();
                first.Push(node);

                while (first.Count > 0)
                {
                    RPN.Node temp = first.Pop();
                    second.Push(temp.Token);

                    for (int i = (temp.Children.Count - 1); i >= 0 ; i--)
                    {
                        first.Push(temp.Children[i]);
                    }
                }

                List<RPN.Token> tokens = new List<Token>();

                while (second.Count > 0)
                {
                    tokens.Add(second.Pop());
                }

                return tokens;
            }

            /// <summary>
            /// Inorder traversal algorithim
            /// </summary>
            /// <param name="node"></param>
            /// <param name="infix"></param>
            private void Infix(RPN.Node node, StringBuilder infix)
            {
                //TODO: Implement nonrecursive algorithim!
                if (node is null)
                {
                    return;
                }

                //Operators with left and right
                if (node.Children.Count == 2 && node.Token.IsOperator())
                {
                    infix.Append("(");
                    Infix(node.Children[1], infix);
                    infix.Append(node.Token.Value);
                    Infix(node.Children[0], infix);
                    infix.Append(")");
                    return;
                }

                //Operators that only have one child
                if (node.Children.Count == 1 && node.Token.IsOperator())
                {
                    infix.Append(node.Token.Value);
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

            public static void ResetCounter()
            {
                counter = 0;
            }
        }
    }
}