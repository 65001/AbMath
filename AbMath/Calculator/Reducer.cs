using System;
using System.Collections.Generic;
using System.Text;
using CLI;

namespace AbMath.Calculator
{
    public partial class RPN
    {

        /// <summary>
        /// The Reducer class simplifies complex expressions
        /// that can be simplified
        /// </summary>
        public class Reducer
        {
            private DataStore _data;

            private Token _prev;
            private Token _token;
            private Token _ahead;
            private Token _null;

            public Reducer(DataStore data)
            {
                _data = data;
            }

            public Token[] Reduce(Queue<Token> tokens)
            {
                _null = GenerateNull();
                string action = string.Empty;
                string type = string.Empty;

                var Tables = new Tables<string>(new Config() {Format = Format.Default,Title = "Reducer"});
                Tables.GenerateHeaders();

                for (int i = 0; i < tokens.Count; i++)
                {
                    _prev = (i > 0) ? _token : _null;
                    _token = (i > 0) ? _ahead : tokens.Dequeue();
                    _ahead = ((i + 1) < tokens.Count) ? tokens.Dequeue() : _null;

                    //Abs Rule: 2 ^ sqrt -> abs
                    if (!_prev.IsNull() && !_ahead.IsNull() &&
                        _prev.Value == "2"  &&
                        _token.Value == "^" &&
                        _ahead.Value == "sqrt"
                        ) {

                        i++;
                    }
                    else
                    {
                        tokens.Enqueue(_prev);
                    }
                }

                tokens.Enqueue(_prev);

                return tokens.ToArray();
            }

            private static Token GenerateNull() => new Token { Type = Type.Null };
        }
    }
}
