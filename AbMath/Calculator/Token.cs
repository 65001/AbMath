using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public class Token
        {
            public string Value;
            public int Arguments;
            public Type Type;

            public override string ToString()
            {
                return Value;
            }

            public bool IsNumber()
            {
                return Type == Type.Number;
            }

            public bool IsNull()
            {
                return Type == Type.Null;
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
        }
    }
}
