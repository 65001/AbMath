using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CLI;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public class Tokenizer : ITokenizer<Term>
        {
            private readonly DataStore _dataStore;
            private string Equation => _dataStore.Equation; 

            private string Token;
            private string Character;
            private string PrevToken;
            private string ReadAhead;
            private Tables<string> tables;

            private List<Term> Tokens;
            private string Rule;

           public event EventHandler<string> Logger;

            public Tokenizer(DataStore dataStore)
            {
                _dataStore = dataStore;
            }

            public List<Term> Tokenize()
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();
                Tokens = new List<Term>();

                tables = new Tables<string>(new Config { Title = "Tokenizer"});
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
                    else if ((i == 0 && _dataStore.IsUnary(Character)) || (Tokens.Count > 0 && (_dataStore.IsOperator(PrevToken) || _dataStore.IsLeftBracket(PrevToken) || PrevToken ==",") && _dataStore.IsUnary(Character) && !_dataStore.IsNumber(Token) && 
                        !_dataStore.IsOperator(Character + ReadAhead)))
                    {
                        Rule = "Unary";
                        Token += Character;
                        if(!string.IsNullOrWhiteSpace(ReadAhead) && (_dataStore.IsVariable(ReadAhead) || _dataStore.IsLeftBracket(ReadAhead) ))
                        {
                            Token += "1";
                            WriteToken("Unary");
                        }
                    }
                    else if ( ( _dataStore.IsNumber(Token) ) && ( _dataStore.IsVariable(Character) || _dataStore.IsLeftBracket(Character) || _dataStore.IsFunction(Character)))
                    {
                        WriteToken("Left Implicit");
                        Token = Character;
                        if (_dataStore.IsLeftBracket(Character) || (i == (Equation.Length - 1)))
                        {
                            WriteToken("Left Implicit");
                        }
                    }
                    else if (_dataStore.IsVariable(Token) && _dataStore.IsNumber(Character))
                    {
                        WriteToken("Left Implicit 2");
                        Token = Character;
                        if (_dataStore.IsLeftBracket(Character) || (i == (Equation.Length - 1)))
                        {
                            WriteToken("Left Implicit 2");
                        }
                    }
                    else if (_dataStore.IsFunction(Token) && _dataStore.IsLeftBracket(Character))
                    {
                        WriteToken("Function Start");
                        Token = Character;
                        WriteToken("Function Start");
                    }
                    else if (_dataStore.IsFunction(Token) && _dataStore.IsRightBracket(Character))
                    {
                        WriteToken("Function End");
                        Token = Character;
                        WriteToken("Function End");
                    }
                    else if (_dataStore.IsFunction(Token) && _dataStore.IsOperator(Character))
                    {
                        WriteToken("Function End");
                        Token = Character;
                        WriteToken("Function End");
                    }
                    else if (_dataStore.IsOperator(Character + ReadAhead))
                    {
                        WriteToken("Operator");
                        Token = Character + ReadAhead;
                        WriteToken("Operator");
                        i = i + 1;
                    }
                    else if (_dataStore.IsNumber(Token) && (_dataStore.IsLeftBracket(Character) || _dataStore.IsRightBracket(Character) || _dataStore.IsOperator(Character)))
                    {
                        WriteToken("Edge Case 1");
                        Token = Character;
                        WriteToken("Edge Case 1");
                    }
                    //Add equivalent for variables?
                    else if (_dataStore.IsVariable(Token) && (_dataStore.IsLeftBracket(Character) || _dataStore.IsRightBracket(Character) || _dataStore.IsOperator(Character)))
                    {
                        WriteToken("Edge Case 2");
                        Token = Character;
                        WriteToken("Edge Case 2");
                    }
                    else if (_dataStore.IsOperator(Character))
                    {
                        Token += Character;
                        WriteToken("Operator");
                    }
                    else if (_dataStore.IsLeftBracket(Character) || _dataStore.IsRightBracket(Character))
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
                if (_dataStore.Aliases.ContainsKey(Token))
                {
                    Token = _dataStore.Aliases[Token];
                }
                else if (_dataStore.Aliases.ContainsKey(Character))
                {
                    Character = _dataStore.Aliases[Character];
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
                    Type = _dataStore.Resolve(Token),
                    Arguments = 0
                };

                if(term.Type == Type.Function)
                {
                    term.Arguments = _dataStore.Functions[Token].Arguments;
                }
                else if (term.Type == Type.Operator)
                {
                    term.Arguments = _dataStore.Operators[Token].Arguments;
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
