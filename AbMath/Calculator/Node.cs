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
                StringBuilder md5 = new StringBuilder();
                md5.Append(Token.Value);

                //Recursively find the trees values
                for (int i = (Children.Count - 1); i >= 0; i--)
                {
                    md5.Append(Children[i].GetHash());
                }

                return MD5(md5.ToString());
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
        }
    }
}
