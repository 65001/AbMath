using System;
using System.Linq;
using AbMath.Utilities;

namespace Test_Console
{
    class Program
    {
        private static RPN RPN;
        static void Main(string[] args)
        {
            Console.Title = "Math Solver 1.0.4";
            Console.WindowWidth = Console.BufferWidth;
            Console.WriteLine("(C) 2018. Abhishek Sathiabalan");

            Console.WriteLine("Recent Changes:");
            Console.WriteLine("Unary negative is now implemented.");
            Console.WriteLine("Composite Function bug should now be fixed.");
            Console.WriteLine("Implicit multiplication.");
            Console.WriteLine("Variadic Function Support");            
            Console.WriteLine();

            while (1 == 1)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                string Equation = string.Empty;
                while (string.IsNullOrWhiteSpace(Equation))
                {
                    Console.Write("Equation>");
                    Equation = Console.ReadLine();

                    if (Equation.Length == 0) { Console.Clear(); }
                }

                RPN = new RPN(Equation);
                RPN.Logger += Write;
                
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
                double Answer = postFix.Compute();

                Console.Write($"Answer: {Answer}");
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
