using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AbMath.Utilities
{
    public partial class RPN
    {
        public class Tokenizer
        {
            private RPN RPN;
            private string Equation;
            private string Token;
            private List<string> Tokens;
            private string Rule;

            public Tokenizer(RPN _RPN)
            {
                RPN = _RPN;
                Equation = RPN.Equation;
            }

            public List<string> Tokenize()
            {
                Tokens = new List<string>();
                Token = string.Empty;
                RPN.Logger?.Invoke(this, $"┌{"".PadRight(68,'─')}┐");
                RPN.Logger?.Invoke(this, $"│{"Tokenizer",28}{"",40}│");
                RPN.Logger?.Invoke(this, $"├{"".PadRight(4, '─') }┬{"".PadRight(12, '─')}┬{"".PadRight(17, '─')}┬{"".PadRight(13, '─')}┬{"".PadRight(18, '─')}┤");
                RPN.Logger?.Invoke(this, $"│{"#",-3} │ {"Character",-10} │ {"Token",-15} │ {"Tokens Count",-12}│ {"Action",-16} │");
                //RPN.Logger?.Invoke(this,  "│#   │ Character  │ Token             │ Tokens Count│");
                int Length = Equation.Length;

                for (int i = 0; i < Length; i++)
                {
                    string Character = Equation.Substring(i, 1);
                    string PrevToken = Tokens.LastOrDefault();
                    string ReadAhead = string.Empty;
                    Rule = string.Empty;

                    if (i < (Length - 1))
                    {
                        ReadAhead = Equation.Substring((i + 1), 1);
                    }

                    //WhiteSpace Rule
                    if (string.IsNullOrWhiteSpace(Character) && Character != ",")
                    {
                        WriteToken("WhiteSpace");
                    }
                    //Unary Input at the start of the input or after another operator or left parenthesis
                    else if ((i == 0 && RPN.IsUniary(Character)) || (Tokens.Count > 0 && (RPN.IsOperator(PrevToken) || RPN.IsLeftBracket(PrevToken)) && RPN.IsUniary(Character)))
                    {
                        Token += Character;
                    }
                    else if (RPN.IsFunction(Token) && RPN.IsLeftBracket(Character))
                    {
                        WriteToken("Function Start");
                        Token = Character;
                        WriteToken("Function Start");
                    }
                    else if (RPN.IsFunction(Token) && RPN.IsRightBracket(Character))
                    {
                        WriteToken("Function End");
                        Token = Character;
                        WriteToken("Function End");
                    }
                    else if (RPN.IsFunction(Token) && RPN.IsOperator(Character))
                    {
                        WriteToken("Function End");
                        Token = Character;
                        WriteToken("Function End");
                    }
                    else if (RPN.IsNumber(Token) && (RPN.IsLeftBracket(Character) || RPN.IsRightBracket(Character) || RPN.IsOperator(Character)))
                    {
                        WriteToken("Edge Case 1");
                        Token = Character;
                        WriteToken("Edge Case 1");
                    }
                    else if (RPN.IsOperator(Character + ReadAhead))
                    {
                        WriteToken("Operator");
                        Token = Character + ReadAhead;
                        WriteToken("Operator");
                        i = i + 1;
                    }
                    else if (RPN.IsOperator(Character))
                    {
                        Token += Character;
                        WriteToken("Operator");
                    }
                    else if (RPN.IsLeftBracket(Character) || RPN.IsRightBracket(Character))
                    {
                        Token += Character;
                        WriteToken("Bracket");
                    }
                    else if (i == (Equation.Length - 1))
                    {
                        Token += Character;
                        WriteToken("End of String");
                    }
                    else
                    {
                        Token += Character;
                    }
                    RPN.Logger?.Invoke(this, $"│{i,-3} │ {Character,-10} │ {Token,-15} │ {Tokens.Count,-12}│ {Rule,-16} │");
                }

                RPN.Logger?.Invoke(this, $"└{"".PadRight(4, '─') }┴{"".PadRight(12, '─')}┴{"".PadRight(17, '─')}┴{"".PadRight(13, '─')}┴{"".PadRight(18, '─')}┘");

                RPN.Logger?.Invoke(this, "");
                return Tokens;
            }

            void WriteToken(string _Rule)
            {
                if (string.IsNullOrWhiteSpace(Token) == true && Token != ",")
                {
                    return;
                }

                Rule = _Rule;
                Tokens.Add(Token);
                Token = string.Empty;
            }
        }
    }
}
