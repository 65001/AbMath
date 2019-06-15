using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
            private Token _null => GenerateNull();

            public event EventHandler<string> Logger;

            public PreSimplify(DataStore dataStore)
            {
                _dataStore = dataStore;
            }

            public List<Token> Apply(List<Token> tokens)
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();
                List<Token> expanded = expand(tokens);
                SW.Stop();

                _dataStore.AddTimeRecord("PreSimplify.Expand", SW);

                SW.Reset();
                SW.Start();
                List<Token> swapped = swap(expanded);
                SW.Stop();
                _dataStore.AddTimeRecord("PreSimplify.Swap", SW);
                SW.Reset();
                SW.Start();
                List<Token> simplified = simplify(swapped);
                SW.Stop();
                _dataStore.AddTimeRecord("PreSimplify.Simplify", SW);
                SW.Reset();
                SW.Start();
                List<Token> compressed = compress(simplified);
                SW.Stop();
                _dataStore.AddTimeRecord("PreSimplify.Compress", SW);

                if (_dataStore.DebugMode)
                {
                    SW.Reset();
                    SW.Start();

                    Tables<string> table = new Tables<string>(new Config()
                        {Title = "Pre Simplification", Format = _dataStore.DefaultFormat}
                    );

                    table.Add(new Schema() {Column = "Type", Width = 12});
                    table.Add(new Schema() {Column = "Tokens", Width = 100});
                    table.Add(new string[] {"Original", tokens.Print()});
                    table.Add(new string[] {"Expanded", expanded.Print()});
                    table.Add(new string[] {"Swapped", swapped.Print()});
                    table.Add(new string[] {"Simplified", simplified.Print()});
                    table.Add(new string[] {"Compressed", compressed.Print()});

                    Log(table.ToString());

                    SW.Stop();
                    _dataStore.AddTimeRecord("PreSimplify.Debug", SW);
                }

                return compressed;
            }

            public List<Token> simplify(List<Token> tokens)
            {
                while (true)
                {
                    List<Token> results = new List<Token>(tokens.Count);
                    Token _null = GenerateNull();

                    for (int i = 0; i < tokens.Count; i++)
                    {
                        GenerateState(ref tokens, i);

                        //p_5 must be a positive addition operator
                        //a_5 must be an addition or subtraction operator or be null
                        //p_4   p_3 p_2 p_1 t   a_1 a_2 a_3 a_4
                        //2     x   ^   1   +   3   x   ^   1 -> (p_4 + a_1)p_3 p_2 p _1
                        //2     x   ^   1   -   3   x   ^   1 -> (p_4 - a_1)p_3 p_2 p _1
                        //p_3 = a_2
                        if ((_prev5.IsNull() || !_prev5.IsNull() && (_prev5.Value == "+" || _prev5.IsLeftBracket())) && !_prev4.IsNull() && _prev4.IsNumber() && !_prev3.IsNull() && _prev3.IsVariable() && !_prev2.IsNull() && _prev2.IsOperator() && _prev2.Value == "^" && !_prev.IsNull() && (_prev.IsNumber() || _prev.IsVariable()) && _token.IsOperator() && (_token.Value == "+" || _token.Value == "-") && !_ahead.IsNull() && _ahead.IsNumber() && !_ahead2.IsNull() && _ahead2.IsVariable() && !_ahead3.IsNull() && _ahead3.IsOperator() && _ahead3.Value == "^" && !_ahead4.IsNull() && (_ahead4.IsNumber() || _ahead4.IsVariable()) && (_ahead5.IsNull() || !_ahead5.IsNull() && (_ahead5.IsRightBracket() || _ahead5.Value == "+" || _ahead5.Value == "-")) && _prev3.Value == _ahead2.Value && //ensures that the variables are equal to each other
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

                            results.Add(new Token(result));
                            results.Add(_prev3);
                            results.Add(_prev2);
                            results.Add(_prev);
                            i += 4;
                        }
                        //p t   a
                        //x ^   0
                        else if (!_prev.IsNull() && !_ahead.IsNull() && _token.Value == "^" && _prev.IsVariable() && _ahead.Value == "0")
                        {
                            i += 1;
                            results.Pop(2);
                            results.Add(new Token(1));
                        }
                        //t a_1 a_2 a_3
                        //0 x   ^   c -> 0
                        else if (_token.IsNumber() && _token.Value == "0" && !_ahead.IsNull() && _ahead.IsVariable() && !_ahead2.IsNull() && _ahead2.IsOperator() && _ahead2.Value == "^" && !_ahead3.IsNull() && (_ahead3.IsNumber() || _ahead3.IsVariable()))
                        {
                            i += 3;
                            results.Add(_token);
                        }
                        else
                        {
                            results.Add(_token);
                        }
                    }

                    if (!tokens.SequenceEqual(results))
                    {
                        tokens = results;
                        continue;
                    }

                    return results;
                }
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

                for (int i = 0; i < tokens.Count; i++)
                {
                    GenerateState(ref tokens, i);
                    // p t a a_2
                    // c x ^ p
                    if (_token.IsVariable())
                    {
                        if ( (_prev.IsNull() || !_prev.IsNull() && !_prev.IsNumber() && _prev.Value != "^") &&
                             (_ahead.IsNull() || !_ahead.IsNull() && !_ahead.IsNumber()))
                        {
                            results.Add(new Token(1));
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
                            results.Add(new Token("^",2,Type.Operator));
                            results.Add(new Token(1));
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

                for (int i = 0; i < tokens.Count; i++)
                {
                    GenerateState(ref tokens, i);

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
                    // t a_1 a_2
                    // x   ^   1 -> x
                    else if (_token.IsVariable() && !_ahead.IsNull() && _ahead.Value == "^" && !_ahead2.IsNull() && _ahead2.Value == "1")
                    {
                        i += 2;
                        results.Add(_token);
                    }
                    //t a_1
                    //1 x
                    //the previous value cannot be -
                    else if (_token.Value == "1" && !_ahead.IsNull() && _ahead.IsVariable() && (_prev.IsNull() || _prev.Value != "-"))
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
                while (true)
                {
                    List<Token> results = new List<Token>(tokens.Count);

                    for (int i = 0; i < tokens.Count; i++)
                    {
                        GenerateState(ref tokens, i);

                        //p_5 must be a [null|+,-,)]
                        //p_4   p_3 p_2 p_1 t   a_1 a_2 a_3 a_4
                        //2     x   ^   1   +   3   x   ^   1 -> (p_4 + a_1)p_3 p_2 p _1
                        //2     x   ^   1   -   3   x   ^   1 -> (p_4 - a_1)p_3 p_2 p _1
                        //p_3 = a_2
                        //either both a4 and p must be numbers while a4 > p
                        //or p can be a variable where p = a4
                        //or p can be a variable while a4 is a number
                        if ((_prev5.IsNull() ||  !_prev5.IsNull() && (_prev5.Value == "+" || _prev5.Value == "-")) && !_prev4.IsNull() && _prev4.IsNumber() && !_prev3.IsNull() && _prev3.IsVariable() && !_prev2.IsNull() && _prev2.IsOperator() && _prev2.Value == "^" && !_prev.IsNull() && (_prev.IsNumber() || _prev.IsVariable()) 
                            && _token.IsOperator() && (_token.Value == "+" || _token.Value == "-") && !_ahead.IsNull() && _ahead.IsNumber() && !_ahead2.IsNull() && _ahead2.IsVariable() && !_ahead3.IsNull() && _ahead3.IsOperator() && _ahead3.Value == "^" && !_ahead4.IsNull() && (_ahead4.IsNumber() || _ahead4.IsVariable()) && (_ahead5.IsNull() || !_ahead5.IsNull() && (_ahead5.Value == "+" || _ahead5.Value == "-")) && _prev3.Value == _ahead2.Value && //ensures that the variables are equal to each other
                            //degree rules 
                            ((_ahead4.Value == _prev.Value && _prev.IsVariable() && double.Parse(_ahead.Value) > double.Parse(_prev4.Value)) ||
                             //p is a variable and a4 is a number
                             _prev.IsVariable() && _ahead4.IsNumber() ||
                             //a4 and p are numbers where a4 > p
                             _ahead4.IsNumber() && _prev.IsNumber() && double.Parse(_ahead4.Value) > double.Parse(_prev.Value)))

                        {
                            Log($"{_prev5.Value} {_prev4.Value}    {_prev3.Value}  {_prev2.Value}  {_prev.Value}   {_token.Value}  {_ahead.Value}  {_ahead2.Value} {_ahead3.Value} {_ahead4.Value}");
                            Log($"Swapping {_token.Value}{_ahead.Value}{_ahead2.Value}^{_ahead4.Value} with {_prev5.Value}{_prev4.Value}{_prev3.Value}^{_prev.Value}");

                            i += 4;
                            if (!_prev5.IsNull())
                            {
                                results.Pop(5);
                            }
                            else
                            {
                                results.Pop(4);
                            }

                            if (_prev5.IsLeftBracket())
                            {
                                results.Add(_prev5);
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
                                results.Add(new Token("+",2,Type.Operator) );
                            }
                            else if (!_prev5.IsLeftBracket())
                            {
                                results.Add(_prev5);
                            }

                            results.Add(_prev4);
                            results.Add(_prev3);
                            results.Add(_prev2);
                            results.Add(_prev);
                        }
                        else
                        {
                            results.Add(_token);
                        }
                    }

                    if (!tokens.SequenceEqual(results))
                    {
                        tokens = results;
                        continue;
                    }

                    return results;
                }
            }

            private void GenerateState(ref List<Token> tokens, int i)
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();

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

                SW.Stop();
                _dataStore.AddTimeRecord("PreSimplify.GenerateState", SW);
            }

            private static Token GenerateNull() => new Token { Type = Type.Null };

            private void Log(string message)
            {
                Logger?.Invoke(this, message.Alias());
            }
        }

        public class PostSimplify
        {
            private DataStore _data;

            private Token _prev6;
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
            private Token _ahead6;

            private Token _null => GenerateNull();

            public event EventHandler<string> Logger;

            public PostSimplify(DataStore dataStore)
            {
                _data = dataStore;
            }

            public List<Token> Apply(List<Token> tokens)
            {
                List<Token> expanded = expand(tokens);
                List<Token> swapped = swap(expanded);
                List<Token> simplified = simplify(swapped);
                List<Token> compressed = compress(simplified);
                List<Token> second_round_simplified = simplify(compressed);

                if (_data.DebugMode)
                {
                    Tables<string> table = new Tables<string>(new Config()
                        { Title = "Post Simplification", Format = _data.DefaultFormat }
                    );

                    table.Add(new Schema() { Column = "Type", Width = 12 });
                    table.Add(new Schema() { Column = "Tokens", Width = 100 });
                    
                    table.Add(new string[] { "Original", tokens.Print() });
                    table.Add(new string[] { "Expanded", expanded.Print() });
                    table.Add(new string[] { "Swapped", swapped.Print() });
                    table.Add(new string[] { "Simplified", simplified.Print() });
                    table.Add(new string[] { "Compressed", compressed.Print() });
                    table.Add(new string[] { "Simplified", second_round_simplified.Print()});

                    Log(table.ToString());
                }

                return second_round_simplified;
            }

            public List<Token> expand(List<Token> tokens)
            {
                List<Token> results = new List<Token>(tokens.Count);

                //p t
                //c x
                for (int i = 0; i < tokens.Count; i++)
                {
                    
                    GenerateState(ref tokens, i);

                    //x @ ^         //should not compile
                    if (_prev.IsVariable() && _token.IsVariable() && _ahead.Value == "^")
                    {
                        results.Add(_token);
                    }
                    //t a a2   a3
                    //x 2 ^    *   -> 2x^2
                    //x * c|v  op  -> 2x [c|v] op
                    //2 x ) NOP
                    else if (_token.IsVariable())
                    {
                        bool addedCoefficient = false;
                        bool addedExponent = false;

                        if (_data.DebugMode)
                        {
                            Log($"Expanding Term of {_token}");
                            Log($"{_prev} {_token} {_ahead} {_ahead2} {_ahead3}");
                        }

                        //There is no coefficient
                        if ((_prev.IsNull() || !_prev.IsNumber()) && (_ahead2.Value != "*") && (_ahead3.IsNull() || _ahead3.Value != "*") && _ahead.Value != "^")
                        {
                            addedCoefficient = true;
                            if (_data.DebugMode)
                            {
                                Log("\tAdding Coefficient");
                            }

                            results.Add(new Token(1));
                        }

                        results.Add(_token);

                        //There is no exponent
                        if ((_ahead2.IsNull() || _ahead2.Value != "^") && _ahead.Value != "^")
                        {
                            //Add exponent stuff
                            if (_data.DebugMode)
                            {
                                Log("\tAdding Exponent");
                            }

                            addedExponent = true;

                            results.Add(new Token(1));
                            results.Add(new Token("^",2,Type.Operator));
                        }

                        if (addedCoefficient)
                        {
                            results.Add(new Token("*",2,Type.Operator));
                        }


                    }
                    else
                    {
                        results.Add(_token);
                    }
                }

                List<Token> data = new List<Token>(results.Count);

                for (int i = 0; i < results.Count; i++)
                {
                    GenerateState(ref results, i);
                    //p4  p3  p2  p     t
                    //1   x   *   5     ^
                    //c   x   *   c|x   ^
                    //p4  p3  p t p2
                    if (!_prev.IsNull() && !_prev2.IsNull() && !_prev3.IsNull() && !_prev4.IsNull() &&
                        _token.Value == "^" && (_prev.IsVariable() || _prev.IsNumber()) && 
                        (_prev2.Value == "*") && _prev3.IsVariable() && _prev4.IsNumber()
                        )
                    {
                        data.Pop(4);
                        data.Add(_prev4);
                        data.Add(_prev3);
                        data.Add(_prev);
                        data.Add(_token);
                        data.Add(_prev2);
                    }
                    else
                    {
                        data.Add(_token);
                    }
                }
                return data;
            }

            public List<Token> compress(List<Token> tokens)
            {
                while (true)
                {
                    List<Token> results = new List<Token>(tokens.Count);

                    for (int i = 0; i < tokens.Count; i++)
                    {
                        GenerateState(ref tokens, i);

                        //1 ^ -> ""
                        if (_token.Value == "^" && !_prev.IsNull() && _prev.Value == "1")
                        {
                            results.Pop(1);
                        }
                        //t a   a2
                        //1 c|x *-> c|x
                        else if (_token.Value == "1" && (!_ahead.IsNull() && !_ahead2.IsNull()) && (_ahead.IsVariable() || _ahead.IsNumber()) && (_ahead2.Value == "*"))
                        {
                            results.Add(_ahead);
                            i += 2;
                        }
                        //c|x 1 *-> c|x
                        else if ((_token.IsVariable() || _token.IsNumber()) &&
                                 (!_ahead.IsNull() && _ahead.Value == "1") &&
                                 (!_ahead2.IsNull() && _ahead2.Value == "*"))
                        {
                            results.Add(_token);
                            i += 2;
                        }
                        //c|x 0   * -> 0
                        else if ((_token.IsVariable() || _token.IsNumber()) &&
                                 !_ahead.IsNull() && _ahead.Value == "0" &&
                                 !_ahead2.IsNull() && _ahead2.Value == "*"
                        )
                        {
                             results.Add(_ahead);
                            i += 2;
                        }
                        //0   c|x * -> 0
                        else if (_token.Value == "0" &&
                                 !_ahead.IsNull() && (_ahead.IsVariable() || _ahead.IsNumber()) &&
                                 !_ahead2.IsNull() && _ahead2.Value == "*")
                        {
                            results.Add(_token);
                            i += 2;
                        }
                        //t 1  2  3 4
                        //1 x c|x ^ * -> 1 2 3
                        //1 x 6   ^ * -> 1 2 3
                        else if (_token.Value == "1" && 
                                 !_ahead.IsNull() && !_ahead2.IsNull() && !_ahead3.IsNull() && !_ahead4.IsNull() && 
                                 _ahead3.Value == "^" && _ahead4.Value == "*" && 
                                 _ahead.IsVariable() && (_ahead2.IsVariable() || _ahead2.IsNumber())
                                 )
                        {
                            i += 4;
                            results.Add(_ahead);
                            results.Add(_ahead2);
                            results.Add(_ahead3);
                        }
                        else
                        {
                            results.Add(_token);
                        }
                    }

                    if (!tokens.SequenceEqual(results))
                    {
                        tokens = results;
                        continue;
                    }

                    return results;
                }
            }

            public List<Token> simplify(List<Token> tokens)
            {
                int pass = 0;
                while (true)
                {
                    List<Token> results = new List<Token>(tokens.Count);

                    for (int i = 0; i < tokens.Count; i++)
                    {
                        GenerateState(ref tokens, i);
                        //p5    p4  p3   p2   p     t   a   a2      a3     a4   a5
                        //c     x   c|x   ^   *     c   x   c|x     ^      *    (+|-|*) ->
                        //NC x pow ^ *
                        //NC p5 \pm t
                        //p3 and a2 must be the same constant or variable (Powers must be constant) or a5 must be *
                        //p4 and a  must be the same variable
                        if (!_prev5.IsNull() && !_prev4.IsNull() && !_prev3.IsNull() && !_prev2.IsNull() && !_prev.IsNull() &&
                                 !_ahead.IsNull() && !_ahead2.IsNull() && !_ahead3.IsNull() && !_ahead4.IsNull() && !_ahead5.IsNull() &&
                                 _prev5.IsNumber() && _prev4.IsVariable() &&
                                 (_prev3.IsVariable() || _prev3.IsNumber()) &&
                                 _prev2.Value == "^" && _prev.Value == "*" &&
                                 _token.IsNumber() && _ahead.IsVariable() &&
                                 (_ahead2.IsVariable() || _ahead2.IsNumber()) &&
                                 _ahead3.Value == "^" && _ahead4.Value == "*" &&
                                 (_ahead5.Value == "+" || _ahead5.Value == "-" || _ahead5.Value == "*") &&
                                 (_prev3.Value == _ahead2.Value || _ahead5.Value == "*") &&
                                 (_prev4.Value == _ahead.Value)
                        )
                        {
                            results.Pop(5);
                            i += 5;

                            double coefficient = 0;
                            if (_ahead5.Value == "+")
                            {
                                coefficient = double.Parse(_prev5.Value) + double.Parse(_token.Value);
                            }
                            else if (_ahead5.Value == "-")
                            {
                                coefficient = double.Parse(_prev5.Value) - double.Parse(_token.Value);
                            }
                            else if (_ahead5.Value == "*")
                            {
                                coefficient = double.Parse(_prev5.Value) * double.Parse(_token.Value);
                            }


                            results.Add(new Token(coefficient));
                            results.Add(_ahead);

                            if (_ahead5.Value == "*")
                            {
                                if (_ahead2.IsVariable() || _prev3.IsVariable())
                                {
                                    results.Add(_ahead2);
                                    results.Add(_prev3);
                                    results.Add(new Token("+",2,Type.Operator));
                                }
                                else
                                {
                                    double power = double.Parse(_ahead2.Value) + double.Parse(_prev3.Value);
                                    results.Add(new Token(power) );
                                }
                            }
                            else
                            {
                                results.Add(_ahead2);
                            }

                            results.Add(new Token("^",2,Type.Operator));
                            results.Add(new Token("*",2,Type.Operator));
                        }
                        //c  x  c  ^  *  0  * -> 0
                        //p  t  a1 a2 a3 a4 a5
                        //a1 >= 0
                        else if (!_ahead.IsNull() && !_ahead2.IsNull() && !_ahead3.IsNull() &&
                                 !_ahead4.IsNull() && !_prev.IsNull() && 
                                 _prev.IsNumber() && _token.IsVariable() && 
                                  ( _ahead.IsNumber() && double.Parse(_ahead.Value) >= 0) &&
                                  _ahead2.Value == "^" && _ahead3.Value == "*" &&
                                 _ahead4.Value == "0" && _ahead5.Value == "*"
                                 )
                        {
                            results.Pop(1);
                            i += 5;
                            results.Add(_ahead4);
                        }
                        //6 5 4 3 2 1 t 1 2 3 4 5 6
                        //c 1 x 1 ^ * + 1 x 1 ^ * +
                        else
                        {
                            results.Add(_token);
                        }
                    }

                    if (!results.SequenceEqual(tokens))
                    {
                        tokens = results;
                        pass++;
                        Log($"Simplify Pass {pass}: {tokens.Print()}");
                        continue;
                    }

                    return results;
                }
            }

            public List<Token> swap(List<Token> tokens)
            {
                while (true)
                {
                    List<Token> results = new List<Token>(tokens.Count);
                    for (int i = 0; i < tokens.Count; i++)
                    {
                        GenerateState(ref tokens, i);


                        //2 x 1 ^ *  3  x 2 ^ *   (+)
                        //5 4 3 2 1  t  1 2 3 4    5
                        if (!_prev5.IsNull() && !_prev4.IsNull() && !_prev3.IsNull() && !_prev2.IsNull() && !_prev.IsNull() && 
                            !_ahead5.IsNull() && !_ahead4.IsNull() && !_ahead3.IsNull() && !_ahead2.IsNull() && !_ahead.IsNull() 
                            && _prev5.IsNumber() && _prev4.IsVariable() && (_prev3.IsNumber() || _prev3.IsVariable()) && 
                            _prev2.Value == "^" && _prev.Value == "*" && _token.IsNumber() && _ahead.IsVariable() && _ahead2.IsNumber() 
                            && _ahead3.Value == "^" && _ahead4.Value == "*" && (_ahead5.Value == "+") &&
                            (_ahead2.IsVariable() || double.Parse(_ahead2.Value) > double.Parse(_prev3.Value)))
                        {
                            Log($"{_token}{_ahead}{_ahead3}{_ahead2} swapped with {_prev5}{_prev4}{_prev2}{_prev3} under Rule 1.");
                            results.Pop(5);
                            i += 5;
                            results.Add(_token);
                            results.Add(_ahead);
                            results.Add(_ahead2);
                            results.Add(_ahead3);
                            results.Add(_ahead4);

                            results.Add(_prev5);
                            results.Add(_prev4);
                            results.Add(_prev3);
                            results.Add(_prev2);
                            results.Add(_prev);

                            results.Add(_ahead5);
                        }
                        //1 x 1 ^ *  +  3 x 2 ^ *  (+)
                        //5 4 3 2 1  t  1 2 3 4 5   6 -> a(1-5) t p(5-1) a6
                        //if a3 is a variable or a3 > p3 then swap
                        else if ( !_prev5.IsNull() && !_prev4.IsNull() && !_prev3.IsNull() && !_prev2.IsNull() && !_prev.IsNull() &&
                               !_ahead.IsNull() && !_ahead2.IsNull() && !_ahead3.IsNull() && !_ahead4.IsNull() && !_ahead5.IsNull() && !_ahead6.IsNull() &&
                               (_token.Value == "+") &&
                               _prev5.IsNumber() && _prev4.IsVariable() && 
                               (_prev3.IsVariable() || _prev3.IsNumber()) &&
                               _prev2.Value == "^" && _prev.Value == "*" &&
                               _ahead.IsNumber() && _ahead2.IsVariable() &&
                               (_ahead3.IsNumber() || _ahead3.IsVariable()) &&
                               _ahead4.Value == "^" && _ahead5.Value == "*" && (_ahead6.Value == "+") &&
                               (_ahead3.IsVariable() || ( double.Parse(_ahead3.Value) > double.Parse(_prev3.Value) ) ) )

                        {
                            Log($"{_ahead}{_ahead2}{_ahead4}{_ahead3} swapped with {_prev5}{_prev4}{_prev2}{_prev3} under Rule 2.");

                            results.Pop(5);
                            i += 6;

                            results.Add(_ahead);
                            results.Add(_ahead2);
                            results.Add(_ahead3);
                            results.Add(_ahead4);
                            results.Add(_ahead5);

                            results.Add(_token);

                            results.Add(_prev5);
                            results.Add(_prev4);
                            results.Add(_prev3);
                            results.Add(_prev2);
                            results.Add(_prev);

                            results.Add(_ahead6);
                        }
                        //p(5-1) a6 t a(1-5)
                        //1 x 2 ^ * + 1 x 4 ^ * +
                        else if (!_prev5.IsNull() && !_prev4.IsNull() && !_prev3.IsNull() && !_prev2.IsNull() &&
                                 !_prev.IsNull() &&
                                 !_ahead6.IsNull() && !_ahead5.IsNull() && !_ahead4.IsNull() && !_ahead3.IsNull() &&
                                 !_ahead2.IsNull() && !_ahead.IsNull() &&
                                 _prev5.IsConstant() && _prev4.IsVariable() && 
                                 (_prev3.IsVariable() || _prev3.IsNumber()) &&
                                 _prev2.Value == "^" && _prev.Value == "*" && _token.IsNumber() &&
                                 (_ahead.IsVariable() || _ahead.IsNumber()) && _ahead2.IsNumber() &&
                                 _ahead3.Value == "^" && _ahead4.Value == "*" && _ahead5.Value == "+" && _ahead6.Value == "+"
                        )
                        {
                            i += 6;
                            results.Pop(5);

                            Log($"{_prev5}{_prev4}{_prev2}{_prev3} swapped with {_token}{_ahead}{_ahead3}{_ahead4} under Rule 3.");

                            results.Add(_prev5);
                            results.Add(_prev4);
                            results.Add(_prev3);
                            results.Add(_prev2);
                            results.Add(_prev);

                            results.Add(_ahead6);
                            results.Add(_token);

                            results.Add(_ahead);
                            results.Add(_ahead2);
                            results.Add(_ahead3);
                            results.Add(_ahead4);
                            results.Add(_ahead5);
                        }
                        //5 4 3 2 1 t a
                        //1 x 1 ^ * 2 + 
                        //t p(1-5) a
                        else if (_prev5.IsNumber() && _prev4.IsVariable() && _prev3.IsNumber() && 
                                 (!_prev2.IsNull() && _prev2.Value == "^") &&
                                 (!_prev.IsNull() && _prev.Value == "*") &&
                                 (!_ahead.IsNull() && _ahead.Value == "+") &&
                                 _token.IsNumber()
                                )
                        {
                            results.Pop(5);
                            i += 1;

                            results.Add(_token);
                            results.Add(_prev5);
                            results.Add(_prev4);
                            results.Add(_prev3);
                            results.Add(_prev2);
                            results.Add(_prev);

                            results.Add(_ahead);

                        }

                        //2 + 1 x 1 ^ * +

                        //5 4  3  2 1 t  1  2 3 4  5 6          
                        //1 x  2  ^ * 1  x  4 ^ *  + + 
                        //c x c|x ^ * c c|x c ^ *  + + ->

                        else
                        {
                            results.Add(_token);
                        }
                    }

                    if (!tokens.SequenceEqual(results))
                    {
                        tokens = results;
                        continue;
                    }

                    return results;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void GenerateState(ref List<Token> tokens, int i)
            {
                _prev6 = (i > 5) ? _prev5 : _null;
                _prev5 = (i > 4) ? _prev4 : _null;
                _prev4 = (i > 3) ? _prev3 : _null;
                _prev3 = (i > 2) ? _prev2 : _null;
                _prev2 = (i > 1) ? _prev : _null;
                _prev = (i > 0) ? _token : _null;
                _token = tokens[i];
                _ahead = ((i + 1) < tokens.Count)  ? tokens[i + 1] : _null;
                _ahead2 = ((i + 2) < tokens.Count) ? tokens[i + 2] : _null;
                _ahead3 = ((i + 3) < tokens.Count) ? tokens[i + 3] : _null;
                _ahead4 = ((i + 4) < tokens.Count) ? tokens[i + 4] : _null;
                _ahead5 = ((i + 5) < tokens.Count) ? tokens[i + 5] : _null;
                _ahead6 = ((i + 6) < tokens.Count) ? tokens[i + 6] : _null;
            }

            private void Log(string message)
            {
                Logger?.Invoke(this, message.Alias());
            }

            private static Token GenerateNull() => new Token { Type = Type.Null };
        }
    }
}