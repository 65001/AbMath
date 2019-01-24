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

            private string _token;
            private string _character;
            private string _prevToken;
            private string _readAhead;
            private Tables<string> _tables;

            private List<Term> _tokens;
            private string _rule;

           public event EventHandler<string> Logger;

            public Tokenizer(DataStore dataStore)
            {
                _dataStore = dataStore;
            }

            public List<Term> Tokenize()
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                _tokens = new List<Term>();

                _tables = new Tables<string>(new Config { Title = "Tokenizer", Format = _dataStore.DefaultFormat});

                if (_dataStore.DebugMode)
                {
                    _tables.Add(new Schema {Column = "#", Width = 3});
                    _tables.Add(new Schema {Column = "Character", Width = 10});
                    _tables.Add(new Schema {Column = "Token", Width = 15});
                    _tables.Add(new Schema {Column = "# Tokens", Width = 11});
                    _tables.Add(new Schema {Column = "Action", Width = 16});
                    Write(_tables.GenerateHeaders());
                }

                _token = string.Empty;
                _prevToken = string.Empty;

                int length = Equation.Length;
                for (int i = 0; i < length; i++)
                {
                    _rule = string.Empty;
                    _character = Equation.Substring(i, 1);
                    _readAhead = i < (length - 1) ? Equation.Substring((i + 1), 1) : string.Empty;

                    Alias();

                    //WhiteSpace Rule
                    if (string.IsNullOrWhiteSpace(_character) && _character != ",")
                    {
                        WriteToken("WhiteSpace");
                    }
                    //Unary Input at the start of the input or after another operator or left parenthesis
                    else if ((i == 0 && _dataStore.IsUnary(_character)) || (_tokens.Count > 0 && (_dataStore.IsOperator(_prevToken) || _dataStore.IsLeftBracket(_prevToken) || _prevToken ==",") && _dataStore.IsUnary(_character) && !_dataStore.IsNumber(_token) && 
                        !_dataStore.IsOperator(_character + _readAhead)))
                    {
                        _rule = "Unary";
                        _token += _character;
                        if(!string.IsNullOrWhiteSpace(_readAhead) && (_dataStore.IsVariable(_readAhead) || _dataStore.IsLeftBracket(_readAhead) ))
                        {
                            _token += "1";
                            WriteToken("Unary");
                        }
                    }
                    else if (_dataStore.IsNumber( _character ) && _token == "-.")
                    {
                        _rule = "Decimal Append";
                        _token += _character;
                    }
                    else if ( ( _dataStore.IsNumber(_token) ) && ( _dataStore.IsVariable(_character) || _dataStore.IsLeftBracket(_character) || _dataStore.IsFunction(_character)))
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
                    else if (_dataStore.IsFunction(_token) && _dataStore.IsRightBracket(_character))
                    {
                        WriteToken("Function End");
                        _token = _character;
                        WriteToken("Function End");
                    }
                    else if (_dataStore.IsFunction(_token) && _dataStore.IsOperator(_character))
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
                    else if (_dataStore.IsNumber(_token) && (_dataStore.IsLeftBracket(_character) || _dataStore.IsRightBracket(_character) || _dataStore.IsOperator(_character)))
                    {
                        WriteToken("Edge Case 1");
                        _token = _character;
                        WriteToken("Edge Case 1");
                    }
                    //Add equivalent for variables?
                    else if (_dataStore.IsVariable(_token) && (_dataStore.IsLeftBracket(_character) || _dataStore.IsRightBracket(_character) || _dataStore.IsOperator(_character)))
                    {
                        WriteToken("Edge Case 2");
                        _token = _character;
                        WriteToken("Edge Case 2");
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

                    if (_dataStore.DebugMode)
                    {
                        _tables.Add(new string[] {i.ToString(), _character, _token, _tokens.Count.ToString(), _rule});
                        Write(_tables.GenerateNextRow());
                    }
                }

                if (_dataStore.DebugMode)
                {
                    Write(_tables.GenerateFooter());
                }

                if (_dataStore.DebugMode && _tables.SuggestedRedraw)
                {
                    Write(_tables.Redraw());
                }
                sw.Stop();

                Write($"Tokenize Time {sw.ElapsedMilliseconds} (ms) Elapsed Ticks: {sw.ElapsedTicks.ToString("N0")}");
                _dataStore.TotalMilliseconds += sw.ElapsedMilliseconds;
                _dataStore.TotalSteps += sw.ElapsedTicks;
                Write("");

                return _tokens;
            }

            /// <summary>
            /// Transforms characters and tokens from mathematical notation into notation
            /// that AbMath understands.
            /// </summary>
            private void Alias()
            {
                if (_dataStore.Aliases.ContainsKey(_token))
                {
                    _token = _dataStore.Aliases[_token];
                }
                else if (_dataStore.Aliases.ContainsKey(_character))
                {
                    _character = _dataStore.Aliases[_character];
                }
            }

            private void WriteToken(string rule)
            {
                if (string.IsNullOrWhiteSpace(_token) && _token != ",")
                {
                    return;
                }

                _rule = rule;
                Term term = new Term
                {
                    Value = _token,
                    Type = _dataStore.Resolve(_token),
                    Arguments = 0
                };

                if(term.Type == Type.Function)
                {
                    term.Arguments = _dataStore.Functions[_token].Arguments;
                }
                else if (term.Type == Type.Operator)
                {
                    term.Arguments = _dataStore.Operators[_token].Arguments;
                }

                _tokens.Add(term);
                _prevToken = _token;
                _token = string.Empty;
            }

            private void Write(string message)
            {
                Logger?.Invoke(this, message);
            }
        }
    }
}
