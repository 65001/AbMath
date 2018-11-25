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
        public enum Type {LParen,RParen,Number,Variable,Function,Operator,Null };
        public delegate double Run(params double[] arguments);
        public delegate void Store(ref Data data,params string[] arguments);

        public event EventHandler<string> Logger;

        public struct Operator
        {
            public double Weight;
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
            public Stack<int> Arity;
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

        public string Equation { get; private set; }

        public Queue<Term> Polish;
        public List<Term> Tokens;

        public bool ContainsVariables  => data.ContainsVariables; 

        ITokenizer<Term> _tokenizer;
        IShunt<Term> _shunt;
        public Data data { get; private set; }


        #region Constructors
        public RPN(string equation)
        {
            Equation = equation;
            Startup();
            _tokenizer = new Tokenizer(data);
            _shunt = new Shunt(data);
        }

        public RPN(string equation, ITokenizer<Term> CustomTokenizer)
        {
            Equation = equation;
            Startup();
            _tokenizer = CustomTokenizer;
            _shunt = new Shunt(data);
        }

        public RPN(string equation, IShunt<Term> CustomShunter)
        {
            Equation = equation;
            Startup();
            _tokenizer = new Tokenizer(data);
            _shunt = CustomShunter;
        }

        public RPN(string equation, ITokenizer<Term> CustomTokenizer, IShunt<Term> CustomShunter)
        {
            Equation = equation;
            Startup();
            _tokenizer = CustomTokenizer;
            _shunt = CustomShunter;
        }

        /**
         * Set's a new equation with the default Tokenizer
         */
        public void SetEquation(string equation)
        {
            Equation = equation;
            data = new Data(Equation);
            _tokenizer = new Tokenizer(data);
            _shunt = new Shunt(data);
        }

        private void Startup()
        {
            data = new Data(Equation);
        }
        #endregion

        public void Compute()
        {
            _tokenizer.Logger += Logger;
            Tokens = _tokenizer.Tokenize();

            _shunt.Logger += Logger;
            Polish = _shunt.ShuntYard(Tokens);
            data.Polish = Polish;
        }
    }
}
