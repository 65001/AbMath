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
        public class Shunt : IShunt<string>
        {
            private Data Data;
            Queue<string> Output;
            Stack<string> Operator;

            //TODO: Implement Variadic Functions
            //See http://wcipeg.com/wiki/Shunting_yard_algorithm#Variadic_functions
            Stack<int> Arity;

            public event EventHandler<string> Logger;

            public Shunt(Data data)
            {
                Data = data;
            }

            //Todo make ShuntYard smart about uniary negative signs 
            public Queue<string> ShuntYard(List<string> Tokens)
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();

                Output = new Queue<string>(Tokens.Count);
                Operator = new Stack<string>(20);

                Tables tables = new Tables(new Config {Title = "Shunting Yard Algorithm" });
                tables.Add(new Schema { Column = "#", Width = 3 });
                tables.Add(new Schema { Column = "Token", Width = 10 });
                tables.Add(new Schema { Column = "Stack Count", Width = 15 });
                tables.Add(new Schema { Column = "Stack Peek", Width = 12 });
                tables.Add(new Schema { Column = "Type", Width = 15 });
                tables.Add(new Schema { Column = "RPN", Width = 20 });
                tables.Add(new Schema { Column = "Action", Width = 30 });

                Write(tables.GenerateHeaders());
                for (int i = 0; i < Tokens.Count; i++)
                {
                    string Token = Tokens[i];

                    string Ahead = ((i+1) < Tokens.Count) ? Tokens[i+1] : string.Empty;
                    string Prev = (i > 0) ? Tokens[i - 1] : string.Empty;

                    string Action = string.Empty;
                    string Notation = string.Empty;
                    string Stack = string.Empty;
                    string Type = string.Empty;

                    if (string.IsNullOrEmpty(Ahead) == false && (Data.IsNumber(Token) || Data.IsVariable(Token)) && (Data.IsFunction(Ahead) || Data.IsLeftBracket(Ahead) || Data.IsVariable(Ahead) ))
                    {
                        //This will flip the order of the multiplication :(
                        Type = "Implicit Left";
                        OperatorRule("*");
                        Operator.Push(Token);
                        if (Data.IsVariable(Token))
                        {
                            Data.AddVariable(Token);
                        }
                    }
                    else if (string.IsNullOrEmpty(Prev) == false && Data.IsRightBracket(Prev)  &&  Data.IsLeftBracket(Token)  )
                    {
                        Type = "Implicit Left 2";
                        OperatorRule("*");
                        Operator.Push(Token);
                    }
                    else if (string.IsNullOrEmpty(Prev) == false && Data.IsVariable(Prev) && Data.IsNumber(Token))
                    {
                        Type = "Implicit Left 3";
                        OperatorRule("*");
                        Operator.Push(Token);
                    }
                    else if (Prev != "," && string.IsNullOrEmpty(Prev) == false && Data.IsRightBracket(Prev) && (Data.IsNumber(Token) || Data.IsVariable(Token)))
                    {
                        Type = "Implicit Right";
                        OperatorRule("*");
                        Output.Enqueue(Token);
                        if (Data.IsVariable(Token))
                        {
                            Data.AddVariable(Token);
                        }
                    }
                    else if (Data.IsNumber(Token))
                    {
                        Action = "Added token to output";
                        Type = "Number";
                        Output.Enqueue(Token);
                    }
                    else if (Data.IsFunction(Token))
                    {
                        Action = "Added token to stack";
                        Type = "Function";
                        Operator.Push(Token);
                    }
                    else if (Data.IsOperator(Token))
                    {
                        Type = "Operator";
                        Action = "Operator Rules";
                        OperatorRule(Token);
                    }
                    else if (Data.IsLeftBracket(Token))
                    {
                        Type = "Left Bracket";
                        Action = "Added token to stack";
                        Operator.Push(Token);
                    }
                    else if (Data.IsRightBracket(Token))
                    {
                        Type = "Right Bracket";
                        Action = "Right Bracket Rules";
                        if (Token == ",")
                        {
                            Type = "Comma";
                        }
                        RightBracketRule(Token);
                    }
                    else if (Data.IsVariable(Token))
                    {
                        Action = "Added token to output";
                        Type = "Variable";
                        Output.Enqueue(Token);
                        Data.AddVariable(Token);
                    }
                    else
                    {
                        throw new NotImplementedException(Token);
                    }

                    var print = new string[] { i.ToString(), Token, Operator.Count.ToString(), Operator.SafePeek() ?? string.Empty, Type, Output.Print(), Action };
                    tables.Add(print);

                    Notation = Output.Print();
                    Stack = Operator.SafePeek();

                    Write(tables.GenerateNextRow());
                }
                Dump();
                Write(tables.GenerateFooter());

                if (tables.SuggestedRedraw)
                {
                    tables.Clear();
                    Write(tables.ToString());
                }

                SW.Stop();
                Write($"Execution Time {SW.ElapsedMilliseconds}(ms). Elapsed Ticks: {SW.ElapsedTicks}");
                Write($"Reverse Polish Notation:\n{Output.Print()}");
                Write("");

                return Output;

                void RightBracketRule(string Token)
                {
                    string Peek = Operator.Peek();
                    while (Data.IsLeftBracket(Operator.Peek()) == false)
                    {
                        if (Operator.Count == 0)
                        {
                            throw new ArgumentException("Error : Mismatched Brackets or Parentheses.");
                        }
                        Peek = Operator.Pop();
                        Output.Enqueue(Peek);
                    }
                    //For functions and composite functions the to work, we must return now.
                    if (Token == ",")
                    {
                        return;
                    }
                    //Pops the left bracket or Parentheses from the stack. 
                    Operator.Pop();
                }

                //Sort Stack equivalent in sb
                void OperatorRule(string Token)
                {
                    //TODO: Revisit 
                     bool Go = true;
                     while (DoOperatorRule(Token) == true && Go == true)
                     {
                         string value = Operator.Pop();
                         Output.Enqueue(value);

                        if (Operator.Count == 0)
                        {
                            Go = false;
                        }
                     }
                    Operator.Push(Token);
                }

                bool DoOperatorRule(string Token)
                {
                    try
                    {
                        return Operator.Count > 0 && 
                                (
                                    (Data.IsFunction(Operator.Peek()) == true) ||
                                    (Data.Operators[Operator.Peek()].weight > Data.Operators[Token].weight) ||
                                    (Data.Operators[Operator.Peek()].weight == Data.Operators[Token].weight && Data.Operators[Token].Assoc == Assoc.Left)
                                    
                                )
                                && Data.IsLeftBracket(Operator.Peek()) == false;
                    }
                    catch (Exception ex) { }
                    return false;
                }
            }

            /// <summary>
            /// Moves all remaining data from the stack onto the queue
            /// </summary>
            void Dump()
            {
                while (Operator.Count > 0)
                {
                    string Peek = Operator.Peek();

                    if (Data.IsLeftBracket(Peek) == true || Data.IsRightBracket(Peek) == true)
                    {
                        throw new ArgumentException("Error: Mismatched Parentheses or Brackets");
                    }
                    Output.Enqueue(Operator.Pop());
                }
            }

            void Write(string Message)
            {
                Logger?.Invoke(this, Message);
            }
        }
    }
}