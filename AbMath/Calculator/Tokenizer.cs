using System;
using System.Collections.Generic;
using System.Diagnostics;
using AbMath.Utilities;

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
                    _tables.Add(new Schema("#", 3));
                    _tables.Add(new Schema("Character"));
                    _tables.Add(new Schema("Code"));
                    _tables.Add(new Schema("Token"));
                    _tables.Add(new Schema("# Tokens"));
                    _tables.Add(new Schema("Action"));
                }

                string token = string.Empty;
                _prevToken = string.Empty;

                int length = Equation.Length;

                ReadOnlySpan<char> equationSpan = Equation.AsSpan();
                ReadOnlySpan<char> localSpan = null;
                int end = Equation.Length - 1;

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
                        //Resolve things ahead of time 
                        Type prevType = _dataStore.Resolve(_prevToken);
                        Type characterType = _dataStore.Resolve(_character);
                        Type tokenType = _dataStore.Resolve(token);
                        Type readAheadType = _dataStore.Resolve(_readAhead);

                        //{ -> list ( 
                        if (_character == "[" || _character == "{")
                        {
                            token = "list";
                            WriteToken("List Expansion", ref token, Type.Function);
                            token = "(";
                            WriteToken("List Expansion",ref token, Type.LParen);
                        }
                        else if (characterType == Type.Operator && readAheadType == Type.Operator)
                        {
                            WriteToken("Operator", ref token, tokenType);
                            token = _character + _readAhead;
                            WriteToken("Operator", ref token, Type.Operator);
                            i += 1;
                        }
                        //Unary Input at the start of the input or after another operator or left parenthesis
                        else if ((i == 0 && _dataStore.IsUnary(_character)) || (_tokens.Count > 0 && (prevType == Type.Operator || prevType == Type.LParen || _prevToken == ",") && _dataStore.IsUnary(_character) && tokenType != Type.Number))
                        { 
                            _rule = "Unary";
                            token += _character;
                            if (!(string.IsNullOrWhiteSpace(_readAhead)) && (readAheadType == Type.Variable || readAheadType == Type.LParen))
                            {
                                token += "1";
                                WriteToken("Unary", ref token);
                            }
                        }
                        else if (token == "-." &&  characterType == Type.Number)
                        {
                            _rule = "Decimal Append";
                            token += _character;
                        }
                        //Token is a number 
                        //Character is [LB, FUNC, Variable]
                        else if ( tokenType == Type.Number && (characterType == Type.LParen || characterType == Type.Function || characterType == Type.Variable))
                        {
                            WriteToken("Left Implicit", ref token);
                            token = _character;
                            if (characterType == Type.LParen || (i == (Equation.Length - 1)))
                            {
                                WriteToken("Left Implicit", ref token, characterType);
                            }
                        }
                        //Token is a variable
                        //Character is a number
                        else if (characterType == Type.Number && tokenType == Type.Variable)
                        {
                            WriteToken("Left Implicit 2", ref token, tokenType);
                            token = _character;
                            if (_dataStore.IsLeftBracket(_character) || (i == (Equation.Length - 1)))
                            {
                                WriteToken("Left Implicit 2", ref token, characterType);
                            }
                        }
                        else if (tokenType == Type.Function && characterType == Type.LParen)
                        {
                            WriteToken("Function Start", ref token, tokenType);
                            token = _character;
                            WriteToken("Function Start", ref token, characterType);
                        }
                        else if (tokenType == Type.Function && (characterType == Type.RParen || characterType == Type.Operator))
                        {
                            WriteToken("Function End", ref token, tokenType);
                            token = _character;
                            WriteToken("Function End", ref token, characterType);
                        }
                        else if ( (tokenType == Type.Number || tokenType == Type.Variable) && (characterType == Type.LParen || characterType == Type.RParen || characterType == Type.Operator))
                        {
                            WriteToken("Edge Case 1", ref token, tokenType);
                            token = _character;
                            WriteToken("Edge Case 1", ref token, characterType);
                        }
                        else if (characterType == Type.Operator)
                        {

                            token += _character;
                            WriteToken("Operator", ref token);
                        }
                        else if ( characterType == Type.LParen || characterType == Type.RParen)
                        {
                            token += _character;
                            WriteToken("Bracket", ref token);
                        }
                        else if (i == end)
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
            private void WriteToken(string rule,ref string tokens, Type type = Type.Null)
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

                if (type == Type.Null)
                {
                    type = _dataStore.Resolve(tokens);
                }

                Token token;
                switch (type)
                {
                    case Type.Function:
                        token = new Token(tokens, _dataStore.Functions[tokens].Arguments, Type.Function);
                        break;
                    case Type.Operator:
                        token = new Token(tokens, _dataStore.Operators[tokens].Arguments, Type.Operator);
                        break;
                    default:
                        token = new Token(tokens, 0, type);
                        break;
                }
                _tokens.Add(token);

                _prevToken = tokens;
                tokens = string.Empty;
            }

            private void Write(string message)
            {
                var logger = _dataStore.Logger;
                logger.Log(Channels.Debug, message);
            }

        }
    }
}
