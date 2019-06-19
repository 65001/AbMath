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

            private int _count = 0;
            private Token _prev;
            private Token _token;
            private Token _ahead;
            private Token _ahead2;

            private Token _multiply;
            private Token _division;
            private Token _null;

            //Variadic Function
            //See http://wcipeg.com/wiki/Shunting_yard_algorithm#Variadic_functions
            //See https://web.archive.org/web/20181008151605/http://wcipeg.com/wiki/Shunting_yard_algorithm#Variadic_functions
            private Stack<int> _arity;

            public event EventHandler<string> Logger;

            public Shunt(DataStore dataStore)
            {
                _dataStore = dataStore;

                _multiply = GenerateMultiply();
                _division = GenerateDivision();
                _null = GenerateNull();
            }

            public Token[] ShuntYard(List<Token> tokens)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                _output = new List<Token>(tokens.Count + 10);
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
                    tables.Add(new Schema {Column = "Arity Peek", Width = 11});
                    tables.Add(new Schema {Column = "Type", Width = 12});
                    tables.Add(new Schema {Column = "RPN", Width = 20});
                    tables.Add(new Schema {Column = "Action", Width = 7});
                }

                string action = string.Empty;
                string type = string.Empty;

                for (int i = 0; i < tokens.Count; i++)
                {
                    _prev = (i > 0) ? _token : _null;
                    _token = tokens[i]; 
                    _ahead = ((i + 1) < tokens.Count) ? tokens[i + 1] : _null;
                    _ahead2 = ((i + 2) < tokens.Count) ? tokens[i + 2] : _null;

                    action = string.Empty;
                    type = string.Empty;
                    
                    //Unary Input at the start of the input or 
                    if ( i == 0 && !_ahead.IsNull() && _dataStore.IsUnary(_token.Value) && _ahead.IsNumber())
                    {
                        type = "Start of Sequence Unary";
                        _ahead.Value = (double.Parse(tokens[i + 1].Value) * -1).ToString();
                        tokens[i + 1] = _ahead;
                    }
                    //TODO: Unary Input after another operator or left parenthesis
                    else if (Chain())
                    {
                        type = "Chain Multiplication";
                        //Right
                        Implicit();
                        //Left
                        OperatorRule(_multiply);
                    }
                    else if (!_prev.IsNull() && !_ahead.IsNull() && _prev.IsOperator() && _prev.Value == "/" && _token.IsNumber() && _ahead.IsVariable() )
                    {
                        //Case for 1/2x -> 1/(2x)
                        //Postfix : 1 2 x * /
                        //Prev : Operator : /
                        //Current : Number
                        //Ahead : Variable 
                        type = "Mixed division and multiplication";
                        OperatorPop();
                        _output.Add(_token);

                        _operator.Push(_division);
                        _operator.Push(_multiply);
                    }
                    //2 x (
                    //2 x sin
                    else if (!_prev.IsNull() && !_ahead.IsNull() && _prev.IsNumber() && _token.IsVariable() && ( _ahead.IsLeftBracket() || _ahead.IsFunction() ))
                    {
                        type = "Variable Chain Multiplication";
                        _output.Add(_token);
                        _output.Add(_multiply);
                    }
                    else if (LeftImplicit())
                    {
                        //This will flip the order of the multiplication :(
                        type = "Implicit Left";
                        Implicit();
                    }
                    else if (!_prev.IsNull()  && (_prev.IsRightBracket() && _token.IsLeftBracket()) || (_prev.IsVariable() && _token.IsNumber()) || (_prev.IsConstant() && _token.IsLeftBracket() && (_ahead.IsNumber() || _ahead.IsFunction()))) 
                    {
                        type = "Implicit Left 2";
                        OperatorRule(_multiply);
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
                        OperatorRule(_multiply);
                        WriteFunction(_token);
                    }
                    else
                    {
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
                    }

                    if (_dataStore.DebugMode)
                    {
                        var print = new[]
                        {
                            i.ToString(), _token.Value, _operator.Count.ToString(),
                            _operator.Print() ?? string.Empty, _arity.Print(), _arity.SafePeek().ToString(),
                            type, _output.Print(), action
                        };
                        tables.Add(print);
                    }
                }

                if (_dataStore.DebugMode)
                {
                    Write(tables.ToString());
                    if (tables.SuggestedRedraw)
                    {
                        Write(tables.Redraw());
                    }
                    Write("");
                }
                Dump();

                Tables<string> arityTables = new Tables<string>(new Config { Title = "Arity", Format = _dataStore.DefaultFormat });

                if (_dataStore.DebugMode)
                {
                    arityTables.Add(new Schema {Column = "#", Width = 3});
                    arityTables.Add(new Schema {Column = "Token", Width = 10});
                    arityTables.Add(new Schema {Column = "Arity", Width = 5});
                }

                
                for (int i = 0; i < _output.Count; i++)
                {
                    Token token = _output[i];

                    if (_dataStore.DebugMode)
                    {
                        string[] message = {i.ToString(), token.Value, token.Arguments.ToString()};
                        arityTables.Add(message);
                    }

                    if (token.IsFunction() && !token.IsConstant())
                    {
                        Function function = _dataStore.Functions[token.Value];
                        //The function has an incorrect number of arguments!
                        if (function.MinArguments > token.Arguments || token.Arguments > function.MaxArguments)
                        {
                            throw new InvalidOperationException($"The function {token.Value} expected between {function.MinArguments} to {function.MaxArguments} arguments but has received {token.Arguments} instead.");
                        }

                    }
                }
                

                if (_dataStore.DebugMode)
                {
                    Write(arityTables.ToString());

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

                _dataStore.AddTimeRecord(new TimeRecord()
                {
                    Type = "Shunting",
                    ElapsedMilliseconds = sw.ElapsedMilliseconds,
                    ElapsedTicks = sw.ElapsedTicks
                });

                Token[] complex = _output.ToArray();

                Write($"Complex RPN : {complex.Print()}");

                if (_output.Print() != complex.Print())
                {
                    if (_dataStore.MarkdownTables)
                    {
                        Write($"Raw Reverse Polish Notation:\n``{_output.Print()}``");
                    }
                    else
                    {
                        Write($"Raw Reverse Polish Notation:\n{_output.Print()}");
                    }
                }

                Write("");

                return complex;
            }

            void Implicit()
            {
                OperatorRule(_multiply);
                _output.Add(_token);
            }

            void RightBracketRule(Token token)
            {
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
                    return;
                }

                //Pops the left bracket or Parentheses from the stack. 
                OperatorPop();
            }

            //Sort Stack equivalent in sb
            private void OperatorRule(Token token)
            {
                while (DoOperatorRule(token))
                {
                    _output.Add(OperatorPop());
                }
                _operator.Push(token);
            }

            private bool DoOperatorRule(Token token)
            {
                if (_operator.Count == 0 || _operator.Peek().IsLeftBracket())
                {
                    return false;
                }

                if (_operator.Peek().IsFunction())
                {
                    return true;
                }

                Operator peek = _dataStore.Operators[_operator.Peek().Value];
                Operator op = _dataStore.Operators[token.Value];

                if (peek.Weight > op.Weight || (peek.Weight == op.Weight && op.Assoc == Assoc.Left) )
                {
                    return true;
                }

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
                return !_prev.IsNull() && !_prev.IsComma() && ( _prev.IsRightBracket() || _prev.IsVariable() ) && (_token.IsNumber() || _token.IsVariable());
            }

            private bool Chain()
            {
                return LeftImplicit() && RightImplicit();
            }

            private Token OperatorPop()
            {
                //TODO: We can use this to generate an AST directly. 
                Token temp = _operator.Pop();
                if (temp.IsFunction())
                {
                    temp.Arguments = _arity.Pop();
                }
                return temp;
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

            private static Token GenerateMultiply() => new Token("*", 2, Type.Operator);

            private static Token GenerateDivision() => new Token("/", 2, Type.Operator);

            private static Token GenerateNull() => new Token { Type = Type.Null };

            /// <summary>
            /// Moves all remaining data from the stack onto the queue
            /// </summary>
            private void Dump()
            {
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
            }

            void Write(string message)
            {
                Logger?.Invoke(this, message.Alias());
            }
        }
    }
}