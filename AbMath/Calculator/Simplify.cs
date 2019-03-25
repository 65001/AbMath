using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Calculator
{
    public partial class RPN
    {

        public class Simplify
        {
            private readonly DataStore _dataStore;
            private Term _prev;
            private Term _token;
            private Term _ahead;


            public Simplify(DataStore dataStore)
            {
                _dataStore = dataStore;
            }

            public List<Term> Apply(List<Term> tokens)
            {
                List<Term> results = new List<Term>();
                Term _null = GenerateNull();
                for (int i = 0; i < tokens.Count; i++)
                {
                    _prev = (i > 0) ? _token : _null;
                    _token = tokens[i];
                    _ahead = ((i + 1) < tokens.Count) ? tokens[i + 1] : _null;

                    //x - x
                    //c - c
                    if (!_prev.IsNull() && !_ahead.IsNull() &&_prev.Value == _ahead.Value && _token.Value == "-")
                    {
                        results.RemoveAt(results.Count - 1);
                        results.Add(new Term {Arguments = 0, Type = Type.Number, Value = "0"});
                        i++;
                    }
                    //Explicit zero multiplication
                    else if (!_prev.IsNull() && !_ahead.IsNull() && _token.Value == "*" && (_prev.Value == "0" || _ahead.Value == "0"))
                    {
                        results.RemoveAt(results.Count - 1);
                        results.Add(new Term { Arguments = 0, Type = Type.Number, Value = "0" });
                        i++;
                    }
                    else
                    {
                        results.Add(_token);
                    }
                }

                return results;
            }

            private static Term GenerateNull() => new Term { Type = Type.Null };
        }
    }
}
