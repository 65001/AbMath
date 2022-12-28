using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using AbMath.Calculator.Operators;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public class Node : IComparable<Node>
        {
            public int ID;
            public Token Token;
            public Node Parent;

            public List<Node> Children => _children;
            private List<Node> _children;

            private MD5 _md5;

            private static object myLock = new object();
            private static int counter = 0;

            public Node(Node[] children, Token token)
            {
                this._children = new List<Node>();
                AssignChildren(children);
                this.ID = NextCounter();
                Parent = null;
                Token = token;
            }

            public Node(double number)
            {
                _children = new List<Node>(0);
                this.ID = NextCounter();
                Parent = null;
                Token = new RPN.Token(number);
            }

            public Node(Token token)
            {
                _children = new List<Node>(0);
                this.ID = NextCounter();
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

            public static Node Generate(RPN.Token[] input)
            {
                Stack<RPN.Node> stack = new Stack<RPN.Node>(5);

                foreach (Token token in input)
                {
                    RPN.Node node;

                    switch (token.Value)
                    {
                        case "+":
                            RPN.Node right = stack.Pop();
                            RPN.Node left = stack.Pop();
                            node = new Add( left, right);
                            break;
                        default:
                            node = new RPN.Node(token);
                            if (node.IsOperator() || node.IsFunction())
                            {
                                //Due to the nature of PostFix we know that all children
                                //of a function or operator have already been processed before this point
                                //this ensures we do not have any overflows or exceptions.
                                RPN.Node[] range = new RPN.Node[node.Token.Arguments];
                                for (int j = 0; j < node.Token.Arguments; j++)
                                {
                                    range[j] = stack.Pop();
                                }
                                node.AddChild(range);
                            }
                            break;
                    }

                    
                    
                    stack.Push(node); //Push new tree into the stack 
                }

                return stack.Pop();
            }

            public Node Clone()
            {
                return Generate(this.ToPostFix().ToArray());
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
                for (int i = 0; i < _children.Count; i++)
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
                    this[i].Replace(identification, node);
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

                this.ID = NextCounter();
                this.Token.Value = number.ToString();
            }

            /// <summary>
            /// Replaces the token of the current node 
            /// and changes its number.
            /// </summary>
            /// <param name="token"></param>
            public void Replace(RPN.Token token)
            {
                this.ID = NextCounter();
                this.Token = token;
            }

            /// <summary>
            /// Modifies the token value of the Node and 
            /// changes the ID of the node. 
            /// </summary>
            /// <param name="token"></param>
            public void Replace(string token)
            {
                this.ID = NextCounter();
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
                Token = null;
                _children.Clear();
            }

            public override string ToString()
            {
                return this.ToInfix();
            }

            public string GetHash()
            {
                return MD5(this.ToPostFix().Print());
            }

            public bool isLeaf => Children.Count == 0;
            public bool isRoot => Parent is null;

            public RPN.Node getRoot()
            {
                if (isRoot)
                {
                    return this;
                }
                return this.Parent.getRoot();
            }

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

            public Function? GetFunction(DataStore data)
            {
                if (!IsFunction())
                {
                    return null;
                }

                return data.Functions[this.Token.Value];
            }

            public Operator? GetOperator(DataStore data)
            {
                if (!IsOperator())
                {
                    return null;
                }

                return data.Operators[this.Token.Value];
            }

            public bool ContainsDomainViolation()
            {
                return Contains(new RPN.Token("/", 2, RPN.Type.Operator));
            }

            public bool Contains(RPN.Node node)
            {
                return Contains(node.Token);
            }

            public bool Contains(RPN.Token token)
            {
                return this.ToPostFix().Contains(token);
            }

            public bool IsNumberOrConstant()
            {
                return Token.IsNumber() || Token.IsConstant();
            }

            public bool IsScalar()
            {
                return IsNumberOrConstant() || IsVariable() ||  (IsFunction() && !this.ToInfix().Contains("list") &&
                                                !this.ToInfix().Contains("matrix"));
            }

            public bool IsNumber()
            {
                return Token.IsNumber();
            }

            public bool IsNumber(double number)
            {
                if (!Token.IsNumber())
                {
                    return false;
                }
                double value = double.Parse(Token.Value);
                bool same = value == number;
                return same;
            }

            public bool IsInteger()
            {
                return IsNumber() && ((int)GetNumber()) == GetNumber();
            }

            public bool IsInteger(int number)
            {
                return IsInteger() && GetNumber() == number;
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

            public bool IsVariable(Node node)
            {
                return IsVariable(node.Token.Value);
            }

            public bool IsVariable(string variable)
            {
                return Token.IsVariable() && Token.Value == variable;
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

            public string ToInfix(RPN.DataStore? data = null)
            {
                return ToInfix(this, data);
            }

            public string ToInfix(RPN.Node node, DataStore? data)
            {
                StringBuilder infix = new StringBuilder();
                Infix(node, infix, data);
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
            private void Infix(RPN.Node node, StringBuilder infix, RPN.DataStore? data)
            {
                //TODO: Implement nonrecursive algorithim!
                if (node is null)
                {
                    return;
                }

                //Rules from https://stackoverflow.com/questions/14175177/how-to-walk-binary-abstract-syntax-tree-to-generate-infix-notation-with-minimall

                bool parenthesis = true;
                if (node.Parent == null)
                {
                    parenthesis = false;
                }
                else if (data != null && node.IsOperator() && node.Parent.IsOperator() && data.Operators[node.Token.Value].Weight >
                         data.Operators[node.Parent.Token.Value].Weight)
                {
                    parenthesis = false;
                }
                else if (node.IsOperator("+") && node.Parent.IsOperator("+"))
                {
                    parenthesis = false;
                }
                else if (node.IsOperator("*") && node.Parent.IsOperator("*"))
                {
                    parenthesis = false;
                }
                else if (node.IsOperator() && node.Parent.IsFunction() && !node.Parent.IsConstant())
                {
                    parenthesis = false; //abs(f(x) + g(x))
                }
                //TODO: Correctly Implement Associative rules

                //Operators with left and right
                
                if (node.Children.Count == 2 && node.Token.IsOperator())
                {
                    if (parenthesis) { 
                        infix.Append("(");
                    }

                    bool printOperator = true;

                    if (node.IsOperator("*"))
                    {
                        if (node[0].IsFunction() && node[1].IsFunction())
                        {
                            printOperator = false; //sin(x)cos(x)
                        }
                        else if (node[0].IsNumber() && node[1].IsFunction())
                        {
                            printOperator = false; //2sin(x)
                        }
                        else if (node[0].IsFunction() && node[1].IsNumber())
                        {
                            printOperator = false; //sin(x)2
                        }
                    }

                    Infix(node[1], infix, data);


                    if (printOperator)
                    {
                        infix.Append(node.Token.Value); //The operator 
                    }

                    Infix(node[0], infix, data);

                    if (parenthesis)
                    {
                        infix.Append(")");
                    }

                    return;
                }
                

                //Operators that only have one child
                if (node.Children.Count == 1 && node.Token.IsOperator())
                {
                    if (node.IsOperator("!"))
                    {
                        infix.Append(node[0].ToInfix());
                        infix.Append(node.Token.Value);
                        return;
                    }

                    infix.Append(node.Token.Value);
                    Infix(node[0], infix, data);
                    return;
                }

                //Functions that have at least one child
                

                if (node.Children.Count > 0 && node.Token.IsFunction())
                {
                    if (node.IsFunction("list") || node.IsFunction("matrix"))
                    {
                        infix.Append("{");
                    }
                    else
                    {
                        infix.Append(node.Token.Value);
                        infix.Append("(");
                    }

                    for (int i = (node.Children.Count - 1); i >= 0; i--)
                    {
                        Infix(node.Children[i], infix, data);
                        if (i > 0)
                        {
                            infix.Append(",");
                        }
                    }

                    if (node.IsFunction("list") || node.IsFunction("matrix"))
                    {
                        infix.Append("}");
                    }
                    else
                    {
                        infix.Append(")");
                    }

                    return;
                }

                //Number, Variable, or constant function
                infix.Append(node.Token.Value);
            }

            public static void ResetCounter()
            {
                MutateCounter(0);
            }

            public static int NextCounter()
            {
                lock (myLock)
                {
                    counter++;
                }
                return counter;
            }

            private static void MutateCounter(int value)
            {
                lock (myLock)
                {
                    counter = value;
                }
            }

            public override bool Equals(Object obj)
            {
                if (obj == null || this.GetType() != obj.GetType())
                {
                    return false;
                }

                return this.GetHash() == ((Node) (obj)).GetHash();
            }

            public override int GetHashCode()
            {
                return ToPostFix().Print().GetHashCode();
            }

            /// <summary>
            /// Compares two nodes to each other.
            /// The value it returns is dependent
            /// on its parent and the structure of the tree.
            /// 
            /// It is expected that two nodes will share a same
            /// parent when being compared. 
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public int CompareTo(Node other)
            {
                //Assume that if you have no parent you really cannot sort.
                if (other.Parent == null || this.Parent == null)
                {
                    return 0;
                }

                //If we have different parents we cannot be sorted! 
                if (other.Parent.ID != this.Parent.ID)
                {
                    return 0;
                }

                /**
                 * Map
                 * 0 -> other
                 * 1 -> this
                 */

                //Here we know we have the same parent and that parent exists! 
                if (this.Parent.IsAddition() || this.Parent.IsFunction("internal_sum") || this.Parent.IsFunction("total"))
                {
                    //Numbers and constants should yield to everything
                    if (this.IsNumberOrConstant() && !other.IsNumberOrConstant())
                    {
                        return -1;
                    }

                    if (!this.IsNumberOrConstant() && other.IsNumberOrConstant())
                    {
                        return 1;
                    }

                    if (this.IsNumberOrConstant() && other.IsNumberOrConstant())
                    {
                        return this.GetNumber().CompareTo(other.GetNumber());
                    }

                    //Something that can be solved should yield like constants and numbers do! 
                    if (this.IsSolveable() && !other.IsSolveable())
                    {
                        return -1;
                    }

                    if (!this.IsSolveable() && other.IsSolveable())
                    {
                        return 1;
                    }

                    if (this.IsSolveable() && other.IsSolveable())
                    {
                        return 0;
                    }

                    //Single Variables should yield to other expressions 
                    if (this.IsVariable() && !other.IsVariable())
                    {
                        return -1;
                    }

                    if (!this.IsVariable() && other.IsVariable())
                    {
                        return 1;
                    }

                    if (this.IsVariable() && other.IsVariable())
                    {
                        return 0;
                    }

                    //Multiplication should yield to exponents
                    if (this.IsMultiplication() && other.IsExponent())
                    {
                        return -1;
                    }
                    if (this.IsExponent() && other.IsMultiplication())
                    {
                        return 1;
                    }

                    //Multiplication should yield to multiplications that contain exponents
                    if (this.IsMultiplication() && other.IsMultiplication())
                    { 
                        //This does not have an exponent and the other one does
                        if (!this.Children.Any(n => n.IsExponent()) && other.Children.Any(n => n.IsExponent()))
                        {
                            return -1;
                        }
                        else if (!other.Children.Any(n => n.IsExponent()) && this.Children.Any(n => n.IsExponent()))
                        {
                            return 1;
                        }
                    }
                    //Exponents compared to other exponents
                    if (this.IsExponent() && other.IsExponent())
                    {
                        //Constant powers should yield to non constant powers

                        //Constant powers should yield depending on their value! 
                        if (this[0].IsNumberOrConstant() && other[0].IsNumberOrConstant())
                        {
                            return this[0].GetNumber().CompareTo(other[0].GetNumber());
                        }
                    }

                    //TODO: A straight exponent should give way to a multiplication with an exponent if...
                    //TODO: Swapping exponent with non exponent
                }
                else if (this.Parent.IsMultiplication() || this.Parent.IsFunction("internal_product"))
                {
                    //Sort order for multiplication
                    //1) Numbers or Constants
                    //2) Exponents of constants
                    //3) Exponents of variables
                    //4) Variables
                    //5) Functions (sorted alphabetically)
                    //6) Expressions (Everything else)
                }

                return 0;
            }
        }
    }
}