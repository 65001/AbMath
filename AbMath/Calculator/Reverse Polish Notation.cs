using System;
using System.Collections.Generic;

namespace AbMath.Calculator
{
    //TODO
    //Generify
    //Auto Scaling from decimal to double to Big Integer
    //Move from using Doubles to Complex
    //Complex Number Support sqrt(-1) = i
    //Remove debug data for release builds

    /// <summary>
    /// Reverse Polish Notation
    /// Used for math equations
    /// </summary>
    public partial class RPN
    {
        public enum Type {LParen,RParen,Number,Variable,Function,Operator,Store, Null,Arity };
        public delegate double Run(params double[] arguments);
        public delegate void Store(ref DataStore dataStore,params string[] arguments);

        public event EventHandler<string> Logger;

        //TODO Add Inequalities : >, ==, <
        public struct Operator
        {
            public int Weight;
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

        public struct Token
        {
            public string Value;
            public int Arguments;
            public Type Type;
            public override string ToString()
            {
                return Value;
            }
        }

        public struct TimeRecord
        {
            public string Type;
            public double ElapsedMilliseconds;
            public double ElapsedTicks;
        }

        public string Equation { get; private set; }

        public Token[] Polish => Data.Polish;
        public List<Token> Tokens;

        public bool ContainsVariables  => Data.ContainsVariables; 

        private ITokenizer<Token> _tokenizer;
        private IShunt<Token> _shunt;
        public DataStore Data { get; private set; }


        #region Constructors
        public RPN(string equation)
        {
            Equation = equation;
            Startup();
            _tokenizer = new Tokenizer(Data);
            _shunt = new Shunt(Data);
        }

        public RPN(string equation, ITokenizer<Token> customTokenizer)
        {
            Equation = equation;
            Startup();
            _tokenizer = customTokenizer;
            _shunt = new Shunt(Data);
        }

        public RPN(string equation, IShunt<Token> customShunter)
        {
            Equation = equation;
            Startup();
            _tokenizer = new Tokenizer(Data);
            _shunt = customShunter;
        }

        public RPN(string equation, ITokenizer<Token> customTokenizer, IShunt<Token> customShunter)
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
            Data.Polish = _shunt.ShuntYard( this.Tokens  );
        }
    }
}
