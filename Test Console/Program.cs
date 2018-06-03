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
            Console.Title = "Math Solver 1.0.3";
            Console.WindowWidth = Console.BufferWidth;
            Console.WriteLine("(C) 2018. Abhishek Sathiabalan");

            Console.WriteLine("Recent Changes:");
            Console.WriteLine("Uniary negative is now implemented.");
            Console.WriteLine("Composite Function bug should now be fixed.");
            Console.WriteLine("Implict multiplication.");

            Console.WriteLine("");
            Console.WriteLine("Known Bugs:");
            Console.WriteLine("Space between terms is necessary.");
            
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

                if (RPN.ContainsVariables)
                {
                    Console.WriteLine("Set the variables");
                    for (int i = 0; i < RPN.data.Variables.Count; i++)
                    {
                        Console.Write(RPN.data.Variables[i] + "=");
                        postFix.SetVariable(RPN.data.Variables[i], Console.ReadLine());
                    }
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Answer:");
                Console.WriteLine(postFix.Compute());
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
