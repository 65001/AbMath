using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    /// <summary>
    /// Reverse Polish Notation
    /// Used for math equations
    /// </summary>
    /// 

    //TODO
    //ABS
    //ARCSIN,ARCTAN,ARCCOS
    //Random
    //Random(Min,Max)
    //Generify
    //Auto Scaling from decimal to double to Big Integer
    //Complex Number Support sqrt(-1) = i
    //Add Implement Variadic Functions

    //TODO
    //Add a data class so that other classes such as
    //the Tokenizer, Shunter, and the like don't need a copy of RPN.

    public partial class RPN 
    {
        public enum Assoc { Left, Right };
        public delegate double Run(params double[] arguments);

        public event EventHandler<string> Logger;

        public struct Operators
        {
            public double weight;
            public int Arguments;
            public Assoc Assoc;            
            public Run Compute;
        }

        public struct Functions 
        {
            public int Arguments;
            public Run Compute;
        }

        public string Equation;

        public Queue<string> Polish;
        public List<string> Tokens;

        public bool ContainsVariables = false;

        ITokenizer<string> tokenizer;
        IShunt<string> shunt;
        public Data data { get; private set; }


        #region Constructors
        public RPN(string equation)
        {
            Equation = equation;
            Startup();
            tokenizer = new Tokenizer(data);
            shunt = new Shunt(data);
        }

        public RPN(string equation, ITokenizer<string> CustomTokenizer)
        {
            Equation = equation;
            Startup();
            tokenizer = CustomTokenizer;
            shunt = new Shunt(data);
        }

        public RPN(string equation, IShunt<string> CustomShunter)
        {
            Equation = equation;
            Startup();
            tokenizer = new Tokenizer(data);
            shunt = CustomShunter;
        }

        public RPN(string equation, ITokenizer<string> CustomTokenizer, IShunt<string> CustomShunter)
        {
            Equation = equation;
            Startup();
            tokenizer = CustomTokenizer;
            shunt = CustomShunter;
        }

        private void Startup()
        {
            data = new Data(Equation);
        }
        #endregion

        public void Compute()
        {
            tokenizer.Logger += Logger;
            Tokens = tokenizer.Tokenize();

            shunt.Logger += Logger;
            Polish = shunt.ShuntYard(Tokens);
            data.Polish = Polish;
        }
    }
}
