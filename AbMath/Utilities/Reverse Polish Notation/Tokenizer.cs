using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using AbMath.CLITables;

namespace AbMath.Utilities
{
    public partial class RPN
    {
        public class Tokenizer : ITokenizer<Term>
        {
            private Data Data;
            private string Equation { get { return Data.Equation; } }

            private string Token;
            private string Character;
            private string PrevToken;
            private string ReadAhead;
            private Tables tables;

            private List<Term> Tokens;
            private string Rule;

           public event EventHandler<string> Logger;

            public Tokenizer(Data data)
            {
                Data = data;
            }

            public List<Term> Tokenize()
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();
                Tokens = new List<Term>();

                tables = new Tables(new Config { Title = "Tokenizer" });
                tables.Add(new Schema { Column="#",Width=3 });
                tables.Add(new Schema { Column = "Character", Width = 10 });
                tables.Add(new Schema { Column = "Token", Width = 15 });
                tables.Add(new Schema { Column = "# Tokens", Width = 11 });
                tables.Add(new Schema { Column = "Action", Width = 16 });
                Write(tables.GenerateHeaders());

                Token = string.Empty;
                int Length = Equation.Length;

                for (int i = 0; i < Length; i++)
                {
                    Character = Equation.Substring(i, 1);
                    PrevToken = Tokens.LastOrDefault().Value;
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
                    else if ((i == 0 && Data.IsUniary(Character)) || (Tokens.Count > 0 && (Data.IsOperator(PrevToken) || Data.IsLeftBracket(PrevToken)) && Data.IsUniary(Character) && Data.IsNumber(Token) == false && 
                        Data.IsOperator(Character + ReadAhead) == false))
                    {
                        Rule = "Uniary";
                        Token += Character;
                    }
                    else if ( ( Data.IsNumber(Token) ) && ( Data.IsVariable(Character) || Data.IsLeftBracket(Character)))
                    {
                        WriteToken("Left Implicit");
                        Token = Character;
                        if (Data.IsLeftBracket(Character) || (i == (Equation.Length - 1)))
                        {
                            WriteToken("Left Implicit");
                        }
                    }
                    else if (Data.IsVariable(Token) && Data.IsNumber(Character))
                    {
                        WriteToken("Left Implicit 2");
                        Token = Character;
                        if (Data.IsLeftBracket(Character) || (i == (Equation.Length - 1)))
                        {
                            WriteToken("Left Implicit 2");
                        }
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
                    else if (Data.IsOperator(Character + ReadAhead))
                    {
                        WriteToken("Operator");
                        Token = Character + ReadAhead;
                        WriteToken("Operator");
                        i = i + 1;
                    }
                    else if (Data.IsNumber(Token) && (Data.IsLeftBracket(Character) || Data.IsRightBracket(Character) || Data.IsOperator(Character)))
                    {
                        WriteToken("Edge Case 1");
                        Token = Character;
                        WriteToken("Edge Case 1");
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
                        Rule = "Append";
                        Token += Character;
                    }
                    tables.Add(new string[] { i.ToString(), Character, Token, Tokens.Count.ToString(), Rule });
                    Write(tables.GenerateNextRow());
                }
                Write(tables.GenerateFooter());

                if (tables.SuggestedRedraw)
                {
                    Write(tables.Redraw());
                }
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
                //Resolve!!
                Term term = new Term
                {
                    Value = Token,
                    Type = Data.Resolve(Token),
                    Arguments = 0
                };

                if(term.Type == Type.Function)
                {
                    term.Arguments = Data.Functions[Token].Arguments;
                }
                else if (term.Type == Type.Operator)
                {
                    term.Arguments = Data.Operators[Token].Arguments;
                }

                Tokens.Add(term);
                Token = string.Empty;
            }

            void Write(string Message)
            {
                Logger?.Invoke(this, Message);
            }
        }
    }
}
