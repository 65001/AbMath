﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CLI;

namespace AbMath.Calculator
{
    public partial class RPN
    {
        ///<summary>
        /// Takes a list of tokens and returns a Queue of Tokens after Order of Operations has been 
        /// taken into consideration.
        ///</summary>
        public class Shunt : IShunt<Token>
        {
            private readonly DataStore _dataStore;
            private Queue<Token> _output;
            private Stack<Token> _operator;

            private Token _prev;
            private Token _token;
            private Token _ahead;

            //TODO: Implement Variadic Function
            //See http://wcipeg.com/wiki/Shunting_yard_algorithm#Variadic_functions
            private Stack<int> _arity;

            public event EventHandler<string> Logger;

            public Shunt(DataStore dataStore)
            {
                _dataStore = dataStore;
            }

            public Token[] ShuntYard(List<Token> tokens)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                _output = new Queue<Token>(tokens.Count);
                _operator = new Stack<Token>(5);

                _arity = new Stack<int>();

                var tables = new Tables<string>(new Config {Title = "Shunting Yard Algorithm", Format = _dataStore.DefaultFormat});
                if (_dataStore.DebugMode)
                {
                    tables.Add(new Schema {Column = "#", Width = 3});
                    tables.Add(new Schema {Column = "Token", Width = 10});
                    tables.Add(new Schema {Column = "Stack Count", Width = 12});
                    tables.Add(new Schema {Column = "Stack ", Width = 12});
                    tables.Add(new Schema {Column = "Arity", Width = 5});
                    tables.Add(new Schema {Column = "Type", Width = 12});
                    tables.Add(new Schema {Column = "RPN", Width = 20});
                    tables.Add(new Schema {Column = "Action", Width = 30});

                    Write(tables.GenerateHeaders());
                }

                string action = string.Empty;
                string type = string.Empty;

                Token _null = GenerateNull();
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
                    else if (!_prev.IsNull() && !_ahead.IsNull() &&
                             _prev.IsNumber() && _token.IsVariable() && _ahead.IsLeftBracket())
                    {
                        type = "Variable Chain Multiplication";
                        _output.Enqueue(_token);
                        _output.Enqueue(GenerateMultiply());
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
                            _operator.Print() ?? string.Empty, _arity.Print(), type,
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
                    Token token = _output.Dequeue();
                    if (token.IsFunction())
                    {
                        Function function = _dataStore.Functions[token.Value];
                        token.Arguments = Math.Max(function.MinArguments, Math.Min( token.Arguments, function.MaxArguments));
                    }

                    if (_dataStore.DebugMode)
                    {
                        string[] message = {i.ToString(), token.Value, token.Arguments.ToString()};
                        arityTables.Add(message);
                        Write(arityTables.GenerateNextRow());
                    }

                    _output.Enqueue(token);
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
                

                Stopwatch SI = new Stopwatch();

                PostSimplify PS = new PostSimplify(_dataStore);
                PS.Logger += Logger;
                Token[] complex = PS.Apply(_output.ToList()).ToArray();

                Write($"Complex RPN : {complex.Print()}");

                SI.Stop();

                _dataStore.AddTimeRecord(new TimeRecord()
                {
                    Type = "Shunting",
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    ElapsedTicks = sw.ElapsedTicks
                });

                _dataStore.AddTimeRecord(new TimeRecord()
                {
                    Type = "PostSimplify",
                    ElapsedMilliseconds = SI.ElapsedMilliseconds,
                    ElapsedTicks = SI.ElapsedTicks
                });

                _dataStore.TotalMilliseconds += sw.ElapsedMilliseconds + SI.ElapsedMilliseconds;
                _dataStore.TotalSteps += sw.ElapsedTicks + SI.ElapsedTicks;

                if (_dataStore.MarkdownTables)
                {
                    Write($"Raw Reverse Polish Notation:\n``{_output.Print()}``");
                }
                else
                {
                    Write($"Raw Reverse Polish Notation:\n{_output.Print()}");
                }

                Write("");

                return complex;
            }

            void Implicit()
            {
                OperatorRule(GenerateMultiply());
                _output.Enqueue(_token);
            }

            void RightBracketRule(Token token)
            {
                while (!_operator.Peek().IsLeftBracket())
                {
                    if (_operator.Count == 0)
                    {
                        throw new ArgumentException("Error : Mismatched Brackets or Parentheses.");
                    }

                    Token output = _operator.Pop();
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
            void OperatorRule(Token token)
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

            bool DoOperatorRule(Token Token)
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
                //p t a
                //3 x (
                return !_ahead.IsNull() && (_token.IsNumber() || _token.IsVariable()) && (_ahead.IsFunction() || _ahead.IsLeftBracket() || _ahead.IsVariable());
            }

            private bool RightImplicit()
            {
                //p t a
                //3 x (
                return !_prev.IsNull() && !_prev.IsComma() && _prev.IsRightBracket() && (_token.IsNumber() || _token.IsVariable());
            }

            private bool Chain()
            {
                return LeftImplicit() && RightImplicit();
            }

            private void WriteFunction(Token function)
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

            private static Token GenerateMultiply() => new Token {
                Value = "*", Arguments = 2, Type = Type.Operator
            };

            private static Token GenerateDivision() => new Token
            {
                Arguments = 2, Type = Type.Operator, Value = "/"
            };

            private static Token GenerateNull() => new Token { Type = Type.Null };

            /// <summary>
            /// Moves all remaining data from the stack onto the queue
            /// </summary>
            void Dump()
            {
                while (_operator.Count > 0)
                {
                    Token peek = _operator.Peek();

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

                        if (output.IsFunction() && _arity.Count > 0)
                        {
                            output.Arguments = _arity.Pop();
                        }

                        _output.Enqueue(output);
                    }
                }

                //This assigns arity values to the output stream
                for (int i = 0; i < _output.Count; i++)
                {
                    Token token = _output.Dequeue();

                    if (token.IsFunction() && _arity.Count > 0)
                    {
                        Function func = _dataStore.Functions[token.Value];
                        //Constants cannot have an arity assigned to them.
                        if (func.MaxArguments != 0)
                        {
                            token.Arguments = _arity.Pop();
                        }
                    }

                    _output.Enqueue( token );
                }

                //This in effect suppresses any Arity Exceptions!!     
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