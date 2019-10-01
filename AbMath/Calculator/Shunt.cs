using System;
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

            //TODO: The _output queue and the _operator stack must be redone to add AST support
            private List<Token> _output;

            private Stack<Token> _operator;

            private Stack<Node> AST;
            //An AST node only gets created when we have a function or operator present

            private Token _prev;
            private Token _token;
            private Token _ahead;
            private Token _ahead2;

            private Token _multiply;
            private Token _division;
            private Token _null;

            private Tables<string> _tables;
            private Tables<string> _arityTables;

            //Variadic Function
            //See http://wcipeg.com/wiki/Shunting_yard_algorithm#Variadic_functions
            //See https://web.archive.org/web/20181008151605/http://wcipeg.com/wiki/Shunting_yard_algorithm#Variadic_functions
            private Stack<int> _arity;

            public event EventHandler<string> Logger;

            public Shunt(DataStore dataStore)
            {
                _dataStore = dataStore;

                _multiply = new Token("*", 2, Type.Operator);
                _division = new Token("/", 2, Type.Operator);

                _null = new Token()
                {
                    Type = Type.Null
                };
            }

            public Token[] ShuntYard(List<Token> tokens)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                _output = new List<Token>(tokens.Count + 10);
                _operator = new Stack<Token>(5);
                _arity = new Stack<int>(5);

                if (_dataStore.DebugMode)
                {
                    _tables = new Tables<string>(new Config { Title = "Shunting Yard Algorithm", Format = _dataStore.DefaultFormat });
                    _tables.Add(new Schema {Column = "#", Width = 3});
                    _tables.Add(new Schema {Column = "Token", Width = 10});
                    _tables.Add(new Schema {Column = "Stack Count", Width = 12});
                    _tables.Add(new Schema {Column = "Stack ", Width = 12});
                    _tables.Add(new Schema {Column = "Arity", Width = 5});
                    _tables.Add(new Schema {Column = "Arity Peek", Width = 11});
                    _tables.Add(new Schema {Column = "Type", Width = 12});
                    _tables.Add(new Schema {Column = "Left | Right", Width = 10});
                    _tables.Add(new Schema {Column = "RPN", Width = 20});
                    _tables.Add(new Schema {Column = "Action", Width = 7});
                }

                string action = string.Empty;
                string type = string.Empty;


                for (int i = 0; i < tokens.Count; i++)
                {
                    _prev = (i > 0) ? _token : _null;
                    _token = tokens[i]; 

                    if (i < tokens.Count - 2)
                    {
                        _ahead = tokens[i + 1];
                        _ahead2 = tokens[i + 2];
                    }
                    else if (i < tokens.Count - 1)
                    {
                        _ahead = tokens[i + 1];
                        _ahead2 = _null;
                    }
                    else
                    {
                        _ahead = _null;
                        _ahead2 = _null;
                    }

                    action = string.Empty;
                    type = string.Empty;

                    bool Left = LeftImplicit();
                    bool Right = RightImplicit();
                    
                    //Unary Input at the start of the input or 
                    if ( i == 0 && _ahead != null && _dataStore.IsUnary(_token.Value) && _ahead.IsNumber())
                    {
                        type = "Start of Sequence Unary";
                        _ahead.Value = (double.Parse(tokens[i + 1].Value) * -1).ToString();
                        tokens[i + 1] = _ahead;
                    }
                    //TODO: Unary Input after another operator or left parenthesis
                    else if (Left && Right)
                    {
                        type = "Chain Multiplication";
                        //Right
                        Implicit();
                        //Left
                        OperatorRule(_multiply);
                    }
                    else if ( _prev != null && _prev.IsDivision() && (Left || Right))
                    {
                        //Case for 8/2(2 + 2)
                        //Case of 1/2x
                        
                        if (_dataStore.ImplicitMultiplicationPriority)
                        {
                            type = "Mixed division and multiplication. Implicit Multiplication has priority.";

                            OperatorPop();
                            _output.Add(_token);

                            //Implicit Multiplication supersedes division
                            _operator.Push(_division);
                            _operator.Push(_multiply);
                        }
                        else
                        {
                            type = "Mixed division and multiplication";
                            _output.Add(_token);

                            if (Left)
                            {
                                OperatorRule(_multiply);
                            }

                            if (Right)
                            {
                                Implicit();
                            }

                            _operator.Push(_division);
                            OperatorPop();
                        }
                    }
                    //2 x (
                    //2 x sin
                    else if (_prev != null && _ahead != null && _prev.IsNumber() && _token.IsVariable() && ( _ahead.IsLeftBracket() || _ahead.IsFunction() ))
                    {
                        type = "Variable Chain Multiplication";
                        _output.Add(_token);
                        _output.Add(_multiply);
                    }
                    else if (Left)
                    {
                        //This will flip the order of the multiplication :(
                        type = "Implicit Left";
                        Implicit();
                    }
                    else if (_prev != null && _ahead != null && (_prev.IsRightBracket() && _token.IsLeftBracket()) || (_prev.IsVariable() && _token.IsNumber()) || (_prev.IsConstant() && _token.IsLeftBracket() && (_ahead.IsNumber() || _ahead.IsFunction()))) 
                    {
                        type = "Implicit Left 2";
                        OperatorRule(_multiply);
                        _operator.Push(_token);
                    }
                    else if (Right)
                    {
                        type = "Implicit Right";
                        Implicit();
                    }
                    else if (_prev != null && _prev.IsRightBracket() && !_prev.IsComma() && _token.IsFunction())
                    {
                        type = "Implicit Left Functional";
                        OperatorRule(_multiply);
                        WriteFunction(_token);
                    }
                    else
                    {
                        Stopwatch SW = new Stopwatch();
                        SW.Start();

                        switch (_token.Type)
                        {
                            case Type.Number: 
                                action = "Added token to output";
                                type = "Number";
                                _output.Add(_token);
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
                                _output.Add(_token);
                                break;
                            default:
                                throw new NotImplementedException(_token.Value);
                        }

                        SW.Stop();
                        _dataStore.AddTimeRecord("Shunt.Shunting", SW);
                    }

                    if (_dataStore.DebugMode)
                    {
                        var print = new[]
                        {
                            i.ToString(), _token.Value, _operator.Count.ToString(),
                            _operator.Print() ?? string.Empty, _arity.Print(), _arity.SafePeek().ToString(),
                            type, $"{Left} | {Right}",
                            _output.Print(), action
                        };
                        _tables.Add(print);
                    }
                }

                if (_dataStore.DebugMode)
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    Write(_tables.ToString());
                    if (_tables.SuggestedRedraw)
                    {
                        Write(_tables.Redraw());
                    }
                    Write("");

                    _dataStore.AddTimeRecord("Shunt.Debug", stopwatch);
                }
                Dump();

                if (_dataStore.DebugMode)
                {
                    _arityTables = new Tables<string>(new Config { Title = "Arity", Format = _dataStore.DefaultFormat });
                    _arityTables.Add(new Schema {Column = "#", Width = 3});
                    _arityTables.Add(new Schema {Column = "Token", Width = 10});
                    _arityTables.Add(new Schema {Column = "Arity", Width = 5});
                }
                
                for (int i = 0; i < _output.Count; i++)
                {
                    Token token = _output[i];

                    if (_dataStore.DebugMode)
                    {
                        string[] message = {i.ToString(), token.Value, token.Arguments.ToString()};
                        _arityTables.Add(message);
                    }

                    if (token.IsFunction() && !token.IsConstant())
                    {
                        Function function = _dataStore.Functions[token.Value];
                        //See if we can apply casting
                        //Cast sum to total if it has more than the possible arguments since thats what the user probably wanted
                        if (token.Value == "sum" && token.Arguments > function.MinArguments)
                        {
                            Write("Casting sum to total since it exceeds max arguments for sum");
                            _output[i] = new Token("total", token.Arguments, RPN.Type.Function);
                        }
                        //The function has an incorrect number of arguments!
                        else if (function.MinArguments > token.Arguments || token.Arguments > function.MaxArguments)
                        {
                            throw new InvalidOperationException($"The function {token.Value} expected between {function.MinArguments} to {function.MaxArguments} arguments but has received {token.Arguments} instead.");
                        }

                    }
                }
                

                if (_dataStore.DebugMode)
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    Write(_arityTables.ToString());

                    if (_arityTables.SuggestedRedraw)
                    {
                        Write(_arityTables.Redraw());
                    }

                    _dataStore.AddTimeRecord("Shunt.Debug", stopwatch);
                }

                if (_arity.Count > 0)
                {
                    Write($"Arity Count {_arity.Count}");
                    Write($"Arity Peek {_arity.SafePeek()}");

                    throw new InvalidOperationException("Arity not completely assigned");
                }

                sw.Stop();
                _dataStore.AddTimeRecord("Shunting", sw);

                Write("");
                Write($"RPN : {_output.Print()}");
                Write("");

                return _output.ToArray();
            }

            void Implicit()
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();

                OperatorRule(_multiply);
                _output.Add(_token);
                _dataStore.AddTimeRecord("Shunt.Implicit", SW);
            }

            void RightBracketRule(Token token)
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();

                while (!_operator.Peek().IsLeftBracket())
                {
                    if (_operator.Count == 0)
                    {
                        throw new ArgumentException("Error : Mismatched Brackets or Parentheses.");
                    }
                    _output.Add(OperatorPop());
                }

                //For functions and composite functions the to work, we must return now.
                if (token.IsComma())
                {
                    _arity.Push(_arity.Pop() + 1);
                    _dataStore.AddTimeRecord("Shunt.RightBracketRule", SW);
                    return;
                }

                //Pops the left bracket or Parentheses from the stack. 
                OperatorPop();
                _dataStore.AddTimeRecord("Shunt.RightBracketRule", SW);
            }

            //Sort Stack equivalent in sb
            private void OperatorRule(Token token)
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();

                while (DoOperatorRule(token))
                {
                    _output.Add(OperatorPop());
                }
                _operator.Push(token);

                _dataStore.AddTimeRecord("Shunt.OperatorRule", SW);
            }

            private bool DoOperatorRule(Token token)
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();

                if (_operator.Count == 0 || _operator.Peek().IsLeftBracket())
                {
                    _dataStore.AddTimeRecord("Shunt.OpRule", SW);
                    return false;
                }

                if (_operator.Peek().IsFunction())
                {
                    _dataStore.AddTimeRecord("Shunt.OpRule", SW);
                    return true;
                }

                Operator peek = _dataStore.Operators[_operator.Peek().Value];
                Operator op = _dataStore.Operators[token.Value];

                if (peek.Weight > op.Weight || (peek.Weight == op.Weight && op.Assoc == Assoc.Left) )
                {
                    _dataStore.AddTimeRecord("Shunt.OpRule", SW);
                    return true;
                }

                _dataStore.AddTimeRecord("Shunt.OpRule", SW);
                return false;
            }

            private bool LeftImplicit()
            {
                return _ahead != null && (_token.IsNumber() || _token.IsVariable()) && (_ahead.IsFunction() || _ahead.IsLeftBracket() || _ahead.IsVariable());
            }

            private bool RightImplicit()
            {
                return _prev != null && !_prev.IsComma() &&  (_prev.IsRightBracket() || _prev.IsVariable()) && (_token.IsNumber() || _token.IsVariable());
            }

            private Token OperatorPop()
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();
                //TODO: We can use this to generate an AST directly. 
                Token temp = _operator.Pop();
                if (temp.IsFunction())
                {
                    temp.Arguments = _arity.Pop();
                }
                SW.Stop();
                _dataStore.AddTimeRecord("Shunt.OperatorPop", SW);
                return temp;
            }

            private void WriteFunction(Token function)
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();
                _operator.Push(function);
                
                if (_dataStore.Functions[function.Value].Arguments > 0)
                {
                    _arity.Push(1);
                }
                else
                {
                    _arity.Push(0);
                }
                SW.Stop();
                _dataStore.AddTimeRecord("Shunt.WriteFunc", SW);
            }

            /// <summary>
            /// Moves all remaining data from the stack onto the queue
            /// </summary>
            private void Dump()
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();

                while (_operator.Count > 0)
                {
                    Token peek = _operator.Peek();

                    if (peek.IsLeftBracket() || peek.IsRightBracket() )
                    {
                        if (!_dataStore.AllowMismatchedParentheses)
                        {
                            throw new ArgumentException("Error: Mismatched Parentheses or Brackets");
                        }

                        OperatorPop();
                    }
                    else
                    {
                        _output.Add(OperatorPop());
                    }
                }
                SW.Stop();
                _dataStore.AddTimeRecord("Shunt.Dump", SW);
            }

            void Write(string message)
            {
                Logger?.Invoke(this, message);
            }
        }
    }
}