using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using CLI;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        public class PreSimplify
        {
            private readonly DataStore _dataStore;

            private Token _prev5;
            private Token _prev4;
            private Token _prev3;
            private Token _prev2;
            private Token _prev;
            private Token _token;
            private Token _ahead;
            private Token _ahead2;
            private Token _ahead3;
            private Token _ahead4;
            private Token _ahead5;

            public event EventHandler<string> Logger;

            public PreSimplify(DataStore dataStore)
            {
                _dataStore = dataStore;
            }

            public List<Token> Apply(List<Token> tokens)
            {
                List<Token> expanded = expand(tokens);
                List<Token> swapped = swap(expanded);
                List<Token> simplified = simplify(swapped);
                List<Token> compressed = compress(simplified);

                if (_dataStore.DebugMode)
                {
                    Tables<string> table = new Tables<string>(new Config()
                        {Title = "Pre Simplification", Format = _dataStore.DefaultFormat}
                    );


                    table.Add(new Schema() {Column = "Type", Width = 12});
                    table.Add(new Schema() {Column = "Tokens", Width = 100});
                    //Log(table.GenerateHeaders());
                    table.Add(new string[] {"Original", tokens.ToArray().Print()});
                    table.Add(new string[] {"Expanded", expanded.ToArray().Print()});
                    table.Add(new string[] {"Swapped", swapped.ToArray().Print()});
                    table.Add(new string[] {"Simplified", simplified.ToArray().Print()});
                    table.Add(new string[] {"Compressed", compressed.ToArray().Print()});
                    //Log(table.GenerateBody());
                    //Log(table.GenerateFooter());

                    Log(table.ToString());
                }

                return compressed;
            }

            public List<Token> simplify(List<Token> tokens)
            {
                List<Token> results = new List<Token>(tokens.Count);
                Token _null = GenerateNull();

                for (int i = 0; i < tokens.Count; i++)
                {
                    _prev5 = (i > 4) ? _prev4 : _null;
                    _prev4 = (i > 3) ? _prev3 : _null;
                    _prev3 = (i > 2) ? _prev2 : _null;
                    _prev2 = (i > 1) ? _prev : _null;
                    _prev = (i > 0) ? _token : _null;
                    _token = tokens[i];
                    _ahead = ((i + 1) < tokens.Count) ? tokens[i + 1] : _null;
                    _ahead2 = ((i + 2) < tokens.Count) ? tokens[i + 2] : _null;
                    _ahead3 = ((i + 3) < tokens.Count) ? tokens[i + 3] : _null;
                    _ahead4 = ((i + 4) < tokens.Count) ? tokens[i + 4] : _null;
                    _ahead5 = ((i + 5) < tokens.Count) ? tokens[i + 5] : _null;

                    //p_5 must be a positive addition operator
                    //a_5 must be an addition or subtraction operator or be null
                    //p_4   p_3 p_2 p_1 t   a_1 a_2 a_3 a_4
                    //2     x   ^   1   +   3   x   ^   1 -> (p_4 + a_1)p_3 p_2 p _1
                    //2     x   ^   1   -   3   x   ^   1 -> (p_4 - a_1)p_3 p_2 p _1
                    //p_3 = a_2
                    if ( ( _prev5.IsNull() || !_prev5.IsNull() && (_prev5.Value == "+" || _prev5.IsLeftBracket())) &&
                         !_prev4.IsNull() && _prev4.IsNumber() &&
                         !_prev3.IsNull() && _prev3.IsVariable() &&
                         !_prev2.IsNull() && _prev2.IsOperator() && _prev2.Value == "^" &&
                         !_prev.IsNull() &&  ( _prev.IsNumber() || _prev.IsVariable() ) && 
                         _token.IsOperator() && (_token.Value == "+" || _token.Value == "-") &&
                         !_ahead.IsNull() && _ahead.IsNumber() &&
                         !_ahead2.IsNull() && _ahead2.IsVariable() &&
                         !_ahead3.IsNull() && _ahead3.IsOperator() && _ahead3.Value == "^" &&
                         !_ahead4.IsNull() && ( _ahead4.IsNumber() || _ahead4.IsVariable() ) &&
                         (_ahead5.IsNull() || !_ahead5.IsNull() && (_ahead5.IsRightBracket() || _ahead5.Value == "+" || _ahead5.Value == "-")) &&
                         _prev3.Value == _ahead2.Value && //ensures that the variables are equal to each other
                         _prev.Value == _ahead4.Value //ensures that the degree is the same
                         )
                    {
                        results.Pop(4);

                        double data1 = double.Parse(_prev4.Value);
                        double data2 = double.Parse(_ahead.Value);
                        double result = 0;

                        if (_token.Value == "+")
                        {
                            result = data1 + data2;
                        }
                        else
                        {
                            result = data1 - data2;
                        }
                        
                        results.Add(new Token() {Arguments = 0, Type = Type.Number, Value = result.ToString()} );
                        results.Add(_prev3);
                        results.Add(_prev2);
                        results.Add(_prev);
                        i += 4;
                    }
                    //p t   a
                    //x ^   0
                    else if (!_prev.IsNull() && !_ahead.IsNull() &&  _token.Value == "^" && _prev.IsVariable() && _ahead.Value == "0")
                    {
                        i += 1;
                        results.Pop(2);
                        results.Add(new Token() { Arguments = 0, Type = Type.Number, Value = "1" });
                    }
                    else
                    {
                        results.Add(_token);
                    }
                }

                if (!tokens.SequenceEqual(results))
                {
                    return simplify(results);
                }
                return results;
            }

            /// <summary>
            /// Expands all variables so that a variable
            /// will have a coefficient and explicit degree
            /// in the token stream.
            /// </summary>
            /// <param name="tokens"></param>
            /// <returns></returns>
            public List<Token> expand(List<Token> tokens)
            {
                List<Token> results = new List<Token>(tokens.Count);

                Token _null = GenerateNull();
                for (int i = 0; i < tokens.Count; i++)
                {
                    _prev = (i > 0) ? _token : _null;
                    _token = tokens[i];
                    _ahead = ((i + 1) < tokens.Count) ? tokens[i + 1] : _null;
                    _ahead2 = ((i + 2) < tokens.Count) ? tokens[i + 2] : _null;

                    // p t a a_2
                    // c x ^ p
                    if (_token.IsVariable())
                    {
                        if ( (_prev.IsNull() || !_prev.IsNull() && !_prev.IsNumber() && _prev.Value != "^") &&
                             (_ahead.IsNull() || !_ahead.IsNull() && !_ahead.IsNumber()))
                        {
                            results.Add(new Token() {Arguments = 0,Type = Type.Number, Value = "1"});
                        }

                        if (!_prev.IsNull() && !_prev.IsNumber() && !_ahead.IsNull() && _ahead.IsNumber())
                        {
                            i++;
                            results.Add(_ahead);
                        }

                        results.Add(_token);

                        //t a   a_2
                        //x ^   c or x
                        if ( (_prev.IsNull() || !_prev.IsNull() && _prev.Value != "^") &&
                            (_ahead.IsNull() || !_ahead.IsNull() && _ahead.Value != "^") &&
                            ( _ahead2.IsNull() || !_ahead2.IsNull() && ( !_ahead2.IsNumber() || !_ahead2.IsVariable() ) ) )
                        {
                            results.Add(new Token() {Arguments = 2,Type = Type.Operator, Value = "^"});
                            results.Add(new Token() { Arguments = 0, Type = Type.Number, Value = "1" });
                            //Log($"Adding exponent");
                            //Log($"Token is a {_token.Type} with a value of {_token.Value}");
                            //Log($"Ahead is a {_ahead.Type} with a value of {_ahead.Value}");
                            //Log($"Ahead 2 is a {_ahead2.Type} with a value of {_ahead2.Value}");
                        }

                    }
                    else
                    {
                        results.Add(_token);
                    }
                }
                return results;
            }
            
            /// <summary>
            /// Removes unnecessary tokens from the token stream.
            /// </summary>
            /// <param name="tokens"></param>
            /// <returns></returns>
            public List<Token> compress(List<Token> tokens)
            {
                List<Token> results = new List<Token>(tokens.Count);
                Token _null = GenerateNull();

                for (int i = 0; i < tokens.Count; i++)
                {
                    _token = tokens[i];
                    _ahead = ((i + 1) < tokens.Count) ? tokens[i + 1] : _null;
                    _ahead2 = ((i + 2) < tokens.Count) ? tokens[i + 2] : _null;
                    _ahead3 = ((i + 3) < tokens.Count) ? tokens[i + 3] : _null;

                    //t a_1 a_2 a_3
                    //1 x   ^   1 -> x
                    if (_token.IsNumber() && _token.Value == "1" &&
                        !_ahead.IsNull() && _ahead.IsVariable() &&
                        !_ahead2.IsNull() && _ahead2.IsOperator() && _ahead2.Value == "^" &&
                        !_ahead3.IsNull() && _ahead3.IsNumber() && _ahead3.Value == "1"
                        )
                    {
                        i += 3;
                        results.Add(_ahead);
                    }
                    //t a_1 a_2 a_3
                    //0 x   ^   c -> 0
                    else if (_token.IsNumber() && _token.Value == "0" &&
                             !_ahead.IsNull() && _ahead.IsVariable() &&
                             !_ahead2.IsNull() && _ahead2.IsOperator() && _ahead2.Value == "^" &&
                             !_ahead3.IsNull() &&  ( _ahead3.IsNumber() || _ahead3.IsVariable() )
                    )
                    {
                        i += 3;
                        results.Add(_token);
                    }
                    // t a_1 a_2
                    // x   ^   1 -> x
                    else if (_token.IsVariable() && !_ahead.IsNull() && _ahead.Value == "^" && !_ahead2.IsNull() && _ahead2.Value == "1")
                    {
                        i += 2;
                        results.Add(_token);
                    }
                    //t a_1
                    //1 x
                    else if (_token.Value == "1" && !_ahead.IsNull() && _ahead.IsVariable())
                    {
                        i += 1;
                        results.Add(_ahead);
                    }
                    else
                    {
                        results.Add(_token);
                    }
                }

                return results;
            }

            public List<Token> swap(List<Token> tokens)
            {
                List<Token> results = new List<Token>(tokens.Count);
                Token _null = GenerateNull();
                //Log($"Swap Input: {tokens.ToArray().Print()}");

                for (int i = 0; i < tokens.Count; i++)
                {
                    _prev5 = (i > 4) ? _prev4 : _null;
                    _prev4 = (i > 3) ? _prev3 : _null;
                    _prev3 = (i > 2) ? _prev2 : _null;
                    _prev2 = (i > 1) ? _prev : _null;
                    _prev = (i > 0) ? _token : _null;
                    _token = tokens[i];
                    _ahead = ((i + 1) < tokens.Count) ? tokens[i + 1] : _null;
                    _ahead2 = ((i + 2) < tokens.Count) ? tokens[i + 2] : _null;
                    _ahead3 = ((i + 3) < tokens.Count) ? tokens[i + 3] : _null;
                    _ahead4 = ((i + 4) < tokens.Count) ? tokens[i + 4] : _null;
                    _ahead5 = ((i + 5) < tokens.Count) ? tokens[i + 5] : _null;
                    //p_5 must be a positive addition operator
                    //p_4   p_3 p_2 p_1 t   a_1 a_2 a_3 a_4
                    //2     x   ^   1   +   3   x   ^   1 -> (p_4 + a_1)p_3 p_2 p _1
                    //2     x   ^   1   -   3   x   ^   1 -> (p_4 - a_1)p_3 p_2 p _1
                    //p_3 = a_2
                    //either both a4 and p must be numbers while a4 > p
                    //or p can be a variable where p = a4
                    //or p can be a variable while a4 is a number
                    if ((_prev5.IsNull() || !_prev5.IsNull() && (_prev5.Value == "+" || _prev5.Value == "-")) &&
                        !_prev4.IsNull() && _prev4.IsNumber() &&
                        !_prev3.IsNull() && _prev3.IsVariable() &&
                        !_prev2.IsNull() && _prev2.IsOperator() && _prev2.Value == "^" &&
                        !_prev.IsNull() && (_prev.IsNumber() || _prev.IsVariable()) &&
                        _token.IsOperator() && (_token.Value == "+" || _token.Value == "-") &&
                        !_ahead.IsNull() && _ahead.IsNumber() &&
                        !_ahead2.IsNull() && _ahead2.IsVariable() &&
                        !_ahead3.IsNull() && _ahead3.IsOperator() && _ahead3.Value == "^" &&
                        !_ahead4.IsNull() && (_ahead4.IsNumber() || _ahead4.IsVariable()) &&
                        (_ahead5.IsNull() || !_ahead5.IsNull() && (_ahead5.Value == "+" || _ahead5.Value == "-")) &&
                        _prev3.Value == _ahead2.Value && //ensures that the variables are equal to each other
                        //degree rules 
                        ( (_ahead4.Value == _prev.Value && _prev.IsVariable() && double.Parse(_ahead.Value) > double.Parse(_prev4.Value)) || 
                          //p is a variable and a4 is a number
                          _prev.IsVariable() && _ahead4.IsNumber() ||
                          //a4 and p are numbers where a4 > p
                        _ahead4.IsNumber() && _prev.IsNumber() && double.Parse(_ahead4.Value) > double.Parse(_prev.Value) )) 
                        
                    {

                        //Log($"{_prev5.Value} {_prev4.Value}    {_prev3.Value}  {_prev2.Value}  {_prev.Value}   {_token.Value}  {_ahead.Value}  {_ahead2.Value} {_ahead3.Value} {_ahead4.Value}");
                        //Log($"Swapping {_token.Value}{_ahead.Value}{_ahead2.Value}^{_ahead4.Value} with {_prev5.Value}{_prev4.Value}{_prev3.Value}^{_prev.Value}");

                        i += 4;
                        if (!_prev5.IsNull())
                        {
                            results.Pop(5);
                        }
                        else
                        {
                            results.Pop(4);
                        }

                        if (results.Count > 0 || _token.Value != "+")
                        {
                            results.Add(_token);
                        }

                        results.Add(_ahead);
                        results.Add(_ahead2);
                        results.Add(_ahead3);
                        results.Add(_ahead4);

                        if (_prev5.IsNull())
                        {
                            results.Add(new Token {Arguments = 2,Type = Type.Operator,Value = "+"});
                        }
                        else
                        {
                            results.Add(_prev5);
                        }

                        results.Add(_prev4);
                        results.Add(_prev3);
                        results.Add(_prev2);
                        results.Add(_prev);

                        //Log(results.ToArray().Print());
                    }
                    else
                    {
                        results.Add(_token);
                    }
                }

                if (!tokens.SequenceEqual(results))
                {
                    return swap( results );
                }
                return results;
            }

            private static Token GenerateNull() => new Token { Type = Type.Null };

            private void Log(string message)
            {
                Logger?.Invoke(this, message);
            }
        }

        public class PostSimplify
        {
            private DataStore _data;

            private Token _prev5;
            private Token _prev4;
            private Token _prev3;
            private Token _prev2;
            private Token _prev;
            private Token _token;
            private Token _ahead;
            private Token _ahead2;
            private Token _ahead3;
            private Token _ahead4;
            private Token _ahead5;

            public event EventHandler<string> Logger;

            public PostSimplify(DataStore dataStore)
            {
                _data = dataStore;
            }

            public List<Token> simplify(List<Token> tokens)
            {
                List<Token> results = new List<Token>(tokens.Count);
                Token _null = GenerateNull();

                for (int i = 0; i < tokens.Count; i++)
                {
                    _prev5 = (i > 4) ? _prev4 : _null;
                    _prev4 = (i > 3) ? _prev3 : _null;
                    _prev3 = (i > 2) ? _prev2 : _null;
                    _prev2 = (i > 1) ? _prev : _null;
                    _prev = (i > 0) ? _token : _null;
                    _token = tokens[i];
                    _ahead = ((i + 1) < tokens.Count) ? tokens[i + 1] : _null;
                    _ahead2 = ((i + 2) < tokens.Count) ? tokens[i + 2] : _null;
                    _ahead3 = ((i + 3) < tokens.Count) ? tokens[i + 3] : _null;
                    _ahead4 = ((i + 4) < tokens.Count) ? tokens[i + 4] : _null;
                    _ahead5 = ((i + 5) < tokens.Count) ? tokens[i + 5] : _null;

                    //p  t   a
                    //x  x   - -> 0
                    if (!_prev.IsNull() && !_ahead.IsNull() && _prev.IsVariable() && _prev.Value == _token.Value && (_ahead.Value == "-" || _ahead.Value == "+"))
                    {
                        if (_ahead.Value == "-")
                        {
                            i += 1;
                            results.Pop(1);
                            results.Add(new Token { Arguments = 0, Type = Type.Number, Value = "0" });
                        }
                        else
                        {
                            i += 1;
                            results.Add(new Token {Arguments = 0, Type = Type.Number, Value = "2"});
                            results.Add(new Token { Arguments = 2, Type = Type.Operator, Value = "*" });
                        }
                    }
                    //p  t   a
                    //0  x   *
                    //x  0   *
                    else if (!_prev.IsNull() && !_ahead.IsNull() && _ahead.Value == "*" &&
                             ((_prev.Value == "0" &&  (_token.IsVariable() || _token.IsNumber() ) || 
                             (_prev.IsVariable() || _prev.IsNumber()) && _token.Value == "0"))
                             )
                    {
                        i += 1;
                        results.Pop(1);
                        results.Add(new Token {Arguments = 0,Type = Type.Number, Value = "0"});
                    }
                    //p3    p2  p1  t   a1  a2  a3
                    //3     x   *   3   x   *   -

                    //p2    p1  t   a   a2
                    //3     x   *   0   *
                    else
                    {
                        results.Add(_token);
                    }


                    //p4    p3  p2  p   t   a   a2  a3  a4  a5  a6 
                    //3     x   2   ^   *   3   x   2   ^   *   -
                }

                if (!results.SequenceEqual(tokens))
                {
                    return simplify(results);
                }

                return results;
            }

            private void Log(string message)
            {
                Logger?.Invoke(this, message);
            }

            private static Token GenerateNull() => new Token { Type = Type.Null };
        }
    }
}