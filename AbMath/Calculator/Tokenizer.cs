using System;
using System.Collections.Generic;
using System.Diagnostics;
using CLI;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public class Tokenizer : ITokenizer<Token>
        {
            private readonly DataStore _dataStore;
            private string Equation => _dataStore.Equation; 

            private string _character;
            private string _prevToken;
            private string _readAhead;

            private Tables<string> _tables;

            private List<Token> _tokens;
            private string _rule;

           public event EventHandler<string> Logger;

            public Tokenizer(DataStore dataStore)
            {
                _dataStore = dataStore;
            }

            public List<Token> Tokenize()
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                _tokens = new List<Token>();
                if (_dataStore.DebugMode)
                {
                    _tables = new Tables<string>(new Config {Title = "Tokenizer", Format = _dataStore.DefaultFormat});
                    _tables.Add(new Schema {Column = "#", Width = 3});
                    _tables.Add(new Schema {Column = "Character", Width = 10});
                    _tables.Add(new Schema {Column = "Code", Width = 5});
                    _tables.Add(new Schema {Column = "Token", Width = 15});
                    _tables.Add(new Schema {Column = "# Tokens", Width = 11});
                    _tables.Add(new Schema {Column = "Action", Width = 16});
                }

                string token = string.Empty;
                _prevToken = string.Empty;

                int length = Equation.Length;

                ReadOnlySpan<char> equationSpan = Equation.AsSpan();
                ReadOnlySpan<char> localSpan = null;

                for (int i = 0; i < length; i++)
                {
                    localSpan = equationSpan.Slice(i);
                    _character = localSpan[0].ToString();
                    _readAhead = i < (length - 1) ? localSpan[1].ToString() : null;

                    //Alias code
                    if (_dataStore.Aliases.ContainsKey(token))
                    {
                        token = _dataStore.Aliases[token];
                    }

                    if (_dataStore.Aliases.ContainsKey(_character))
                    {
                        _character = _dataStore.Aliases[_character];
                    }

                    //WhiteSpace Rule
                    if (string.IsNullOrWhiteSpace(_character) && _character != ",")
                    {
                        WriteToken("WhiteSpace", ref token);
                    }
                    else
                    {

                        if (_dataStore.IsOperator(_character + _readAhead))
                        {
                            WriteToken("Operator", ref token);
                            token = _character + _readAhead;
                            WriteToken("Operator", ref token);
                            i = i + 1;
                        }
                        //Unary Input at the start of the input or after another operator or left parenthesis
                        else if ((i == 0 && _dataStore.IsUnary(_character)) || (_tokens.Count > 0 && (_dataStore.IsOperator(_prevToken) || _dataStore.IsLeftBracket(_prevToken) || _prevToken == ",") && _dataStore.IsUnary(_character) && !_dataStore.IsNumber(token)))
                        {
                            _rule = "Unary";
                            token += _character;
                            if (!(string.IsNullOrWhiteSpace(_readAhead)) && (_dataStore.IsVariable(_readAhead) || _dataStore.IsLeftBracket(_readAhead)))
                            {
                                token += "1";
                                WriteToken("Unary", ref token);
                            }
                        }
                        else if (token == "-." && _dataStore.IsNumber(_character))
                        {
                            _rule = "Decimal Append";
                            token += _character;
                        }
                        //Token is a number 
                        //Character is [LB, FUNC, Variable]
                        else if ( _dataStore.IsNumber(token) && (_dataStore.IsLeftBracket(_character) || _dataStore.IsFunction(_character) || _dataStore.IsVariable(_character)))
                        {
                            WriteToken("Left Implicit", ref token);
                            token = _character;
                            if (_dataStore.IsLeftBracket(_character) || (i == (Equation.Length - 1)))
                            {
                                WriteToken("Left Implicit", ref token);
                            }
                        }
                        //Token is a variable
                        //Character is a number
                        else if (_dataStore.IsNumber(_character) && _dataStore.IsVariable(token))
                        {
                            WriteToken("Left Implicit 2", ref token);
                            token = _character;
                            if (_dataStore.IsLeftBracket(_character) || (i == (Equation.Length - 1)))
                            {
                                WriteToken("Left Implicit 2", ref token);
                            }
                        }
                        else if (_dataStore.IsFunction(token) && _dataStore.IsLeftBracket(_character))
                        {
                            WriteToken("Function Start", ref token);
                            token = _character;
                            WriteToken("Function Start", ref token);
                        }
                        else if (_dataStore.IsFunction(token) && (_dataStore.IsRightBracket(_character) || _dataStore.IsOperator(_character)))
                        {
                            WriteToken("Function End", ref token);
                            token = _character;
                            WriteToken("Function End", ref token);
                        }

                        else if ( ( _dataStore.IsNumber(token) || _dataStore.IsVariable(token) ) && (_dataStore.IsLeftBracket(_character) || _dataStore.IsRightBracket(_character) || _dataStore.IsOperator(_character)))
                        {
                            WriteToken("Edge Case 1", ref token);
                            token = _character;
                            WriteToken("Edge Case 1", ref token);
                        }
                        else if (_dataStore.IsOperator(_character))
                        {
                            token += _character;
                            WriteToken("Operator", ref token);
                        }
                        else if (_dataStore.IsLeftBracket(_character) || _dataStore.IsRightBracket(_character))
                        {
                            token += _character;
                            WriteToken("Bracket", ref token);
                        }
                        else if (i == (Equation.Length - 1))
                        {
                            token += _character;
                            WriteToken("End of String", ref token);
                        }
                        else
                        {
                            _rule = "Append";
                            token += _character;
                        }

                        if (i == (Equation.Length - 1) && token.Length > 0)
                        {
                            WriteToken("End of String", ref token);
                        }

                        if (_dataStore.DebugMode)
                        {
                            _tables.Add(new string[] { i.ToString(), _character, ((int)_character[0]).ToString(), token, _tokens.Count.ToString(), _rule ?? string.Empty });
                        }
                    }
                }

                if (_dataStore.DebugMode)
                {
                    Write(_tables.ToString());

                    if (_tables.SuggestedRedraw)
                    {
                        Write(_tables.Redraw());
                    }
                }

                sw.Stop();
                _dataStore.AddTimeRecord("Tokenize", sw);

                Write("");

                return _tokens;
            }

            /// <summary>
            /// Creates a token from a string.
            /// This clears the token to an empty string
            /// and sets the previous token.
            /// </summary>
            /// <param name="rule"></param>
            private void WriteToken(string rule,ref string tokens)
            {
                if (string.IsNullOrWhiteSpace(tokens) && tokens != ",")
                {
                    return;
                }

                if (_dataStore.Aliases.ContainsKey(tokens))
                {
                    tokens = _dataStore.Aliases[tokens];
                }

                _rule = rule;

                Token token;

                switch (_dataStore.Resolve(tokens))
                {
                    case Type.Number:
                        token = new Token(tokens, 0, Type.Number);
                        break;
                    case Type.Function:
                        token = new Token(tokens, _dataStore.Functions[tokens].Arguments, Type.Function);
                        break;
                    case Type.Operator:
                        token = new Token(tokens, _dataStore.Operators[tokens].Arguments, Type.Operator);
                        break;
                    default:
                        token = new Token(tokens, 0, _dataStore.Resolve(tokens));
                        break;
                }
                _tokens.Add(token);

                _prevToken = tokens;
                tokens = string.Empty;
            }

            private void Write(string message)
            {
                if (_dataStore.DebugMode)
                {
                    Logger?.Invoke(this, message);
                }
            }

            private void Log(string message)
            {
                Logger?.Invoke(this, message);
            }
        }
    }
}
