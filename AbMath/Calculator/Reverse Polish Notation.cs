using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using AbMath.Utilities;

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
        public enum Type {LParen,RParen,Number,Variable,Function,Operator,Null};
        public delegate double Run(params double[] arguments);
        public delegate void Store(ref DataStore dataStore,params string[] arguments);

        /// <summary>
        /// This event handler contains debug information that
        /// is created when DebugMode is on.
        /// </summary>
        public event EventHandler<string> Logger;

        /// <summary>
        /// This event handler contains output from the program
        /// in scenarios when certain meta-commands are invoked.
        /// You do not need to hook into this unless you are using a
        /// certain subset of meta-commands that create output.
        /// </summary>
        public event EventHandler<string> Output; 

        public struct TimeRecord
        {
            public string Type;
            public double ElapsedMilliseconds;
            public double ElapsedTicks;
            public int Count;
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
        public RPN SetEquation(string equation)
        {
            Equation = equation;

            Data.ClearTimeRecords();
            Data.SetEquation(equation);

            _tokenizer = new Tokenizer(Data);
            _shunt = new Shunt(Data);
            return this;
        }

        private void Startup()
        {
            Data = DataFactory.getInstance().generate(Equation);
        }
        #endregion

        public RPN Compute()
        {
            //We bind late because otherwise there is no way that someone can attach into 
            //either of the below channels! 
            Data.Logger.Bind(Channels.Debug, Logger);
            Data.Logger.Bind(Channels.Output, Output);

            Tokens = _tokenizer.Tokenize();
            Data.Polish = _shunt.ShuntYard( this.Tokens  );

            //Generate an Abstract Syntax Tree

            var logger = Data.Logger;

            AST ast = new AST(this);
            ast.Output += Output;
            ast.Logger += Logger;

            ast.Generate(this.Data.Polish);
            
            logger.Log(Channels.Debug, "AST RPN : " + ast.Root.ToPostFix().Print());

            //Simplify the Abstract Syntax Tree
            //This can take quite a lot of time
            ast.Simplify();
            ast.MetaFunctions();

            this.Data.Polish = ast.Root.ToPostFix().ToArray();
            this.Data.SimplifiedEquation = ast.Root.ToInfix(this.Data);

            logger.Log(Channels.Debug, "");
            logger.Log(Channels.Debug, "AST Simplified RPN : " + Data.Polish.Print());
            logger.Log(Channels.Debug, "AST Simplified Infix : " + Data.SimplifiedEquation);
            logger.Log(Channels.Debug, ast.Root.Print());

            //TODO: We should flush the Logger here
            return this;
        }
    }
}
