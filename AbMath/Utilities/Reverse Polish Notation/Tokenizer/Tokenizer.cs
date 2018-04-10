using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AbMath.Utilities
{
    public partial class RPN
    {
        public class Tokenizer : ITokenizer<string>
        {
            private Data Data;
            private string Equation;

            private string Token;
            private string Character;
            private string PrevToken;
            private string ReadAhead;

            private List<string> Tokens;
            private string Rule;

           public event EventHandler<string> Logger;

            public Tokenizer(Data data)
            {
                Data = data;
                Equation = data.Equation;
            }

            public List<string> Tokenize()
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();
                Tokens = new List<string>();
                Token = string.Empty;
                Write( $"┌{"".PadRight(68,'─')}┐");
                Write( $"│{"Tokenizer",28}{"",40}│");
                Write( $"├{"".PadRight(4, '─') }┬{"".PadRight(12, '─')}┬{"".PadRight(17, '─')}┬{"".PadRight(13, '─')}┬{"".PadRight(18, '─')}┤");
                Write( $"│{"#",-3} │ {"Character",-10} │ {"Token",-15} │ {"Tokens Count",-12}│ {"Action",-16} │");
                
                int Length = Equation.Length;

                for (int i = 0; i < Length; i++)
                {
                    Character = Equation.Substring(i, 1);
                    PrevToken = Tokens.LastOrDefault();
                    ReadAhead = string.Empty;
                    Rule = string.Empty;

                    if (i < (Length - 1))
                    {
                        ReadAhead = Equation.Substring((i + 1), 1);
                    }

                    Alias();

                    //WhiteSpace Rule
                    if (string.IsNullOrWhiteSpace(Character) && Character != ",")
                    {
                        WriteToken("WhiteSpace");
                    }
                    //Unary Input at the start of the input or after another operator or left parenthesis
                    else if ((i == 0 && Data.IsUniary(Character)) || (Tokens.Count > 0 && (Data.IsOperator(PrevToken) || Data.IsLeftBracket(PrevToken)) && Data.IsUniary(Character)))
                    {
                        Token += Character;
                    }
                    else if (Data.IsFunction(Token) && Data.IsLeftBracket(Character))
                    {
                        WriteToken("Function Start");
                        Token = Character;
                        WriteToken("Function Start");
                    }
                    else if (Data.IsFunction(Token) && Data.IsRightBracket(Character))
                    {
                        WriteToken("Function End");
                        Token = Character;
                        WriteToken("Function End");
                    }
                    else if (Data.IsFunction(Token) && Data.IsOperator(Character))
                    {
                        WriteToken("Function End");
                        Token = Character;
                        WriteToken("Function End");
                    }
                    else if (Data.IsNumber(Token) && (Data.IsLeftBracket(Character) || Data.IsRightBracket(Character) || Data.IsOperator(Character)))
                    {
                        WriteToken("Edge Case 1");
                        Token = Character;
                        WriteToken("Edge Case 1");
                    }
                    else if (Data.IsOperator(Character + ReadAhead))
                    {
                        WriteToken("Operator");
                        Token = Character + ReadAhead;
                        WriteToken("Operator");
                        i = i + 1;
                    }
                    //Add equivalent for variables?
                    else if (Data.IsVariable(Token) && (Data.IsLeftBracket(Character) || Data.IsRightBracket(Character) || Data.IsOperator(Character)))
                    {
                        WriteToken("Edge Case 2");
                        Token = Character;
                        WriteToken("Edge Case 2");
                    }
                    else if (Data.IsOperator(Character))
                    {
                        Token += Character;
                        WriteToken("Operator");
                    }
                    else if (Data.IsLeftBracket(Character) || Data.IsRightBracket(Character))
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
                    Write( $"│{i,-3} │ {Character,-10} │ {Token,-15} │ {Tokens.Count,-12}│ {Rule,-16} │");
                }

                Write($"└{"".PadRight(4, '─') }┴{"".PadRight(12, '─')}┴{"".PadRight(17, '─')}┴{"".PadRight(13, '─')}┴{"".PadRight(18, '─')}┘");
                SW.Stop();
                Write($"Execution Time {SW.ElapsedMilliseconds}(ms) Elappsed Ticks: {SW.ElapsedTicks}");
                Write("");

                return Tokens;
            }

            /// <summary>
            /// Transforms characters and tokens from mathematical notation into notation
            /// that AbMath understands.
            /// </summary>
            void Alias()
            {
                if (Data.Aliases.ContainsKey(Token))
                {
                    Token = Data.Aliases[Token];
                }
                else if (Data.Aliases.ContainsKey(Character))
                {
                    Character = Data.Aliases[Character];
                }
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

            void Write(string Message)
            {
                Logger?.Invoke(this, Message);
            }
        }
    }
}
