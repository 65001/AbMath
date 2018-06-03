using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using AbMath.CLITables;

namespace AbMath.Utilities
{
    public partial class RPN
    {
        ///<summary>
        /// Takes a list of tokens and returns a Queue of Tokens after Order of Operations has been 
        /// taken into consideration.
        ///</summary>

        //Should be able to correctly shunt functions?
        public class Shunt : IShunt<Term>
        {
            private Data Data;
            Queue<Term> Output;
            Stack<Term> Operator;

            //TODO: Implement Variadic Functions
            //See http://wcipeg.com/wiki/Shunting_yard_algorithm#Variadic_functions
            Stack<int> Arity;

            public event EventHandler<string> Logger;

            public Shunt(Data data)
            {
                Data = data;
            }

            //Todo make ShuntYard smart about uniary negative signs 
            public Queue<Term> ShuntYard(List<Term> Tokens)
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();

                Output = new Queue<Term>(Tokens.Count);
                Operator = new Stack<Term>(20);

                Arity = new Stack<int>();

                Tables tables = new Tables(new Config {Title = "Shunting Yard Algorithm" });
                tables.Add(new Schema { Column = "#", Width = 3 });
                tables.Add(new Schema { Column = "Token", Width = 10 });
                tables.Add(new Schema { Column = "Stack Count", Width = 15 });
                tables.Add(new Schema { Column = "Stack Peek", Width = 12 });
                tables.Add(new Schema { Column = "Arity", Width=5});
                tables.Add(new Schema { Column = "Type", Width = 15 });
                tables.Add(new Schema { Column = "RPN", Width = 20 });
                tables.Add(new Schema { Column = "Action", Width = 30 });

                Write(tables.GenerateHeaders());
                for (int i = 0; i < Tokens.Count; i++)
                {
                    Term Token = Tokens[i];
                    Term? Ahead = null;
                    Term? Prev = null;

                    if ((i + 1) < Tokens.Count)
                    {
                        Ahead = Tokens[i + 1];
                    }
                    if( (i > 0))
                    {
                        Prev = Tokens[i - 1];
                    }

                    string Action = string.Empty;
                    string Stack = string.Empty;
                    string Type = string.Empty;


                    if (Ahead != null && (Token.Type == RPN.Type.Number || Token.Type == RPN.Type.Variable) && ( ((Term)Ahead).IsFunction() || ((Term)Ahead).Type == RPN.Type.LParen || ((Term)Ahead).Type == RPN.Type.Variable ))
                    {
                        //This will flip the order of the multiplication :(
                        Type = "Implicit Left";
                        OperatorRule(Multiply());
                        Operator.Push(Token);
                        if (Token.IsVariable())
                        {
                            Data.AddVariable(Token.Value);
                        }
                    }
                    else if (Prev != null && ((Term)Prev).IsRightBracket() && Token.IsLeftBracket())
                    {
                        Type = "Implicit Left 2";
                        OperatorRule(Multiply());
                        Operator.Push(Token);
                    }
                    else if (Prev != null && ((Term)Prev).IsVariable() && Token.IsNumber())
                    {
                        Type = "Implicit Left 3";
                        OperatorRule(Multiply());
                        Operator.Push(Token);
                    }
                    else if ( (Prev != null && Prev.Value.ToString() != ",") && ((Term)Prev).IsRightBracket() && (Token.IsNumber() || Token.IsVariable()))
                    {
                        Type = "Implicit Right";
                        OperatorRule(Multiply());
                        Output.Enqueue(Token);
                        if (Token.IsVariable())
                        {
                            Data.AddVariable(Token.Value);
                        }
                    }
                    else
                    {
                        switch (Token.Type)
                        {
                            case RPN.Type.Number: 
                                Action = "Added token to output";
                                Type = "Number";
                                Output.Enqueue(Token);
                                break;
                            case RPN.Type.Function:
                                Action = "Added token to stack";
                                Type = "Function";
                                WriteFunction(Token);
                                if (Data.Functions[Token.Value].Arguments > 0)
                                {
                                    Arity.Push(1);
                                }
                                break;
                            case RPN.Type.Operator:
                                Type = "Operator";
                                Action = "Operator Rules";
                                OperatorRule(Token);
                                break;
                            case RPN.Type.LParen:
                                Type = "Left Bracket";
                                Action = "Added token to stack";
                                Operator.Push(Token);
                                break;
                            case RPN.Type.RParen:
                                Type = "Right Bracket";
                                Action = "Right Bracket Rules";
                                if (Token.Value == ",")
                                {
                                    Type = "Comma";
                                }
                                RightBracketRule(Token);
                                break;
                            case RPN.Type.Variable:
                                Action = "Added token to output";
                                Type = "Variable";
                                Output.Enqueue(Token);
                                Data.AddVariable(Token.Value);
                                break;
                            default:
                                throw new NotImplementedException(Token.Value);
                        }
                    }


                    var print = new string[] { i.ToString(), Token.Value, Operator.Count.ToString(), Operator.SafePeek().Value ?? string.Empty, Arity.SafePeek().ToString() , Type, Output.Print(), Action };
                    tables.Add(print);

                    Write(tables.GenerateNextRow());
                }
                Dump();
                Write(tables.GenerateFooter());

                if (tables.SuggestedRedraw)
                {
                    Write(tables.Redraw());
                }

                SW.Stop();
                Write($"Execution Time {SW.ElapsedMilliseconds}(ms). Elapsed Ticks: {SW.ElapsedTicks}");
                Write($"Reverse Polish Notation:\n{Output.Print()}");
                Write("");

                return Output;

                void RightBracketRule(Term Token)
                {
                    while (Operator.Peek().Type !=  RPN.Type.LParen)
                    {
                        if (Operator.Count == 0)
                        {
                            throw new ArgumentException("Error : Mismatched Brackets or Parentheses.");
                        }

                        Term output = Operator.Pop();
                        if (Arity.Count > 0)
                        {
                            //output.Arguments = 
                            Arity.Pop();
                        }
                        Write("Enqueuing " + output.ToString());
                        Output.Enqueue(output);
                    }
                    //For functions and composite functions the to work, we must return now.
                    if (Token.Value == ",")
                    {
                        if (Arity.Count == 0)
                        {
                            Arity.Push(1);
                        }
                        else
                        {
                            Arity.Push(Arity.Pop() + 1);
                        }
                        return;
                    }

                    //Pops the left bracket or Parentheses from the stack. 
                    Operator.Pop();
                }

                //Sort Stack equivalent in sb
                void OperatorRule(Term Token)
                {
                    //TODO: Revisit 
                     bool Go = true;
                     while (DoOperatorRule(Token) == true && Go == true)
                     {
                         Output.Enqueue(Operator.Pop());

                        if (Operator.Count == 0)
                        {
                            Go = false;
                        }
                     }
                    Operator.Push(Token);
                }

                bool DoOperatorRule(Term Token)
                {
                    try
                    {
                        return Operator.Count > 0 && 
                                (
                                    (Data.IsFunction(Operator.Peek().Value) == true) ||
                                    (Data.Operators[Operator.Peek().Value].weight > Data.Operators[Token.Value].weight) ||
                                    (Data.Operators[Operator.Peek().Value].weight == Data.Operators[Token.Value].weight && Data.Operators[Token.Value].Assoc == Assoc.Left)
                                    
                                )
                                && Data.IsLeftBracket(Operator.Peek().Value) == false;
                    }
                    catch (Exception ex) { }
                    return false;
                }

                void WriteFunction(Term Function)
                {
                    Operator.Push(Function);
                    Arity.Push(0);
                }

                Term Multiply()
                {
                    return new Term { Value = "*", Arguments = 2, Type = Type.Operator };
                }
            }

            /// <summary>
            /// Moves all remaining data from the stack onto the queue
            /// </summary>
            void Dump()
            {
                while (Operator.Count > 0)
                {
                    string Peek = Operator.Peek().Value;

                    if (Data.IsLeftBracket(Peek) == true || Data.IsRightBracket(Peek) == true)
                    {
                        throw new ArgumentException("Error: Mismatched Parentheses or Brackets");
                    }
                    var output = Operator.Pop();
                    if (Arity.Count > 0)
                    {
                        //output.Arguments = 
                        Arity.Pop();
                    }
                    Output.Enqueue(output);
                }
            }

            void Write(string Message)
            {
                Logger?.Invoke(this, Message);
            }
        }
    }
}