using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace AbMath.Calculator
{
    public partial class RPN
    {

        public class PreSimplify
        {
            private readonly DataStore _dataStore;

            private Term _prev5;
            private Term _prev4;
            private Term _prev3;
            private Term _prev2;
            private Term _prev;
            private Term _token;
            private Term _ahead;
            private Term _ahead2;
            private Term _ahead3;
            private Term _ahead4;

            private static int itteration;
            public event EventHandler<string> Logger;

            public PreSimplify(DataStore dataStore)
            {
                _dataStore = dataStore;
                itteration = 0;
            }

            public List<Term> Apply(List<Term> tokens)
            {
                List<Term> expanded = expand(tokens);
                List<Term> simplified = simplify(expanded);
                List<Term> compressed = compress(simplified);

                
                Log($"Original Tokens: {tokens.ToArray().Print()}");
                Log($"Expanded Tokens: {expanded.ToArray().Print()}");
                Log($"Simplified Tokens: {simplified.ToArray().Print()}");
                Log($"Compressed Tokens: {compressed.ToArray().Print()}");
                

                return compressed;
            }

            public List<Term> simplify(List<Term> tokens)
            {
                List<Term> results = new List<Term>(tokens.Count);
                Term _null = GenerateNull();

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
                    
                    //p_5 must be a positive addition operator
                    //p_4   p_3 p_2 p_1 t   a_1 a_2 a_3 a_4
                    //2     x   ^   1   +   3   x   ^   1 -> (p_4 + a_1)p_3 p_2 p _1
                    //2     x   ^   1   -   3   x   ^   1 -> (p_4 - a_1)p_3 p_2 p _1
                    //p_3 = a_2
                    if ( ( _prev5.IsNull() || !_prev5.IsNull() && (_prev5.Value == "+" || _prev5.Value == "(")) &&
                         !_prev4.IsNull() && _prev4.IsNumber() &&
                         !_prev3.IsNull() && _prev3.IsVariable() &&
                         !_prev2.IsNull() && _prev2.IsOperator() && _prev2.Value == "^" &&
                         !_prev.IsNull() &&  ( _prev.IsNumber() || _prev.IsVariable() ) && 
                         _token.IsOperator() && (_token.Value == "+" || _token.Value == "-") &&
                         !_ahead.IsNull() && _ahead.IsNumber() &&
                         !_ahead2.IsNull() && _ahead2.IsVariable() &&
                         !_ahead3.IsNull() && _ahead3.IsOperator() && _ahead3.Value == "^" &&
                         !_ahead4.IsNull() && ( _ahead4.IsNumber() || _ahead4.IsVariable() ) &&
                         _prev3.Value == _ahead2.Value && //ensures that the variables are equal to each other
                         _prev.Value == _ahead4.Value //ensures that the degree is the same
                         )
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            results.RemoveAt(results.Count - 1);
                        }

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
                        
                        results.Add(new Term() {Arguments = 0, Type = Type.Number, Value = result.ToString()} );
                        results.Add(_prev3);
                        results.Add(_prev2);
                        results.Add(_prev);
                        i += 4;
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

            public List<Term> expand(List<Term> tokens)
            {
                List<Term> results = new List<Term>(tokens.Count);

                Term _null = GenerateNull();
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
                            results.Add(new Term() {Arguments = 0,Type = Type.Number, Value = "1"});
                        }

                        if (!_ahead.IsNull() && _ahead.IsNumber())
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
                            results.Add(new Term() {Arguments = 2,Type = Type.Operator, Value = "^"});
                            results.Add(new Term() { Arguments = 0, Type = Type.Number, Value = "1" });
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

            public List<Term> compress(List<Term> tokens)
            {
                List<Term> results = new List<Term>(tokens.Count);
                Term _null = GenerateNull();

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
                    else
                    {
                        results.Add(_token);
                    }
                }

                return results;
            }

            private static Term GenerateNull() => new Term { Type = Type.Null };

            private void Log(string message)
            {
                Logger?.Invoke(this, message);
            }
        }
    }
}
