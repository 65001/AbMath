using System;
using System.Collections.Generic;

namespace AbMath.Calculator
{
    /// <summary>
    /// Reverse Polish Notation
    /// Used for math equations
    /// </summary>
    /// 

    //TODO
    //ABS
    //ARCSIN,ARCTAN,ARCCOS

    //Generify
    //Auto Scaling from decimal to double to Big Integer
    //Move from using Doubles to Complex
    //Complex Number Support sqrt(-1) = i

    public partial class RPN
    {
        public enum Type {LParen,RParen,Number,Variable,Function,Operator,Null };
        public delegate double Run(params double[] arguments);
        public delegate void Store(ref DataStore dataStore,params string[] arguments);

        public event EventHandler<string> Logger;

        public struct Operator
        {
            public double Weight;
            public Assoc Assoc;
            public int Arguments;
            public Run Compute;
        }

        public struct Function
        {
            public int Arguments;
            public int MaxArguments;
            public int MinArguments;

            public Run Compute;
        }

        public struct Term
        {
            public string Value;
            public int Arguments;
            public Type Type;
            public override string ToString()
            {
                return Value;
            }

        }

        public string Equation { get; private set; }

        public Queue<Term> Polish;
        public List<Term> Tokens;

        public bool ContainsVariables  => Data.ContainsVariables; 

        private ITokenizer<Term> _tokenizer;
        private IShunt<Term> _shunt;
        public DataStore Data { get; private set; }


        #region Constructors
        public RPN(string equation)
        {
            Equation = equation;
            Startup();
            _tokenizer = new Tokenizer(Data);
            _shunt = new Shunt(Data);
        }

        public RPN(string equation, ITokenizer<Term> customTokenizer)
        {
            Equation = equation;
            Startup();
            _tokenizer = customTokenizer;
            _shunt = new Shunt(Data);
        }

        public RPN(string equation, IShunt<Term> customShunter)
        {
            Equation = equation;
            Startup();
            _tokenizer = new Tokenizer(Data);
            _shunt = customShunter;
        }

        public RPN(string equation, ITokenizer<Term> customTokenizer, IShunt<Term> customShunter)
        {
            Equation = equation;
            Startup();
            _tokenizer = customTokenizer;
            _shunt = customShunter;
        }

        /**
         * Set's a new equation with the default Tokenizer
         */
        public void SetEquation(string equation)
        {
            Equation = equation;
            Data = new DataStore(Equation);
            _tokenizer = new Tokenizer(Data);
            _shunt = new Shunt(Data);
        }

        private void Startup()
        {
            Data = new DataStore(Equation);
        }
        #endregion

        public void Compute()
        {
            _tokenizer.Logger += Logger;
            Tokens = _tokenizer.Tokenize();

            _shunt.Logger += Logger;
            Polish = _shunt.ShuntYard(Tokens);
            Data.Polish = Polish;
        }
    }
}
