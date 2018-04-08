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

        Dictionary<string, Operators> Ops = new Dictionary<string, Operators>();
        Dictionary<string, Functions> functions = new Dictionary<string, Functions>();
        Dictionary<string, string> aliases = new Dictionary<string, string>();

        List<string> LeftBracket = new List<string>();
        List<string> RightBracket = new List<string>();

        public List<string> Variables = new List<string>();

        public Queue<string> Polish;
        public List<string> Tokens;

        public bool ContainsVariables = false;

        ITokenizer<string> tokenizer;
        IShunt<string> shunt;

        #region Constructors
        public RPN(string equation)
        {
            Equation = equation;
            Startup();
            tokenizer = new Tokenizer(this);
            shunt = new Shunt(this);
        }

        public RPN(string equation, ITokenizer<string> CustomTokenizer)
        {
            Equation = equation;
            Startup();
            tokenizer = CustomTokenizer;
            shunt = new Shunt(this);
        }

        public RPN(string equation, IShunt<string> CustomShunter)
        {
            Equation = equation;
            Startup();
            tokenizer = new Tokenizer(this);
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
            DefaultOperators();
            DefaultFunctions();

            LeftBracket.Add("(");
            LeftBracket.Add("{");
            LeftBracket.Add("[");

            RightBracket.Add(")");
            RightBracket.Add("}");
            RightBracket.Add("]");
            RightBracket.Add(",");

            aliases.Add("÷", "/");
            aliases.Add("π", "pi");
            aliases.Add("≠", "!=");
            aliases.Add("≥",">=");
            aliases.Add("≤", "<=");
        }
        #endregion

        public void AddOperator(string Operator,Operators operators)
        {
            if (Ops.ContainsKey(Operator) == true)
            {
                Ops[Operator] = operators;
                return;
            }
            Ops.Add(Operator, operators);
        }

        public void AddFunction(string Function,Functions Args)
        {
            if (functions.ContainsKey(Function))
            {
                functions[Function] = Args;
            }
            functions.Add(Function, Args);
        }

        public void AddAlias(string Key, string Value)
        {
            if (aliases.ContainsKey(Key))
            {
                aliases[Key] = Value;
            }
            aliases.Add(Key, Value);
        }

        public Operators GetOperators(string Token)
        {
            return Ops[Token];
        }

        public Functions GetFunction(string Token)
        {
            return functions[Token];
        }

        public void Compute()
        {
            Tokens = tokenizer.Tokenize();
            Polish = shunt.ShuntYard(Tokens);
            Variables =  Variables.Distinct().ToList();
        }

        public IReadOnlyDictionary<string,Functions> ReadOnlyFunctions
        {
            get { return functions; }
        }

        public IReadOnlyDictionary<string, Operators> ReadOnlyOperators
        {
            get { return Ops; }
        }

        public IReadOnlyDictionary<string, string> Aliases
        {
             get { return aliases; } 
        }

        #region Fake Extension Methods
        public bool IsNumber(string value)
        {
            return double.TryParse(value,out double data);
        }

        public bool IsOperator(string value)
        {
            if (Ops.ContainsKey(value))
            {
                return true;
            }
            return false;
        }

        public bool IsUniary(string value)
        {
            if (IsOperator(value) == false)
            {
                return false;
            }
            return value == "-" || value == "−" || value == "+";
        }

        public bool IsFunction(string value)
        {
            if (functions.ContainsKey(value))
            {
                return true;
            }
            return false;
        }

        public bool IsLeftBracket(string value)
        {
            if (LeftBracket.Contains(value))
            {
                return true;
            }
            return false;
        }

        public bool IsRightBracket(string value)
        {
            if (RightBracket.Contains(value))
            {
                return true;
            }
            return false;
        }

        public bool IsVariable(string value)
        {
            //If a value is a number, operator, or function it CANNOT be a variable
            //All other values ought to be variables?
            if (IsNumber(value) || IsOperator(value) || IsFunction(value) || IsLeftBracket(value) || IsRightBracket(value))
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}