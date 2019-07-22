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

            private string _token;
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

                _token = string.Empty;
                _prevToken = string.Empty;
                int length = Equation.Length;
                char[] equation = Equation.ToCharArray();

                for (int i = 0; i < length; i++)
                {
                    //We could convert this into a span?
                    _readAhead = i < (length - 1) ? equation[i + 1].ToString() : null;
                    _character = equation[i].ToString();

                    //Alias code
                    if (_dataStore.Aliases.ContainsKey(_token))
                    {
                        _token = _dataStore.Aliases[_token];
                    }

                    if (_dataStore.Aliases.ContainsKey(_character))
                    {
                        _character = _dataStore.Aliases[_character];
                    }

                    
                    //WhiteSpace Rule
                    if (string.IsNullOrWhiteSpace(_character) && _character != ",")
                    {
                        WriteToken("WhiteSpace");
                    }
                    else
                    {
                        //Unary Input at the start of the input or after another operator or left parenthesis
                        if ((i == 0 && _dataStore.IsUnary(_character)) || (_tokens.Count > 0 && (_dataStore.IsOperator(_prevToken) || _dataStore.IsLeftBracket(_prevToken) || _prevToken == ",") && _dataStore.IsUnary(_character) && !_dataStore.IsNumber(_token) && !_dataStore.IsOperator(_character + _readAhead)))
                        {
                            _rule = "Unary";
                            _token += _character;
                            if (!(string.IsNullOrWhiteSpace(_readAhead)) && (_dataStore.IsVariable(_readAhead) || _dataStore.IsLeftBracket(_readAhead)))
                            {
                                _token += "1";
                                WriteToken("Unary");
                            }
                        }
                        else if (_dataStore.IsNumber(_character) && _token == "-.")
                        {
                            _rule = "Decimal Append";
                            _token += _character;
                        }
                        else if ((_dataStore.IsNumber(_token)) && (_dataStore.IsVariable(_character) || _dataStore.IsLeftBracket(_character) || _dataStore.IsFunction(_character)))
                        {
                            WriteToken("Left Implicit");
                            _token = _character;
                            if (_dataStore.IsLeftBracket(_character) || (i == (Equation.Length - 1)))
                            {
                                WriteToken("Left Implicit");
                            }
                        }
                        else if (_dataStore.IsVariable(_token) && _dataStore.IsNumber(_character))
                        {
                            WriteToken("Left Implicit 2");
                            _token = _character;
                            if (_dataStore.IsLeftBracket(_character) || (i == (Equation.Length - 1)))
                            {
                                WriteToken("Left Implicit 2");
                            }
                        }
                        else if (_dataStore.IsFunction(_token) && _dataStore.IsLeftBracket(_character))
                        {
                            WriteToken("Function Start");
                            _token = _character;
                            WriteToken("Function Start");
                        }
                        else if (_dataStore.IsFunction(_token) && (_dataStore.IsRightBracket(_character) || _dataStore.IsOperator(_character)))
                        {
                            WriteToken("Function End");
                            _token = _character;
                            WriteToken("Function End");
                        }
                        else if (_dataStore.IsOperator(_character + _readAhead))
                        {
                            WriteToken("Operator");
                            _token = _character + _readAhead;
                            WriteToken("Operator");
                            i = i + 1;
                        }
                        else if ( ( _dataStore.IsNumber(_token) || _dataStore.IsVariable(_token) ) && (_dataStore.IsLeftBracket(_character) || _dataStore.IsRightBracket(_character) || _dataStore.IsOperator(_character)))
                        {
                            WriteToken("Edge Case 1");
                            _token = _character;
                            WriteToken("Edge Case 1");
                        }
                        else if (_dataStore.IsOperator(_character))
                        {
                            _token += _character;
                            WriteToken("Operator");
                        }
                        else if (_dataStore.IsLeftBracket(_character) || _dataStore.IsRightBracket(_character))
                        {
                            _token += _character;
                            WriteToken("Bracket");
                        }
                        else if (i == (Equation.Length - 1))
                        {
                            _token += _character;
                            WriteToken("End of String");
                        }
                        else
                        {
                            _rule = "Append";
                            _token += _character;
                        }

                        if (i == (Equation.Length - 1) && _token.Length > 0)
                        {
                            WriteToken("End of String");
                        }

                        if (_dataStore.DebugMode)
                        {
                            _tables.Add(new string[] { i.ToString(), _character, ((int)_character[0]).ToString(), _token, _tokens.Count.ToString(), _rule ?? string.Empty });
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
            private void WriteToken(string rule)
            {
                if (string.IsNullOrWhiteSpace(_token) && _token != ",")
                {
                    return;
                }

                _rule = rule;

                Token token;
                switch (_dataStore.Resolve(_token))
                {
                    case Type.Function:
                        token = new Token(_token, _dataStore.Functions[_token].Arguments, Type.Function);
                        break;
                    case Type.Operator:
                        token = new Token(_token, _dataStore.Operators[_token].Arguments, Type.Operator);
                        break;
                    default:
                        token = new Token(_token, 0, _dataStore.Resolve(_token));
                        break;
                }
                _tokens.Add(token);

                _prevToken = _token;
                _token = string.Empty;
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
