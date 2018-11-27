using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using CLI;

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

            Term Prev;
            Term Token;
            Term Ahead;

            //TODO: Implement Variadic Function
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

                Tables<string> tables = new Tables<string>(new Config {Title = "Shunting Yard Algorithm" });
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
                    Token = Tokens[i];
                    Ahead = ((i + 1) < Tokens.Count)? Tokens[i + 1] : GenerateNull();
                    Prev = (i > 0) ? Tokens[i - 1]  : GenerateNull();

                    string Action = string.Empty;
                    string Stack = string.Empty;
                    string Type = string.Empty;


                    if (Chain())
                    {
                        Type = "Chain Multiplication";
                        //Right
                        Implicit();
                        //Left
                        OperatorRule(GenerateMultiply());
                    }
                    else if (LeftImplicit())
                    {
                        //This will flip the order of the multiplication :(
                        Type = "Implicit Left";
                        Implicit();
                    }
                    else if (!Prev.IsNull()  && (Prev.IsRightBracket() && Token.IsLeftBracket()) || (Prev.IsVariable() && Token.IsNumber())) 
                    {
                        Type = "Implicit Left 2";
                        OperatorRule(GenerateMultiply());
                        Operator.Push(Token);
                    }
                    else if (RightImplicit())
                    {
                        Type = "Implicit Right";
                        Implicit();
                    }
                    else if (Prev.IsRightBracket() && Prev.Value != "," && Token.IsFunction())
                    {
                        Type = "Implicit Left Functional";
                        OperatorRule(GenerateMultiply());
                        WriteFunction(Token);
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

                Write("");
                Tables<string> arityTables = new Tables<string>(new Config { Title = "Arity" });
                arityTables.Add(new Schema { Column = "#", Width = 3 });
                arityTables.Add(new Schema { Column = "Token", Width = 10 });
                arityTables.Add(new Schema { Column = "Arity", Width = 5 });
                Write(arityTables.GenerateHeaders());

                for (int i = 0; i < Output.Count; i++)
                {
                    Term arityTerm = Output.Dequeue();
                    Output.Enqueue(arityTerm);

                    string[] message =  {i.ToString(), arityTerm.Value, arityTerm.Arguments.ToString() };
                    arityTables.Add(message);
                    Write( arityTables.GenerateNextRow() );
                }

                Write(arityTables.GenerateFooter());
                Write($"Arity Count : {Arity.Count}");
                Write($"Arity Peek {Arity.SafePeek()}");
                Write("");

                if (Arity.Count > 0)
                {
                    throw new InvalidOperationException("Arity not completely assigned");
                }

                SW.Stop();
                Write($"Execution Time {SW.ElapsedMilliseconds}(ms). Elapsed Ticks: {SW.ElapsedTicks}");
                Write($"Reverse Polish Notation:\n{Output.Print()}");
                Write("");

                return Output;
            }

            void Implicit()
            {
                OperatorRule(GenerateMultiply());
                Output.Enqueue(Token);
                if (Token.IsVariable())
                {
                    Data.AddVariable(Token.Value);
                }
            }

            void RightBracketRule(Term Token)
            {
                while (!Operator.Peek().IsLeftBracket())
                {
                    if (Operator.Count == 0)
                    {
                        throw new ArgumentException("Error : Mismatched Brackets or Parentheses.");
                    }

                    Term output = Operator.Pop();
                    //This ensures that only functions 
                    //can have variable number of arguments
                    if (output.IsFunction() )
                    {
                        int args = Arity.Pop();
                        //TODO Bounds Checking
                        output.Arguments = args;
                    }
                    Output.Enqueue(output);
                }

                //For functions and composite functions the to work, we must return now.
                if (Token.Value == ",")
                {
                    Arity.Push(Arity.Pop() + 1);
                    return;
                }

                //Pops the left bracket or Parentheses from the stack. 
                Operator.Pop();
            }

            //Sort Stack equivalent in sb
            void OperatorRule(Term Token)
            {
                bool Go = true;
                while (DoOperatorRule(Token) && Go)
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
                                (Data.Operators[Operator.Peek().Value].Weight > Data.Operators[Token.Value].Weight) ||
                                (Data.Operators[Operator.Peek().Value].Weight == Data.Operators[Token.Value].Weight && Data.Operators[Token.Value].Assoc == Assoc.Left)

                            )
                            && Data.IsLeftBracket(Operator.Peek().Value) == false;
                }
                catch (Exception ex) { }
                return false;
            }

            bool LeftImplicit()
            {
                return !Ahead.IsNull() && (Token.IsNumber() || Token.IsVariable()) && (Ahead.IsFunction() || Ahead.IsLeftBracket() || Ahead.IsVariable());
            }

            bool RightImplicit()
            {
                return !Prev.IsNull() && Prev.Value != "," && Prev.IsRightBracket() && (Token.IsNumber() || Token.IsVariable());
            }

            bool Chain()
            {
                return LeftImplicit()  && RightImplicit();
            }

            void WriteFunction(Term Function)
            {
                Operator.Push(Function);
                
                if (Data.Functions[Function.Value].Arguments > 0)
                {
                    Arity.Push(1);
                }
                else
                {
                    Arity.Push(0);
                }
            }

            Term GenerateMultiply()
            {
                return new Term { Value = "*", Arguments = 2, Type = Type.Operator };
            }

            Term GenerateNull()
            {
                return new Term { Type = Type.Null };
            }

            

            /// <summary>
            /// Moves all remaining data from the stack onto the queue
            /// </summary>
            void Dump()
            {
                while (Operator.Count > 0)
                {
                    Term peek = Operator.Peek();

                    if (peek.Type == Type.LParen || peek.Type == Type.RParen)
                    {
                        throw new ArgumentException("Error: Mismatched Parentheses or Brackets");
                    }
                    var output = Operator.Pop();
                    Output.Enqueue(output);
                }

                while ( Arity.Count > 0)
                {
                    for (int i = 0; i < (Output.Count - 1); i++)
                    {
                        Output.Enqueue( Output.Dequeue() );
                    }

                    var foo = Output.Dequeue();

                    if (foo.IsFunction())
                    {
                        foo.Arguments = Arity.Pop();
                    }
                    else
                    {
                        Arity.Pop();
                    }

                    Output.Enqueue(foo);
                }
                
            }

            void Write(string message)
            {
                Logger?.Invoke(this, message);
            }
        }
    }
}