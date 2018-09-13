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
    //Random()
    //Random(Min,Max)
    //Generify
    //Auto Scaling from decimal to double to Big Integer
    //Complex Number Support sqrt(-1) = i
    //Add Implement Variadic Function

    //TODO
    //Add a data class so that other classes such as
    //the Tokenizer, Shunter, and the like don't need a copy of RPN.

    public partial class RPN
    {
        public enum Assoc { Left, Right };

        public enum Type {LParen,RParen,Number,Variable,Function,Operator,Null };
        public delegate double Run(params double[] arguments);
        public delegate void Store(ref Data data,params string[] arguments);

        public event EventHandler<string> Logger;


        public struct Operator
        {
            public double weight;
            public Assoc Assoc;
            public int Arguments;
            public Run Compute;
            public Store Store;
        }

        public struct Function
        {
            public int Arguments;
            public Run Compute;
            public Store Store;
        }

        public struct Term
        {
            public string Value { get; set; }

            public int Arguments;
            public Type Type { get; set; }
            public override string ToString()
            {
                return Value;
            }

        }

        public string Equation;

        public Queue<Term> Polish;
        public List<Term> Tokens;

        public bool ContainsVariables  => data.ContainsVariables; 

        ITokenizer<Term> tokenizer;
        IShunt<Term> shunt;
        public Data data { get; private set; }


        #region Constructors
        public RPN(string equation)
        {
            Equation = equation;
            Startup();
            tokenizer = new Tokenizer(data);
            shunt = new Shunt(data);
        }

        public RPN(string equation, ITokenizer<Term> CustomTokenizer)
        {
            Equation = equation;
            Startup();
            tokenizer = CustomTokenizer;
            shunt = new Shunt(data);
        }

        public RPN(string equation, IShunt<Term> CustomShunter)
        {
            Equation = equation;
            Startup();
            tokenizer = new Tokenizer(data);
            shunt = CustomShunter;
        }

        public RPN(string equation, ITokenizer<Term> CustomTokenizer, IShunt<Term> CustomShunter)
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
