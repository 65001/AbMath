using System;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public class Token
        {
            public string Value;
            public int Arguments;
            public Type Type;

            public Token()
            {

            }

            public Token(double number)
            {
                Value = number.ToString();
                Arguments = 0;
                Type = Type.Number;
            }

            public Token(string value, int arguments, Type type)
            {
                Value = value;
                Arguments = arguments;
                Type = type;
            }

            

            public bool IsNumber()
            {
                return Type == Type.Number;
            }

            public bool IsFunction()
            {
                return Type == Type.Function;
            }

            public bool IsConstant()
            {
                return IsFunction() && Arguments == 0;
            }

            public bool IsOperator()
            {
                return Type == Type.Operator;
            }

            public bool IsVariable()
            {
                return Type == Type.Variable;
            }

            public bool IsLeftBracket()
            {
                return Type == Type.LParen;
            }

            public bool IsRightBracket()
            {
                return Type == Type.RParen;
            }

            public bool IsComma()
            {
                return Value == ",";
            }

            public bool IsAddition()
            {
                return Value == "+";
            }

            public bool IsSubtraction()
            {
                return Value == "-";
            }

            public bool IsDivision()
            {
                return Value == "/";
            }

            public bool IsMultiplication()
            {
                return Value == "*";
            }

            public bool IsExponent()
            {
                return Value == "^";
            }

            public bool IsLog()
            {
                return Value == "log";
            }

            public bool IsLn()
            {
                return Value == "ln";
            }

            public bool IsSqrt()
            {
                return Value == "sqrt";
            }

            public bool IsAbs()
            {
                return Value == "abs";
            }

            public override string ToString()
            {
                return Value;
            }

            public override bool Equals(Object obj)
            {
                if (obj == null)
                {
                    return false;
                }

                if (obj == this)
                {
                    return true;
                }

                if (obj.GetType() != this.GetType())
                {
                    return false;
                }

                Token token = (Token) obj;
                return this.Type == token.Type && this.Arguments == token.Arguments && this.Value == token.Value;
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 23 + Type.GetHashCode();
                hash = hash * 23 + Arguments.GetHashCode();
                hash = hash * 23 + Value.GetHashCode();
                return hash; 
            }
        }
    }
}
