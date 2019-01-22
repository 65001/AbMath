using System;
using System.Collections.Generic;
using System.Diagnostics;
using CLI;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        ///<summary>
        /// Takes a list of tokens and returns a Queue of Tokens after Order of Operations has been 
        /// taken into consideration.
        ///</summary>
        public class Shunt : IShunt<Term>
        {
            private readonly DataStore _dataStore;
            private Queue<Term> _output;
            private Stack<Term> _operator;

            private Term _prev;
            private Term _token;
            private Term _ahead;

            //TODO: Implement Variadic Function
            //See http://wcipeg.com/wiki/Shunting_yard_algorithm#Variadic_functions
            private Stack<int> _arity;

            public event EventHandler<string> Logger;

            public Shunt(DataStore dataStore)
            {
                _dataStore = dataStore;
            }

            public Term[] ShuntYard(List<Term> tokens)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                _output = new Queue<Term>(tokens.Count);
                _operator = new Stack<Term>(5);

                _arity = new Stack<int>();

                var tables = new Tables<string>(new Config {Title = "Shunting Yard Algorithm", Format = _dataStore.DefaultFormat});
                if (_dataStore.DebugMode)
                {
                    tables.Add(new Schema {Column = "#", Width = 3});
                    tables.Add(new Schema {Column = "Token", Width = 10});
                    tables.Add(new Schema {Column = "Stack Count", Width = 15});
                    tables.Add(new Schema {Column = "Stack Peek", Width = 12});
                    tables.Add(new Schema {Column = "Arity", Width = 5});
                    tables.Add(new Schema {Column = "Type", Width = 15});
                    tables.Add(new Schema {Column = "RPN", Width = 20});
                    tables.Add(new Schema {Column = "Action", Width = 30});

                    Write(tables.GenerateHeaders());
                }

                string action = string.Empty;
                string type = string.Empty;

                Term _null = GenerateNull();
                for (int i = 0; i < tokens.Count; i++)
                {
                    _prev = (i > 0) ? _token : _null;
                    _token = tokens[i]; 
                    _ahead = ((i + 1) < tokens.Count)? tokens[i + 1] : _null;

                    action = string.Empty;
                    type = string.Empty;


                    if (Chain())
                    {
                        type = "Chain Multiplication";
                        //Right
                        Implicit();
                        //Left
                        OperatorRule(GenerateMultiply());
                    }
                    else if (!_prev.IsNull() 
                             && !_ahead.IsNull() 
                             && _prev.IsOperator() 
                             && _prev.Value == "/" 
                             && _token.IsNumber() 
                             && _ahead.IsVariable() )
                    {
                        //Case for 1/2x -> 1/(2x)
                        //Postfix : 1 2 x * /
                        //Prev : Operator : /
                        //Current : Number
                        //Ahead : Variable 
                        type = "Mixed division and multiplication";
                        _operator.Pop();
                        _output.Enqueue(_token);

                        _operator.Push(GenerateDivision());
                        _operator.Push(GenerateMultiply());
                    }
                    else if (LeftImplicit())
                    {
                        //This will flip the order of the multiplication :(
                        type = "Implicit Left";
                        Implicit();
                    }
                    else if (!_prev.IsNull()  
                             && (_prev.IsRightBracket() && _token.IsLeftBracket()) 
                             || (_prev.IsVariable() && _token.IsNumber()) 
                             || (_prev.IsConstant() && _token.IsLeftBracket() && (_ahead.IsNumber() || _ahead.IsFunction()))
                             ) 
                    {
                        type = "Implicit Left 2";
                        OperatorRule(GenerateMultiply());
                        _operator.Push(_token);
                    }
                    else if (RightImplicit())
                    {
                        type = "Implicit Right";
                        Implicit();
                    }
                    else if (_prev.IsRightBracket() && !_prev.IsComma() && _token.IsFunction())
                    {
                        type = "Implicit Left Functional";
                        OperatorRule(GenerateMultiply());
                        WriteFunction(_token);
                    }
                    else
                    {
                        switch (_token.Type)
                        {
                            case Type.Number: 
                                action = "Added token to output";
                                type = "Number";
                                _output.Enqueue(_token);
                                break;
                            case Type.Function:
                                action = "Added token to stack";
                                type = "Function";
                                WriteFunction(_token);
                                break;
                            case Type.Operator:
                                type = "Operator";
                                action = "Operator Rules";
                                OperatorRule(_token);
                                break;
                            case Type.LParen:
                                type = "Left Bracket";
                                action = "Added token to stack";
                                _operator.Push(_token);
                                break;
                            case Type.RParen:
                                type = "Right Bracket";
                                action = "Right Bracket Rules";
                                if (_token.IsComma())
                                {
                                    type = "Comma";
                                }
                                RightBracketRule(_token);
                                break;
                            case Type.Variable:
                                action = "Added token to output";
                                type = "Variable";
                                _output.Enqueue(_token);
                                _dataStore.AddVariable(_token.Value);
                                break;
                            default:
                                throw new NotImplementedException(_token.Value);
                        }
                    }

                    if (_dataStore.DebugMode)
                    {
                        var print = new[]
                        {
                            i.ToString(), _token.Value, _operator.Count.ToString(),
                            _operator.SafePeek().Value ?? string.Empty, _arity.SafePeek().ToString(), type,
                            _output.Print(), action
                        };
                        tables.Add(print);

                        Write(tables.GenerateNextRow());
                    }
                }
                Dump();

                if (_dataStore.DebugMode)
                {
                    Write(tables.GenerateFooter());
                }

                if (_dataStore.DebugMode && tables.SuggestedRedraw)
                {
                    Write(tables.Redraw());
                }

                if (_dataStore.DebugMode)
                {
                    Write("");
                }

                Tables<string> arityTables = new Tables<string>(new Config { Title = "Arity", Format = _dataStore.DefaultFormat });

                if (_dataStore.DebugMode)
                {
                    arityTables.Add(new Schema {Column = "#", Width = 3});
                    arityTables.Add(new Schema {Column = "Token", Width = 10});
                    arityTables.Add(new Schema {Column = "Arity", Width = 5});
                    Write(arityTables.GenerateHeaders());
                }

                //TODO: Eliminate
                //Ensures that all functions are within their stated max and min arguments
                for (int i = 0; i < _output.Count; i++)
                {
                    Term term = _output.Dequeue();
                    if (term.IsFunction())
                    {
                        Function function = _dataStore.Functions[term.Value];
                        term.Arguments = Math.Max(function.MinArguments, Math.Min( term.Arguments, function.MaxArguments));
                    }

                    if (_dataStore.DebugMode)
                    {
                        string[] message = {i.ToString(), term.Value, term.Arguments.ToString()};
                        arityTables.Add(message);
                        Write(arityTables.GenerateNextRow());
                    }

                    _output.Enqueue(term);
                }

                if (_dataStore.DebugMode)
                {
                    Write(arityTables.GenerateFooter());

                    if (arityTables.SuggestedRedraw)
                    {
                        Write(arityTables.Redraw());
                    }
                }

                if (_arity.Count > 0)
                {
                    Write($"Arity Count {_arity.Count}");
                    Write($"Arity Peek {_arity.SafePeek()}");

                    throw new InvalidOperationException("Arity not completely assigned");
                }

                Write("");

                sw.Stop();
                Write($"Shunting Time {sw.ElapsedMilliseconds} (ms). Elapsed Ticks: {sw.ElapsedTicks.ToString("N0")}");
                _dataStore.TotalMilliseconds += sw.ElapsedMilliseconds;
                _dataStore.TotalSteps += sw.ElapsedTicks;

                
                if (_dataStore.MarkdownTables)
                {
                    Write($"Reverse Polish Notation:\n``{_output.Print()}``");
                }
                else
                {
                    Write($"Reverse Polish Notation:\n{_output.Print()}");
                }

                Write("");

                return _output.ToArray();
            }

            void Implicit()
            {
                OperatorRule(GenerateMultiply());
                _output.Enqueue(_token);
                if (_token.IsVariable())
                {
                    _dataStore.AddVariable(_token.Value);
                }
            }

            void RightBracketRule(Term token)
            {
                while (!_operator.Peek().IsLeftBracket())
                {
                    if (_operator.Count == 0)
                    {
                        throw new ArgumentException("Error : Mismatched Brackets or Parentheses.");
                    }

                    Term output = _operator.Pop();
                    //This ensures that only functions 
                    //can have variable number of arguments
                    if (output.IsFunction() )
                    {
                        output.Arguments = _arity.Pop();
                    }
                    _output.Enqueue(output);
                }

                //For functions and composite functions the to work, we must return now.
                if (token.IsComma())
                {
                    _arity.Push(_arity.Pop() + 1);
                    return;
                }

                //Pops the left bracket or Parentheses from the stack. 
                _operator.Pop();
            }

            //Sort Stack equivalent in sb
            void OperatorRule(Term token)
            {
                bool go = true;
                while (DoOperatorRule(token) && go)
                {
                    _output.Enqueue(_operator.Pop());

                    if (_operator.Count == 0)
                    {
                        go = false;
                    }
                }
                _operator.Push(token);
            }

            bool DoOperatorRule(Term Token)
            {
                try
                { 
                    return _operator.Count > 0 && !_operator.Peek().IsLeftBracket() &&
                            (
                                _operator.Peek().IsFunction() ||
                                (_dataStore.Operators[_operator.Peek().Value].Weight > _dataStore.Operators[Token.Value].Weight) ||
                                (_dataStore.Operators[_operator.Peek().Value].Weight == _dataStore.Operators[Token.Value].Weight 
                                 && _dataStore.Operators[Token.Value].Assoc == Assoc.Left)
                            );
                }
                catch (Exception ex) { }
                return false;
            }

            private bool LeftImplicit()
            {
                return !_ahead.IsNull() && (_token.IsNumber() || _token.IsVariable()) && (_ahead.IsFunction() || _ahead.IsLeftBracket() || _ahead.IsVariable());
            }

            private bool RightImplicit()
            {
                return !_prev.IsNull() && !_prev.IsComma() && _prev.IsRightBracket() && (_token.IsNumber() || _token.IsVariable());
            }

            private bool Chain()
            {
                return LeftImplicit() && RightImplicit();
            }

            private void WriteFunction(Term function)
            {
                _operator.Push(function);
                
                if (_dataStore.Functions[function.Value].Arguments > 0)
                {
                    _arity.Push(1);
                }
                else
                {
                    _arity.Push(0);
                }
            }

            private static Term GenerateMultiply() => new Term {
                Value = "*", Arguments = 2, Type = Type.Operator
            };

            private static Term GenerateDivision() => new Term
            {
                Arguments = 2, Type = Type.Operator, Value = "/"
            };

            private static Term GenerateNull() => new Term { Type = Type.Null };

            /// <summary>
            /// Moves all remaining data from the stack onto the queue
            /// </summary>
            void Dump()
            {
                while (_operator.Count > 0)
                {
                    Term peek = _operator.Peek();

                    if (peek.Type == Type.LParen || peek.Type == Type.RParen)
                    {
                        if (!_dataStore.AllowMismatchedParentheses)
                        {
                            throw new ArgumentException("Error: Mismatched Parentheses or Brackets");
                        }

                        _operator.Pop();
                    }
                    else
                    {
                        var output = _operator.Pop();
                        _output.Enqueue(output);
                    }
                }

                while ( _arity.Count > 0)
                {
                    for (int i = 0; i < (_output.Count - 1); i++)
                    {
                        _output.Enqueue( _output.Dequeue() );
                    }

                    var foo = _output.Dequeue();

                    if (foo.IsFunction())
                    {
                        foo.Arguments = _arity.Pop();
                    }
                    else
                    {
                        _arity.Pop();
                    }

                    _output.Enqueue(foo);
                }
                
            }

            void Write(string message)
            {
                Logger?.Invoke(this, message);
            }
        }
    }
}