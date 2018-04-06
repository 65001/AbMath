using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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
            private RPN RPN;
            Queue<string> Output;
            Stack<string> Operator;

            public Shunt(RPN rpn)
            {
                RPN = rpn;
            }

            //Todo make ShuntYard smart about uniary negative signs 
            public Queue<string> ShuntYard(List<string> Tokens)
            {
                Stopwatch SW = new Stopwatch();
                SW.Start();

                Output = new Queue<string>(Tokens.Count);
                Operator = new Stack<string>(20);

                Logger( $"┌{"".PadRight(117, '─')}┐");
                Logger( $"│{"Shunting Yard Algorithm",48}{"",69}│");
                Logger( $"├{"".PadRight(4, '─') }┬{"".PadRight(12, '─')}┬{"".PadRight(17, '─')}┬{"".PadRight(14, '─')}┬{"".PadRight(16, '─')}┬{"".PadRight(22, '─')}┬{"".PadRight(26, '─')}┤");
                Logger( $"│{"#",-3} │ {"Token",-10} │ {"Stack Count",-15} │ {"Stack Peek",-12} │ {"Type",-15}│ {"RPN",-20} │ {"Action",-24} │");
                for (int i = 0; i < Tokens.Count; i++)
                {
                    string Token = Tokens[i];
                    string Action = string.Empty;
                    string Notation = string.Empty;
                    string Stack = string.Empty;
                    string Type = string.Empty;

                    if (RPN.IsNumber(Token))
                    {
                        Action = "Added token to output";
                        Type = "Number";
                        Output.Enqueue(Token);
                    }
                    else if (RPN.IsFunction(Token))
                    {
                        Action = "Added token to stack";
                        Type = "Function";
                        Operator.Push(Token);
                    }
                    else if (RPN.IsOperator(Token))
                    {
                        Type = "Operator";
                        Action = "Operator Rules";
                        OperatorRule(Token);
                    }
                    else if (RPN.IsLeftBracket(Token))
                    {
                        Type = "Left Bracket";
                        Action = "Added token to stack";
                        Operator.Push(Token);
                    }
                    else if (RPN.IsRightBracket(Token))
                    {
                        Type = "Right Bracket";
                        Action = "Right Bracket Rules";
                        if (Token == ",")
                        {
                            Type = "Comma";
                        }
                        RightBracketRule(Token);
                    }
                    else if (RPN.IsVariable(Token))
                    {
                        Action = "Added token to output";
                        Type = "Variable";
                        RPN.ContainsVariables = true;
                        Output.Enqueue(Token);
                        RPN.Variables.Add(Token);
                    }
                    else
                    {
                        throw new NotImplementedException(Token);
                    }

                    Notation = Output.Print();
                    Stack = Operator.SafePeek();

                    string Log = $"│{i,-3} │ {Token,-10} │ {Operator.Count,-15} │ {Stack,-12} │ {Type,-14} │ {Notation,-20} │ {Action,-24} │";
                    RPN.Logger?.Invoke(this, Log);
                }
                Dump();
                Logger($"└{"".PadRight(4, '─') }┴{"".PadRight(12, '─')}┴{"".PadRight(17, '─')}┴{"".PadRight(14, '─')}┴{"".PadRight(16, '─')}┴{"".PadRight(22, '─')}┴{"".PadRight(26, '─')}┘");
                SW.Stop();
                Logger($"Execution Time {SW.ElapsedMilliseconds}(ms). Elapsed Ticks: {SW.ElapsedTicks}");
                Logger("");

                return Output;

                void RightBracketRule(string Token)
                {
                    string Peek = Operator.Peek();
                    while (RPN.IsLeftBracket(Operator.Peek()) == false)
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
                                    (RPN.IsFunction(Operator.Peek()) == true) ||
                                    (RPN.Ops[Operator.Peek()].weight > RPN.Ops[Token].weight) ||
                                    (RPN.Ops[Operator.Peek()].weight == RPN.Ops[Token].weight && RPN.Ops[Token].Assoc == Assoc.Left)
                                    
                                )
                                && RPN.IsLeftBracket(Operator.Peek()) == false;
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

                    if (RPN.IsLeftBracket(Peek) == true || RPN.IsRightBracket(Peek) == true)
                    {
                        throw new ArgumentException("Error: Mismatched Parentheses or Brackets");
                    }
                    Output.Enqueue(Operator.Pop());
                }
            }

            void Logger(string Message)
            {
                RPN.Logger?.Invoke(this, Message);
            }
        }
    }
}