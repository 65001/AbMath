﻿using System;
using System.Diagnostics;
using AbMath.Calculator;

namespace Test_Console
{
    class Program
    {
        private static RPN RPN;
        private static bool debug;
        static void Main(string[] args)
        {
            Console.Title = "AbMath v2.3.2";
            Console.WindowWidth = Console.BufferWidth;
            Console.WriteLine("(C) 2022. Abhishek Sathiabalan");

            Console.WriteLine("Recent Changes:");
            Console.WriteLine("Unary negative is now implemented.");
            Console.WriteLine("Composite Function bug should now be fixed.");
            Console.WriteLine("Implicit multiplication.");
            Console.WriteLine("Variadic Function Support");            
            Console.WriteLine();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                string equation = string.Empty;
                while (string.IsNullOrWhiteSpace(equation))
                {
                    Console.Write("Equation>");
                    equation = Console.ReadLine();

                    if (equation.Length == 0) { Console.Clear(); }
                }
                if (equation == "~debug")
                {
                    debug = !debug;
                    Console.WriteLine($"Debug Mode: {debug}");
                    continue;
                }

                RPN = new RPN(equation);
                if (debug)
                {
                    RPN.Data.DebugMode = debug;
                }

                if (RPN.Data.MarkdownTables)
                {
                    Console.Clear();
                    Console.WriteLine($"Equation>``{equation}``");
                }
                
                RPN.Compute();

                PostFix postFix = new PostFix(RPN);
                postFix.Logger += Write;   

                if (RPN.ContainsVariables)
                {
                    Console.WriteLine("Set the variables");
                    for (int i = 0; i < RPN.Data.Variables.Count; i++)
                    {
                        Console.Write(RPN.Data.Variables[i] + "=");
                        postFix.SetVariable(RPN.Data.Variables[i], Console.ReadLine());
                    }
                }

                Console.ForegroundColor = ConsoleColor.White;
                double answer = postFix.Compute();

                if (RPN.Data.MarkdownTables)
                {
                    Console.Write($"Answer: ``{answer}``");
                }
                else
                {
                    Console.Write($"Answer: {answer}");
                }

                Console.WriteLine();
                Console.WriteLine(RPN.Data.TimeRecords().ToString());

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("Press any key to continue...");
                Console.ReadKey(true);
                Console.Clear();
            }

            void Write(object sender, string Event)
            {
                Console.WriteLine(Event);
            }
        }
    }
}
