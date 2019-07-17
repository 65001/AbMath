﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AbMath.Calculator
{
    public class AST
    {
        public RPN.Node Root { get; private set; }

        private enum SimplificationMode
        {
            Sqrt, Log, Imaginary, Division, Exponent, Subtraction, Addition, Multiplication, Swap, Trig, Compress, COUNT
        }

        private RPN _rpn;
        private RPN.DataStore _data;
        private int count = -1;

        private bool debug => _rpn.Data.DebugMode;

        private readonly RPN.Token _derive = new RPN.Token("derive", 1, RPN.Type.Function);

        public event EventHandler<string> Logger;
        public event EventHandler<string> Output;

        public AST(RPN rpn)
        {
            _rpn = rpn;
            _data = rpn.Data;
            
        }

        public RPN.Node Generate(RPN.Token[] input)
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();

            Stack<RPN.Node> stack = new Stack<RPN.Node>(5);

            RPN.Node node;
            for (int i = 0; i < input.Length; i++)
            {
                node = new RPN.Node(GenerateNextID(), input[i]);
                if (node.IsOperator() || node.IsFunction())
                {
                    //Due to the nature of PostFix we know that all children
                    //of a function or operator have already been processed before this point
                    //this ensures we do not have any overflows or exceptions.
                    node.Children = new RPN.Node[node.Token.Arguments];

                    for (int j = 0; j < node.Token.Arguments; j++)
                    {
                        RPN.Node temp = stack.Pop();
                        temp.Parent = node;
                        node.Children[j] = temp;
                    }
                }
                stack.Push(node); //Push new tree into the stack 
            }

            //This prevents the reassignment of the root node
            if (Root is null)
            {
                Root = stack.Peek();
            }

            SW.Stop();
            _rpn.Data.AddTimeRecord("AST Generate", SW);
            return stack.Pop();
        }

        /// <summary>
        /// Simplifies the current tree.
        /// </summary>
        /// <returns></returns>
        public AST Simplify()
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();

            int pass = 0;
            string hash = string.Empty;

            while (hash != Root.GetHash())
            {
                hash = Root.GetHash();
                Write($"{pass}. {Root.ToInfix()}.");
                Simplify(Root);
                pass++;
            }

            SW.Stop();
            _data.AddTimeRecord("AST Simplify", SW);
            return this;
        }

        private void Simplify(RPN.Node node)
        {
            Simplify(node, SimplificationMode.Sqrt);
            Simplify(node, SimplificationMode.Log);
            Simplify(node, SimplificationMode.Imaginary);
            Simplify(node, SimplificationMode.Division);

            Simplify(node, SimplificationMode.Exponent);
            Simplify(node, SimplificationMode.Subtraction);
            Simplify(node, SimplificationMode.Addition);
            Simplify(node, SimplificationMode.Trig);
            Simplify(node, SimplificationMode.Multiplication);
            Simplify(node, SimplificationMode.Swap);
            Swap(node);
        }

        private void Simplify(RPN.Node node, SimplificationMode mode)
        {
            //If Root is a number abort. 
            if (Root.IsNumber() || node.IsNumber() || node.IsConstant() )
            {
                return;
            }

            if (mode == SimplificationMode.Sqrt)
            {
                if (node.IsExponent() && node.Children[0].IsNumber(2) && node.Children[1].IsSqrt())
                {
                    Write("\tsqrt(g(x))^2 -> g(x)");
                    Assign(node, node.Children[1].Children[0]);
                }
                else if (node.IsSqrt() && node.Children[0].IsExponent() && node.Children[0].Children[0].IsNumber(2))
                {
                    Write("\tsqrt(g(x)^2) -> abs(g(x))");
                    RPN.Node abs = new RPN.Node(GenerateNextID(), new[] { node.Children[0].Children[1] }, new RPN.Token("abs", 1, RPN.Type.Function));
                    Assign(node, abs);
                }
                else if (node.IsSqrt() && node.Children[0].IsExponent() && node.Children[0].Children[0].IsNumber() && node.Children[0].Children[0].GetNumber() % 4 == 0)
                {
                    Write("\tsqrt(g(x)^n) where n is a multiple of 4. -> g(x)^n/2");
                    RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), node.Children[0].Children[0].GetNumber() / 2), Clone(node.Children[0].Children[1]) }, new RPN.Token("^", 2, RPN.Type.Operator));
                    Assign(node, exponent);
                }
            }
            else if (mode == SimplificationMode.Log)
            {
                RPN.Node temp = null;
                if (node.Token.IsLog() && node.Children[0].IsNumber(1))
                {
                    Write("\tlog(b,1) -> 0");
                    temp = new RPN.Node(GenerateNextID(), 0);
                }
                else if (node.Token.IsLog() && node.ChildrenAreIdentical())
                {
                    Write("\tlog(b,b) -> 1");
                    temp = new RPN.Node(GenerateNextID(), 1);
                }
                else if (node.IsExponent() && node.Children[0].IsLog() && node.Children[0].Children[1].Matches(node.Children[1]) )
                {
                    Write($"\tb^log(b,x) -> x");
                    temp = node.Children[0].Children[0];
                }
                else if (node.IsLog() && node.Children[0].IsExponent() && !node.Children[0].Children[1].IsVariable())
                {
                    Write("\tlog(b,R^c) -> c * log(b,R)");
                    RPN.Node exponent = node.Children[0];
                    RPN.Node baseNode = exponent.Children[1];
                    RPN.Node power = exponent.Children[0];

                    RPN.Node log = new RPN.Node(GenerateNextID(), new[] {Clone(baseNode) ,node.Children[1] }, new RPN.Token("log",2,RPN.Type.Function));
                    RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { log, power }, new RPN.Token("*", 2, RPN.Type.Operator));
                    temp = multiply;
                }
                else if (node.IsLn() && node.Children[0].IsExponent() && !node.Children[0].Children[1].IsVariable())
                {
                    Write("\tln(R^c) -> log(e,R^c) -> c * ln(R)");
                    RPN.Node exponent = node.Children[0];
                    RPN.Node power = exponent.Children[0];

                    RPN.Node log = new RPN.Node(GenerateNextID(), new[] { exponent.Children[1] }, new RPN.Token("ln", 1, RPN.Type.Function));
                    RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { log, power }, new RPN.Token("*", 2, RPN.Type.Operator));
                    temp = multiply;
                }
                else if ( (node.IsAddition() || node.IsSubtraction()) &&  node.Children[0].IsLog() && node.Children[1].IsLog() && node.Children[0].Children[1].Matches( node.Children[1].Children[1] ))
                {
                    RPN.Node parameter;
                    if (node.IsAddition())
                    {
                        Write("\tlog(b,R) + log(b,S) -> log(b,R*S)");
                        parameter = new RPN.Node(GenerateNextID(), new[] { node.Children[0].Children[0], node.Children[1].Children[0] }, new RPN.Token("*", 2, RPN.Type.Operator));
                    }
                    else
                    {
                        Write("\tlog(b,R) - log(b,S) -> log(b,R/S)");
                        parameter = new RPN.Node(GenerateNextID(), new[] {  node.Children[0].Children[0], node.Children[1].Children[0] }, new RPN.Token("/", 2, RPN.Type.Operator));
                    }
                    RPN.Node baseNode = node.Children[0].Children[1];
                    RPN.Node log = new RPN.Node(GenerateNextID(), new[] {parameter, baseNode }, new RPN.Token("log", 2, RPN.Type.Function));
                    temp = log;
                }
                else if ( (node.IsAddition() || node.IsSubtraction()) && node.Children[0].IsLn() && node.Children[1].IsLn())
                {
                    RPN.Node parameter;
                    if (node.IsAddition())
                    {
                        Write("\tln(R) + ln(S) -> log(e,R) + log(e,S) -> ln(R*S)");
                        parameter = new RPN.Node(GenerateNextID(), new[] { node.Children[0].Children[0], node.Children[1].Children[0] }, new RPN.Token("*", 2, RPN.Type.Operator));
                    }
                    else
                    {
                        Write("\tln(R) - ln(S) -> log(e,R) - log(e,S) -> ln(R/S)");
                        parameter = new RPN.Node(GenerateNextID(), new[] { node.Children[0].Children[0], node.Children[1].Children[0] }, new RPN.Token("/", 2, RPN.Type.Operator));
                    }
                    RPN.Node ln = new RPN.Node(GenerateNextID(), new[] { parameter }, new RPN.Token("ln", 1, RPN.Type.Function));
                    temp = ln;
                }

                if (temp != null)
                {
                    Assign(node, temp);
                }
            }
            //Imaginary
            else if (mode == SimplificationMode.Imaginary && node.IsSqrt())
            {
                //Any sqrt function with a negative number -> Imaginary number to the root node
                //An imaginary number propagates anyways
                if (node.Children[0].IsLessThanNumber(0))
                {
                    Root = new RPN.Node(GenerateNextID(), double.NaN);
                    Write($"\tSqrt Imaginary Number -> Root.");
                }
                //MAYBE: Any sqrt function with any non-positive number -> Cannot simplify further??
            }
            //Division
            else if (mode == SimplificationMode.Division && node.IsDivision())
            {
                //if there are any divide by zero exceptions -> NaN to the root node
                //NaN propagate anyways
                if (node.Children[0].IsNumber(0))
                {
                    Root = new RPN.Node(GenerateNextID(), double.NaN);
                    Write("\tDivision by zero -> Root");
                }
                else if (node.Children[0].IsNumber(1))
                {
                    Write("\tDivision by one");
                    Assign(node, node.Children[1]);
                }
                //gcd if the leafs are both numbers since the values of the leafs themselves are changed
                //we don't have to worry about if the node is the root or not
                else if (node.Children[0].IsNumber() && node.Children[1].IsNumber())
                {
                    double num1 = node.Children[0].GetNumber();
                    double num2 = node.Children[1].GetNumber();
                    double gcd = RPN.DoFunctions.Gcd(new double[] { num1, num2 });

                    node.Replace(node.Children[0], new RPN.Node(GenerateNextID(), (num1 / gcd)));
                    node.Replace(node.Children[1], new RPN.Node(GenerateNextID(), (num2 / gcd)));
                    Write("\tDivision GCD.");
                }
                else if (node.Children[0].IsDivision() && node.Children[1].IsDivision())
                {
                    Write("\tDivison Flip");
                    RPN.Node[] numerator = { Clone(node.Children[0].Children[1]), Clone(node.Children[1].Children[1]) };
                    RPN.Node[] denominator = { Clone(node.Children[0].Children[0]), Clone(node.Children[1].Children[0]) };

                    RPN.Node top = new RPN.Node(GenerateNextID(), new[] { denominator[0] , numerator[1] }, new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node bottom = new RPN.Node(GenerateNextID(), new[] { denominator[1], numerator[0] }, new RPN.Token("*", 2, RPN.Type.Operator));
                    RPN.Node division = new RPN.Node(GenerateNextID(), new[] { bottom, top }, new RPN.Token("/", 2, RPN.Type.Operator));
                    Assign(node, division);
                }
                //TODO:
                // [f(x)/g(x)]/h(x) - > f(x)/[g(x) * h(x)]
            }
            //Subtraction
            else if (mode == SimplificationMode.Subtraction && node.IsSubtraction())
            {
                //3sin(x) - 3sin(x)
                if ( node.ChildrenAreIdentical())
                {
                    Write("\tSimplification: Subtraction");
                    Assign(node, new RPN.Node(GenerateNextID(), 0));
                }
                //3sin(x) - 2sin(x)
                else if (node.Children[0].IsMultiplication() && node.Children[1].IsMultiplication())
                {
                    if (node.Children[0].Children[1].IsNumber() && node.Children[1].Children[1].IsNumber() && node.Children[0].Children[0].Matches(node.Children[1].Children[0]))
                    {
                        Write("\tSimplification: Subtraction Dual Node");
                        double coefficient = node.Children[1].Children[1].GetNumber() - node.Children[0].Children[1].GetNumber();
                        node.Children[0].Children[1].Token.Value = "0";
                        node.Children[1].Children[1].Token.Value = coefficient.ToString();
                    }
                }
                //3sin(x) - sin(x)
                else if (node.Children[1].IsMultiplication() && node.Children[1].Children[1].IsNumber() && node.Children[1].Children[0].Matches( node.Children[0]) )
                {
                    Write("\tSimplification: Subtraction: Dual Node: Sub one.");
                    RPN.Node temp = new RPN.Node(GenerateNextID(), 0)
                    {
                        Parent = node,
                    };
                    node.Replace( node.Children[0], temp );
                    node.Children[1].Children[1].Token.Value = (double.Parse(node.Children[1].Children[1].Token.Value) - 1).ToString();
                }
                //3sin(x) - 0
                else if (node.Children[0].IsNumber(0))
                {
                    //Root case
                    Assign(node, node.Children[1]);
                    Write("\tSubtraction by zero.");
                }
                //0 - 3sin(x)
                else if (node.Children[1].IsNumber(0))
                {
                    RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), -1), node.Children[0] }, new RPN.Token("*",2,RPN.Type.Operator));

                    Write($"\tSubtraction by zero. Case 2.");
                    Assign(node, multiply);
                }
            }
            //Addition
            else if (mode == SimplificationMode.Addition && node.IsAddition())
            {
                //Is root and leafs have the same hash
                if (node.ChildrenAreIdentical())
                {
                    RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { node.Children[0], new RPN.Node(GenerateNextID(), 2) }, new RPN.Token("*",2,RPN.Type.Operator));
                    Assign(node, multiply);
                    Write("\tSimplification: Addition -> Multiplication");
                }
                //Both nodes are multiplications with 
                //the parent node being addition
                //Case: 2sin(x) + 3sin(x)
                else if (node.Children[0].IsMultiplication() && node.Children[1].IsMultiplication())
                {
                    if ( node.Children[0].Children[1].IsNumber() && node.Children[1].Children[1].IsNumber() && node.Children[0].Children[0].Matches(node.Children[1].Children[0]))
                    {
                        Write("\tSimplification: Addition");
                        double coef1 = double.Parse( node.Children[0].Children[1].Token.Value);
                        double coef2 = double.Parse( node.Children[1].Children[1].Token.Value);
                        string sum = (coef1 + coef2).ToString();

                        node.Children[1].Children[1].Token.Value = sum;
                        node.Children[0].Children[1].Token.Value = "0";
                    }
                }
                //Zero addition
                else if (node.Children[0].IsNumber(0))
                {
                    Write("\tZero Addition.");
                    Assign(node, node.Children[1]);
                }
                //Case: 0 + sin(x)
                else if (node.Children[1].IsNumber(0))
                {
                    //Child 1 is the expression in this case.
                    Write("\tZero Addition. Case 2.");
                    Assign(node, node.Children[0]);
                }
                //7sin(x) + sin(x)
                //C0: Anything
                //C1:C0: Compare hash to C0.
                else if (node.Children[1].IsMultiplication() && node.Children[1].Children[1].IsNumber() && node.Children[1].Children[0].Matches( node.Children[0]) )
                {
                      Write("\tSimplification Addition Dual Node.");

                     //Clears Children
                      node.Children[0].Children = new RPN.Node[0];
                     //Changes child node C0 to a zero number
                      node.Children[0].Token.Value = "0";
                      node.Children[0].Token.Type = RPN.Type.Number;

                      //Changes child node c1:c1 by incrementing it by one.
                      node.Children[1].Children[1].Token.Value = (double.Parse(node.Children[1].Children[1].Token.Value) + 1).ToString();
                }

            }
            else if (mode == SimplificationMode.Trig)
            {
                if (node.IsAddition() &&
                    node.Children[0].IsExponent() &&
                    node.Children[1].IsExponent() &&
                    node.Children[0].Children[0].IsNumber(2) &&
                    node.Children[1].Children[0].IsNumber(2) &&
                    (node.Children[0].Children[1].IsFunction("cos") || node.Children[0].Children[1].IsFunction("sin")) &&
                    (node.Children[1].Children[1].IsFunction("sin") || node.Children[1].Children[1].IsFunction("cos")) &&
                    !node.ChildrenAreIdentical() &&
                    node.Children[0].Children[1].Children[0].Matches(node.Children[1].Children[1].Children[0])
                )
                {
                    RPN.Node head = new RPN.Node(GenerateNextID(), 1);
                    Write("\tsin²(x) + cos²(x) -> 1");
                    Assign(node, head);
                }
                else if ( node.IsDivision() && node.Children[0].IsFunction("sin") && node.Children[1].IsFunction("cos") && node.Children[0].Children[0].Matches( node.Children[1].Children[0] ) )
                {
                    Write("\tcos(x)/sin(x) -> cot(x)");
                    RPN.Node cot = new RPN.Node(GenerateNextID(), new[] { Clone(node.Children[0].Children[0]) }, new RPN.Token("cot", 1, RPN.Type.Function));
                    Assign(node, cot);
                }
                else if (node.IsDivision() && node.Children[0].IsFunction("cos") && node.Children[1].IsFunction("sin") && node.Children[0].Children[0].Matches(node.Children[1].Children[0]))
                {
                    Write("\tsin(x)/cos(x) -> tan(x)");
                    RPN.Node tan = new RPN.Node(GenerateNextID(), new[] { Clone(node.Children[0].Children[0]) }, new RPN.Token("tan", 1, RPN.Type.Function));
                    Assign(node, tan);
                }
                else if (node.IsDivision() && node.Children[1].IsMultiplication() && node.Children[0].IsFunction("sin") && node.Children[1].Children[0].IsFunction("cos") && node.Children[0].Children[0].Matches( node.Children[1].Children[0].Children[0] ) )
                {
                    Write("\t[f(x) * cos(x)]/sin(x) -> f(x) * cot(x)");
                    RPN.Node cot = new RPN.Node(GenerateNextID(), new[] { Clone( node.Children[0].Children[0] ) }, new RPN.Token("cot", 1, RPN.Type.Function));
                    RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { cot, Clone(node.Children[1].Children[1]) }, new RPN.Token("*", 2, RPN.Type.Operator));
                    Assign(node, multiplication);
                }
                else if (node.IsDivision() && node.Children[0].IsFunction("sec"))
                {
                    Write("\tf(x)/sec(g(x)) -> f(x)cos(g(x))");
                    RPN.Node cos = new RPN.Node(GenerateNextID(), new[] { Clone(node.Children[0].Children[0]) }, new RPN.Token("cos", 1, RPN.Type.Function) );
                    RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { cos, Clone(node.Children[1]) }, new RPN.Token("*", 2, RPN.Type.Operator));
                    Assign(node, multiplication);
                }
                else if (node.IsDivision() && node.Children[0].IsFunction("csc"))
                {
                    Write("\tf(x)/csc(g(x)) -> f(x)sin(g(x))");
                    RPN.Node sin = new RPN.Node(GenerateNextID(), new[] { Clone(node.Children[0].Children[0]) }, new RPN.Token("sin", 1, RPN.Type.Function));
                    RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { sin, Clone(node.Children[1]) }, new RPN.Token("*", 2, RPN.Type.Operator));
                    Assign(node, multiplication);
                }
                else if (node.IsDivision() && node.Children[0].IsFunction("cot"))
                {
                    Write("\tf(x)/cot(g(x)) -> f(x)tan(g(x))");
                    RPN.Node tan = new RPN.Node(GenerateNextID(), new[] { Clone(node.Children[0].Children[0]) }, new RPN.Token("tan", 1, RPN.Type.Function));
                    RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { tan, Clone(node.Children[1]) }, new RPN.Token("*", 2, RPN.Type.Operator));
                    Assign(node, multiplication);
                }
                else if (node.IsDivision() && node.Children[0].IsFunction("cos"))
                {
                    Write("\tf(x)/cos(g(x)) -> f(x)sec(g(x))");
                    RPN.Node sec = new RPN.Node(GenerateNextID(), new[] { Clone(node.Children[0].Children[0]) }, new RPN.Token("sec", 1, RPN.Type.Function));
                    RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { sec, Clone(node.Children[1]) }, new RPN.Token("*", 2, RPN.Type.Operator));
                    Assign(node, multiplication);
                }
                else if (node.IsDivision() && node.Children[0].IsFunction("sin"))
                {
                    Write("\tf(x)/sin(g(x)) -> f(x)csc(g(x))");
                    RPN.Node csc = new RPN.Node(GenerateNextID(), new[] { Clone(node.Children[0].Children[0]) }, new RPN.Token("csc", 1, RPN.Type.Function));
                    RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { csc, Clone(node.Children[1]) }, new RPN.Token("*", 2, RPN.Type.Operator));
                    Assign(node, multiplication);
                }
                else if (node.IsDivision() && node.Children[0].IsFunction("tan"))
                {
                    Write("\tf(x)/tan(g(x)) -> f(x)cot(g(x))");
                    RPN.Node cot = new RPN.Node(GenerateNextID(), new[] { Clone(node.Children[0].Children[0]) }, new RPN.Token("cot", 1, RPN.Type.Function));
                    RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { cot, Clone(node.Children[1]) }, new RPN.Token("*", 2, RPN.Type.Operator));
                    Assign(node, multiplication);
                }
                //TODO:
                //cos(x)/[f(x) * sin(x)] -> cot(x)/f(x)
                //[f(x) * cos(x)]/[g(x) * sin(x)] -> [f(x) * cot(x)]/g(x) 

                //[f(x) * sin(x)]/cos(x) -> f(x) * tan(x)
                //sin(x)/[f(x) * cos(x)] -> tan(x)/f(x)
                //[f(x) * sin(x)]/[g(x) * cos(x)] -> [f(x) * tan(x)]/g(x) 

                //[1 + tan(f(x))^2] -> sec(f(x))^2
                //[cot(f(x))^2 + 1] -> csc(f(x))^2
            }
            else if (mode == SimplificationMode.Multiplication && node.IsMultiplication())
            {
                //TODO: If one of the leafs is a division and the other a number or variable
                if (node.ChildrenAreIdentical())
                {
                    RPN.Node temp = node.Children[0];

                    RPN.Node two = new RPN.Node(GenerateNextID(), 2)
                    {
                        Parent = node,
                    };

                    RPN.Node head = new RPN.Node()
                    {
                        Children = new RPN.Node[] {two, temp},
                        ID = GenerateNextID(),
                        Parent = node.Parent,
                        Token = new RPN.Token("^",2,RPN.Type.Operator)
                    };

                    Assign(node, head);
                    Write("\tSimplification: Multiplication -> Exponent");
                }
                else if ( node.Children[0].IsNumber(1) || node.Children[1].IsNumber(1) )
                {
                    RPN.Node temp;

                    if (node.Children[1].IsNumber(1))
                    {
                        temp = node.Children[0];
                    }
                    else 
                    {
                        temp = node.Children[1];
                    }
                    Assign(node, temp);
                    Write($"\tMultiplication by one simplification.");
                }
                else if ( node.Children[1].IsNumber(0) || node.Children[0].IsNumber(0) )
                {
                    Write($"\tMultiplication by zero simplification.");
                    RPN.Node temp = new RPN.Node(GenerateNextID(), 0);
                    Assign(node, temp);
                }
                //sin(x)sin(x)sin(x) -> sin(x)^3
                else if (node.Children[1].IsExponent() && node.Children[1].Children[0].IsNumber() && node.Children[0].Matches( node.Children[1].Children[1]) )
                {
                    Write("\tIncrease Exponent");
                    RPN.Node one = new RPN.Node(GenerateNextID(),1)
                    {
                        Parent = node,
                    };

                    node.Replace( node.Children[0], one );
                    node.Children[1].Children[0].Token.Value = (double.Parse(node.Children[1].Children[0].Token.Value) + 1).ToString();
                }
                else if (node.Children[0].IsExponent() && node.Children[1].IsMultiplication() && node.Children[0].Children[0].IsGreaterThanNumber(0) && node.Children[1].Children[0].Matches( node.Children[0].Children[1]) )
                {
                    Write($"\tIncrease Exponent 2:");
                    node.Children[0].Children[0].Token.Value = (double.Parse(node.Children[0].Children[0].Token.Value) + 1).ToString();
                    node.Children[1].Children[0].Remove(new RPN.Node(GenerateNextID(), 1));
                }
                else if (node.Children[0].IsExponent() && node.Children[1].IsMultiplication() && node.Children[0].Children[1].Matches(node.Children[1]))
                {
                    Write("\tIncrease Exponent 3");
                    node.Children[0].Children[0].Token.Value = (double.Parse(node.Children[0].Children[0].Token.Value) + 1).ToString();
                    node.Children[1].Remove(new RPN.Node(GenerateNextID(), 1));
                }
                else if (node.Children[1].IsNumber() && node.Children[0].IsMultiplication() && node.Children[0].Children[1].IsNumber() && !node.Children[0].Children[0].IsNumber())
                {

                    Write($"\tDual Node Multiplication.");
                    double num1 = double.Parse(node.Children[0].Children[1].Token.Value);
                    double num2 = double.Parse(node.Children[1].Token.Value);

                    node.Children[0].Replace(node.Children[0].Children[1], new RPN.Node(GenerateNextID(), 1));
                    node.Replace(node.Children[1], new RPN.Node(GenerateNextID(), num1 * num2));
                }
                else if ( (node.Children[0].IsDivision() || node.Children[1].IsDivision()) && !(node.Children[0].IsDivision() && node.Children[1].IsDivision()) )
                {
                    Write($"\tExpression times a divison -> Division ");
                    RPN.Node division;
                    RPN.Node expression; 
                    if (node.Children[0].IsDivision())
                    {
                        division = node.Children[0];
                        expression = node.Children[1];
                    }
                    else
                    {
                        division = node.Children[1];
                        expression = node.Children[0];
                    }
                    RPN.Node numerator = division.Children[1];
                    RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] {Clone(numerator), Clone(expression) }, new RPN.Token("*", 2, RPN.Type.Operator));
                    numerator.Remove(multiply);
                    expression.Remove(new RPN.Node(GenerateNextID(), 1));
                }
                else if (node.Children[0].IsDivision() && node.Children[1].IsDivision())
                {
                    Write($"\tDivision times a division -> Division");
                    RPN.Node[] numerator  =  { Clone( node.Children[0].Children[1] ), Clone( node.Children[1].Children[1] )};
                    RPN.Node[] denominator = { Clone( node.Children[0].Children[0] ), Clone( node.Children[1].Children[0] )};
                    RPN.Token multiply = new RPN.Token("*", 2, RPN.Type.Operator);

                    RPN.Node top = new RPN.Node(GenerateNextID(), numerator, multiply);
                    RPN.Node bottom = new RPN.Node(GenerateNextID(), denominator, multiply);
                    RPN.Node division = new RPN.Node(GenerateNextID(), new[] { bottom, top }, new RPN.Token("/", 2, RPN.Type.Operator));

                    node.Children[0].Remove(division);
                    node.Children[1].Remove(new RPN.Node(GenerateNextID(), 1));
                }
                else if (node.Children[0].IsLessThanNumber(0) && node.Children[1].IsLessThanNumber(1))
                {
                    Write("\tA negative times a negative is always positive.");
                    node.Replace(node.Children[0], new RPN.Node(GenerateNextID(), Math.Abs( double.Parse(node.Children[0].Token.Value ))));
                    node.Replace(node.Children[1], new RPN.Node(GenerateNextID(), Math.Abs( double.Parse(node.Children[1].Token.Value ))));
                }
            }
            else if (mode == SimplificationMode.Swap)
            {
                //We can do complex swapping in here
                if (node.IsMultiplication() && node.Children[0].IsMultiplication() && node.Children[0].Children[0].Matches( node.Children[1]) )
                {
                    Write($"\tComplex Swap: Dual Node Multiplication Swap");
                    RPN.Node temp = node.Children[0].Children[1];

                    node.Children[0].Children[1] = node.Children[1];
                    node.Children[1] = temp;
                }
                else if (node.IsMultiplication() && node.Children[0].IsMultiplication() && node.Children[1].IsMultiplication() )
                {
                    if (node.Children[0].Children[1].IsNumber() && node.Children[1].Children[1].IsNumber())
                    {
                        Write($"\tComplex Swap: Tri Node Multiplication Swap");
                        RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { Clone( node.Children[0].Children[1] ), Clone( node.Children[1].Children[1] ) }, new RPN.Token("*", 2, RPN.Type.Operator));
                        node.Children[1].Children[1].Remove(multiply);
                        node.Children[0].Children[1].Remove(new RPN.Node(GenerateNextID(),1));
                    }
                }
                else if (node.IsDivision() && node.Children[0].IsVariable() && node.Children[1].IsNumber())
                {
                    Write($"\tDivision -> Multiplication and exponentiation.");
                    RPN.Node negativeOne = new RPN.Node(GenerateNextID(), -1);
                    RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { negativeOne, node.Children[0] }, new RPN.Token("^", 2, RPN.Type.Operator));

                    node.Token.Value = "*";
                    node.Replace(node.Children[0], exponent);
                }
            }
            else if (mode == SimplificationMode.Exponent && node.IsExponent())
            {
                RPN.Node baseNode = node.Children[1];
                RPN.Node power = node.Children[0];
                if (power.IsNumber(1))
                {
                    Write("\tf(x)^1 -> f(x)");
                    Assign(node, baseNode);
                    power.Delete();
                    node.Delete();
                }
                else if (power.IsNumber(0))
                {
                    Write("\tf(x)^0 -> 1");
                    Assign(node, new RPN.Node(GenerateNextID(), 1));

                    baseNode.Delete();
                    power.Delete();
                    node.Delete();
                }
                else if (baseNode.IsNumber(1))
                {
                    Write("\t1^(fx) -> 1");
                    Assign(node, new RPN.Node(GenerateNextID(), 1));

                    baseNode.Delete();
                    power.Delete();
                    node.Delete();
                }
                else if (power.IsLessThanNumber(0))
                {
                    RPN.Node powerClone = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), -1), Clone(power) } , new RPN.Token("*", 2, RPN.Type.Operator) );
                    RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { powerClone, Clone(baseNode) }, new RPN.Token("^", 2, RPN.Type.Operator));
                    RPN.Node division = new RPN.Node(GenerateNextID(), new[] { exponent, new RPN.Node(GenerateNextID(), 1) }, new RPN.Token("/", 2, RPN.Type.Operator) );
                    Assign(power.Parent, division);
                    Write($"\tf(x)^-c -> 1/f(x)^c");
                }
                else if (power.IsNumber(0.5))
                {
                    RPN.Node sqrt = new RPN.Node(GenerateNextID(), new[] { Clone(baseNode) }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                    Assign(power.Parent, sqrt);
                    Write("\tf(x)^0.5 -> sqrt( f(x) )");
                }
                else if ( ( power.IsNumber() || power.IsConstant() ) && baseNode.IsExponent() && (baseNode.Children[0].IsNumber() || baseNode.Children[0].IsConstant()) )
                {
                    Write("\t(f(x)^c)^a -> f(x)^[c * a]");
                    RPN.Node multiply;

                    if (power.IsNumber() && baseNode.Children[0].IsNumber())
                    {
                        multiply = new RPN.Node(GenerateNextID(), double.Parse( power.Token.Value ) * double.Parse(baseNode.Children[0].Token.Value) );
                    }
                    else
                    {
                        multiply = new RPN.Node(GenerateNextID(), new[] { Clone(power), Clone(baseNode.Children[0]) }, new RPN.Token("*", 2, RPN.Type.Operator));
                    }

                    RPN.Node func = Clone(baseNode.Children[1]);
                    RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { multiply, func }, new RPN.Token("^", 2, RPN.Type.Operator));
                    Assign(power.Parent, exponent);
                }
            }

            //Propagate down the tree IF there is a root 
            //which value is not NaN or a number
            if (Root == null || Root.IsNumber() || Root.IsNumber(double.NaN))
            {
                return;
            }

            //Propagate down the tree
            for (int i = (node.Children.Length - 1); i >= 0; i--)
            {
                Simplify(node.Children[i], mode);
            }
        }

        private void Swap()
        {
            Swap(Root);
        }

        private void Swap(RPN.Node node)
        {
            //Addition operator
            if (node.IsAddition())
            {
                //Two numbers

                //Number and a variable
                if ( node.Children[1].IsNumber() && !node.Children[0].IsNumber())
                {
                    node.Children.Swap(0, 1);
                    Write("\tNode Swap : Add : Number and a nonnumber");
                }
                //Number,variable, or constant and an exponent
                else if ( node.Children[0].IsExponent() && !(node.Children[1].IsExponent() || node.Children[1].IsAddition()) )
                {
                    Write($"\tNode flip addition on {node.ID}");
                    node.Children.Swap(0, 1);
                }
            }
            //Multiplication operator
            else if (node.IsMultiplication())
            {
                //a number and a expression
                if (node.Children[0].IsNumber() && !(node.Children[1].IsNumber() || node.Children[1].IsVariable()))
                {
                    Write($"\tMultiplication Swap.");
                    node.Children.Swap(1, 0);
                }
                else if (node.Children[1].IsExponent() && node.Children[0].IsMultiplication())
                {
                    Write("\tSwap: Multiplication and Exponent");
                    node.Children.Swap(1, 0);
                }
            }

            //Propagate down the tree
            for (int i = (node.Children.Length - 1); i >= 0; i--)
            {
                Swap(node.Children[i]);
            }
        }

        /// <summary>
        /// Simplifies or evaluates meta functions that
        /// cannot be easily represented or understood by PostFix.
        /// </summary>
        public AST MetaFunctions()
        {
            Stopwatch SW = new Stopwatch();
            SW.Start();
            MetaFunctions(Root);
            SW.Stop();

            _data.AddTimeRecord("AST MetaFunctions", SW);

            Simplify();

            return this;
        }

        private void MetaFunctions(RPN.Node node)
        {
            //Propagate down the tree
            for (int i = 0; i < node.Children.Length; i++)
            {
                MetaFunctions(node.Children[i]);
            }

            if (node.IsFunction() && _data.MetaFunctions.Contains(node.Token.Value))
            {
                if (node.Token.Value == "integrate")
                {
                    double answer = double.NaN;
                    if (node.Children.Length == 5)
                    {
                        answer = MetaCommands.Integrate(_rpn,
                            node.Children[4],
                            node.Children[3],
                            node.Children[2],
                            node.Children[1],
                            node.Children[0]);
                    }
                    else if (node.Children.Length == 4)
                    {
                        answer = MetaCommands.Integrate(_rpn,
                            node.Children[3],
                            node.Children[2],
                            node.Children[1],
                            node.Children[0],
                            new RPN.Node(GenerateNextID(), 0.001));
                    }

                    RPN.Node temp = new RPN.Node(GenerateNextID(), answer);
                    Assign(node, temp);
                }
                else if (node.Token.Value == "table")
                {
                    string table;
                    if (node.Children.Length == 5)
                    {
                        table = MetaCommands.Table(_rpn,
                            node.Children[4],
                            node.Children[3],
                            node.Children[2],
                            node.Children[1],
                            node.Children[0]);
                    }
                    else
                    {
                        table = MetaCommands.Table(_rpn,
                            node.Children[3],
                            node.Children[2],
                            node.Children[1],
                            node.Children[0],
                            new RPN.Node(GenerateNextID(), 0.001));
                    }
                    //Write("Table Write: " + table);
                    stdout(table);
                    SetRoot(new RPN.Node(GenerateNextID(), double.NaN));
                }
                else if (node.Token.Value == "derivative")
                {
                    GenerateDerivativeAndReplace(node.Children[1]);
                    Derive(node.Children[0]);
                    Assign(node, node.Children[1]);
                    node.Delete();
                    Solve(Root);
                    //TODO: Remove solve
                    //Tests that fail afterwards :
                    //Constant Multiplications
                    //Power Chain Rule
                    //Sec
                    //Sqrt
                }
            }
        }

        private void Solve(RPN.Node node)
        {
            if (node == null)
            {
                return;
            }

            //All children of a node must be either numbers of constant functions
            bool isSolveable = node.IsSolveable();

            //Functions that are not constants and/or meta functions 
            if ( (node.IsFunction() && ! (node.IsConstant() || _data.MetaFunctions.Contains(node.Token.Value)) || node.IsOperator()) && isSolveable)
            {
                PostFix math = new PostFix(_rpn);
                double answer = math.Compute(node.ToPostFix().ToArray());
                Assign(node, new RPN.Node(GenerateNextID(), answer));
                //Since we solved something lower in the tree we may be now able 
                //to solve something higher up in the tree!
                Solve(node.Parent);
            }

            //Propagate down the tree
            for (int i = 0; i < node.Children.Length; i++)
            {
                Solve(node.Children[i]);
            }
        }


        private AST Derive(RPN.Node variable)
        {
            if (!variable.IsVariable())
            {
                throw new ArgumentException("The variable of deriviation is not a variable!", nameof(variable));
            }

            Stopwatch SW = new Stopwatch();
            SW.Start();

            Write($"Starting to derive ROOT: {Root.ToInfix()}");
            Derive(Root, variable);
            Write("");
            SW.Stop();
            _data.AddTimeRecord("AST.Derive", SW);
            return this;
        }

        private void Derive(RPN.Node node, RPN.Node variable)
        {
            //TODO: Move away from recursion to an itterative approach
            //We do not know in advance the depth of a tree 
            //and given a big enough expression the current recursion 
            //is more likley to fail compared to an itterative approach.

            try
            {
                if (node.Token.Value == "derive")
                {
                    string v = variable.ToInfix();

                    if (node.Children[0].IsAddition() || node.Children[0].IsSubtraction())
                    {
                        if (debug)
                        {
                            string f_x = node.Children[0].Children[0].ToInfix();
                            string g_x = node.Children[0].Children[1].ToInfix();
                            Write($"\td/d{v}[ {f_x} ± {g_x} ] -> d/d{v}( {f_x} ) ± d/d{v}( {g_x} )");
                        }
                        else
                        {
                            Write("\td/dx[ f(x) ± g(x) ] -> d/dx( f(x) ) ± d/dx( g(x) )");
                        }

                        GenerateDerivativeAndReplace(node.Children[0].Children[0]);
                        GenerateDerivativeAndReplace(node.Children[0].Children[1]);
                        //Recurse explicitly down these branches
                        Derive(node.Children[0].Children[0], variable);
                        Derive(node.Children[0].Children[1], variable);
                        //Delete myself from the tree
                        node.Remove();
                    }
                    else if (node.Children[0].IsNumber() || node.Children[0].IsConstant() || (node.Children[0].IsVariable() && node.Children[0].Token.Value != variable.Token.Value) || node.IsSolveable())
                    {
                        if (debug)
                        {
                            Write($"\td/d{v}[ {node.Children[0].ToInfix()} ] -> 0");
                        }
                        else
                        {
                            Write("\td/dx[ c ] -> 0");
                        }
                        node.Children[0].Parent = null;
                        RPN.Node temp = new RPN.Node(GenerateNextID(), 0);
                        //Remove myself from the tree
                        node.Remove(temp);
                    }
                    else if (node.Children[0].IsVariable() && node.Children[0].Token.Value == variable.Token.Value)
                    {
                        if (debug)
                        {
                            Write($"\td/d{v}[ {node.Children[0].ToInfix()} ] -> 1");
                        }
                        else
                        {
                            Write("\td/dx[ x ] -> 1");
                        }
                        node.Children[0].Parent = null;
                        RPN.Node temp = new RPN.Node(GenerateNextID(), 1);
                        //Remove myself from the tree
                        node.Remove(temp);
                    }
                    else if (node.Children[0].IsMultiplication())
                    {
                        //Both numbers
                        if ((node.Children[0].Children[0].IsNumber() || node.Children[0].Children[0].IsConstant()) && (node.Children[0].Children[1].IsNumber() || node.Children[0].Children[1].IsConstant()))
                        {
                            if (debug)
                            {
                                Write($"\td/d{v}[ {node.Children[0].Children[0].ToInfix()} * {node.Children[0].Children[1].ToInfix()} ] -> 0");
                            }
                            else
                            {
                                Write("\td/dx[ c_0 * c_1 ] -> 0");
                            }
                            RPN.Node temp = new RPN.Node(GenerateNextID(), 0);
                            //Remove myself from the tree
                            node.Remove(temp);
                        }
                        //Constant multiplication - 0
                        else if ((node.Children[0].Children[0].IsNumber() || node.Children[0].Children[0].IsConstant()) && node.Children[0].Children[1].IsExpression())
                        {
                            if (debug)
                            {
                                Write($"\td/d{v}[ {node.Children[0].Children[1].ToInfix()} * {node.Children[0].Children[0].ToInfix()}] -> d/d{v}[ {node.Children[0].Children[1].ToInfix()} ] * {node.Children[0].Children[0].ToInfix()}");
                            }
                            else
                            {
                                Write("\td/dx[ f(x) * c] -> d/dx[ f(x) ] * c");
                            }
                            GenerateDerivativeAndReplace(node.Children[0].Children[1]);
                            //Recurse explicitly down these branches
                            Derive(node.Children[0].Children[1], variable);
                            //Remove myself from the tree
                            node.Remove();
                        }
                        //Constant multiplication - 1
                        else if ((node.Children[0].Children[1].IsNumber() || node.Children[0].Children[1].IsConstant()))
                        {
                            if (debug)
                            {
                                string constant = node.Children[0].Children[1].ToInfix();
                                string expr = node.Children[0].Children[0].ToInfix();
                                Write($"\td/d{v}[ {constant} * {expr}] -> {constant} * d/d{v}[ {expr} ]");
                            }
                            else
                            {
                                Write("\td/dx[ c * f(x)] -> c * d/dx[ f(x) ]");
                            }
                            GenerateDerivativeAndReplace(node.Children[0].Children[0]);
                            //Recurse explicitly down these branches
                            Derive(node.Children[0].Children[0], variable);

                            //Remove myself from the tree
                            node.Remove();
                        }
                        //Product Rule [Two expressions] 
                        else
                        {
                            RPN.Token multiply = new RPN.Token("*", 2, RPN.Type.Operator);

                            RPN.Node fNode = node.Children[0].Children[0];
                            RPN.Node gNode = node.Children[0].Children[1];

                            if (debug)
                            {
                                string f = fNode.ToInfix();
                                string g = gNode.ToInfix();
                                Write($"\td/d{v}[ {f} * {g} ] -> {f} * d/d{v}[ {g} ] + d/d{v}[ {f} ] * {g}");
                            }
                            else
                            {
                                Write($"\td/dx[ f(x) * g(x) ] -> f(x) * d/dx[ g(x) ] + d/dx[ f(x) ] * g(x)");
                            }

                            RPN.Node fDerivative = new RPN.Node(GenerateNextID(), new[] { Clone(fNode) }, _derive);
                            RPN.Node gDerivative = new RPN.Node(GenerateNextID(), new[] { Clone(gNode) }, _derive);

                            RPN.Node multiply1 = new RPN.Node(GenerateNextID(), new[] { gDerivative, fNode }, multiply);
                            RPN.Node multiply2 = new RPN.Node(GenerateNextID(), new[] { fDerivative, gNode }, multiply);

                            RPN.Node add = new RPN.Node(GenerateNextID(), new[] { multiply1, multiply2 }, new RPN.Token("+", 2, RPN.Type.Operator));

                            //Remove myself from the tree
                            node.Remove(add);

                            //Explicit recursion
                            Derive(fDerivative, variable);
                            Derive(gDerivative, variable);
                        }
                    }
                    else if (node.Children[0].IsDivision())
                    {
                        //Quotient Rule
                        RPN.Token multiply = new RPN.Token("*", 2, RPN.Type.Operator);

                        RPN.Node numerator = node.Children[0].Children[1];
                        RPN.Node denominator = node.Children[0].Children[0];

                        RPN.Node numeratorDerivative = new RPN.Node(GenerateNextID(), new[] { Clone(numerator) }, _derive);
                        RPN.Node denominatorDerivative = new RPN.Node(GenerateNextID(), new[] { Clone(denominator) }, _derive);

                        RPN.Node multiplicationOne = new RPN.Node(GenerateNextID(), new[] { numeratorDerivative, denominator }, multiply);
                        RPN.Node multiplicationTwo = new RPN.Node(GenerateNextID(), new[] { denominatorDerivative, numerator }, multiply);

                        RPN.Node subtraction = new RPN.Node(GenerateNextID(), new[] { multiplicationTwo, multiplicationOne }, new RPN.Token("-", 2, RPN.Type.Operator));

                        RPN.Node denominatorSquared = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 2), Clone(denominator) }, new RPN.Token("^", 2, RPN.Type.Operator));

                        if (debug)
                        {
                            string n = numerator.ToInfix();
                            string d = denominator.ToInfix();
                            Write($"\td/d{v}[ {n} / {d} ] -> [ d/d{v}( {n} ) * {d} - {d} * d/d{v}( {n} ) ]/{d}^2");
                        }
                        else
                        {
                            Write($"\td/dx[ f(x) / g(x) ] -> [ d/dx( f(x) ) * g(x) - g(x) * d/dx( f(x) ) ]/g(x)^2");
                        }

                        //Replace in tree
                        node.Children[0].Replace(numerator, subtraction);
                        node.Children[0].Replace(denominator, denominatorSquared);
                        //Delete myself from the tree
                        node.Remove();

                        //Explicitly recurse down these branches
                        Derive(subtraction, variable);
                    }
                    //Exponents! 
                    else if (node.Children[0].IsExponent())
                    {
                        RPN.Node baseNode = node.Children[0].Children[1];
                        RPN.Node power = node.Children[0].Children[0];
                        if ((baseNode.IsVariable() || baseNode.IsFunction() || baseNode.IsExpression()) && (power.IsConstant() || power.IsNumber()) && baseNode.Token.Value == variable.Token.Value)
                        {
                            if (debug)
                            {
                                string b = baseNode.ToInfix();
                                string p = power.ToInfix();
                                Write($"\td/d{v}[ {b}^{p} ] -> {p} * {b}^({p} - 1)");
                            }
                            else
                            {
                                Write("\td/dx[ x^n ] -> n * x^(n - 1)");
                            }

                            RPN.Node powerClone = Clone(power);
                            RPN.Node exponent;

                            if (!powerClone.Token.IsNumber())
                            {
                                //1
                                RPN.Node one = new RPN.Node(GenerateNextID(), 1);

                                //(n - 1)
                                RPN.Node subtraction = new RPN.Node(GenerateNextID(), new[] { one, powerClone }, new RPN.Token("-", 2, RPN.Type.Operator));

                                //x^(n - 1) 
                                exponent = new RPN.Node(GenerateNextID(), new RPN.Node[] { subtraction, baseNode }, new RPN.Token("^", 2, RPN.Type.Operator));
                            }
                            else
                            {
                                double temp = double.Parse(powerClone.Token.Value) - 1;
                                exponent = new RPN.Node(GenerateNextID(), new RPN.Node[] { new RPN.Node(GenerateNextID(), temp), baseNode }, new RPN.Token("^", 2, RPN.Type.Operator));
                            }


                            RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { exponent, power }, new RPN.Token("*", 2, RPN.Type.Operator));

                            node.Replace(node.Children[0], multiplication);

                            //Delete self from the tree
                            node.Remove();
                        }
                        else if ( (baseNode.IsFunction() || baseNode.IsExpression()) && (power.IsConstant() || power.IsNumber()))
                        {
                            if (debug)
                            {
                                string b = baseNode.ToInfix();
                                string p = power.ToInfix();
                                Write($"\td/d{v}[ {b}^{p} ] -> {p} * {b}^({p} - 1) * d/d{v}[ {b} ]");
                            }
                            else
                            {
                                Write("\td/dx[ f(x)^n ] -> n * f(x)^(n - 1) * d/dx[ f(x) ]");
                            }

                            RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(baseNode) }, _derive);

                            RPN.Node powerClone = Clone(power);
                            RPN.Node one = new RPN.Node(GenerateNextID(), 1);

                            RPN.Node subtraction = new RPN.Node(GenerateNextID(), new[] { one, powerClone }, new RPN.Token("-", 2, RPN.Type.Operator));

                            //Replace n with (n - 1) 
                            RPN.Node exponent = new RPN.Node(GenerateNextID(), new RPN.Node[] { subtraction, baseNode }, new RPN.Token("^", 2, RPN.Type.Operator));

                            RPN.Node temp = new RPN.Node(GenerateNextID(), new[] { exponent, power }, new RPN.Token("*", 2, RPN.Type.Operator));
                            RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { bodyDerive, temp }, new RPN.Token("*", 2, RPN.Type.Operator));

                            node.Replace(node.Children[0], multiply);

                            //Delete self from the tree
                            node.Remove();

                            Derive(bodyDerive, variable);
                        }
                        else if (baseNode.IsConstant() && baseNode.Token.Value == "e")
                        {
                            if (debug)
                            {
                                string p = power.ToInfix();
                                Write($"\td/d{v}[ e^{p} ] -> d/d{v}[ {p} ] * e^{p}");
                            }
                            else
                            {
                                Write("\td/dx[ e^g(x) ] -> d/dx[ g(x) ] * e^g(x)");
                            }
                            RPN.Node exponent = baseNode.Parent;
                            RPN.Node powerDerivative = new RPN.Node(GenerateNextID(), new[] { Clone(power) }, _derive);
                            RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { powerDerivative, exponent }, new RPN.Token("*", 2, RPN.Type.Operator));
                            node.Replace(power.Parent, multiply);
                            //Delete self from the tree
                            node.Remove();

                            Derive(powerDerivative, variable);
                        }
                        else if ((baseNode.IsConstant() || baseNode.IsNumber()) && (power.IsExpression() || power.IsVariable()))
                        {
                            if (debug)
                            {
                                string b = baseNode.ToInfix();
                                string p = power.ToInfix();
                                Write($"\td/d{v}[ {b}^{p} ] -> ln({b}) * {b}^{p} * d/d{v}[ {p} ]");
                            }
                            else
                            {
                                Write($"\td/dx[ b^g(x) ] -> ln(b) * b^g(x) * d/dx[ g(x) ]");
                            }

                            RPN.Node exponent = baseNode.Parent;
                            RPN.Node ln = new RPN.Node(GenerateNextID(), new[] { Clone(baseNode) }, new RPN.Token("ln", 1, RPN.Type.Function));
                            RPN.Node powerDerivative = new RPN.Node(GenerateNextID(), new[] { Clone(power) }, _derive);
                            RPN.Node temp = new RPN.Node(GenerateNextID(), new[] { exponent, ln }, new RPN.Token("*", 2, RPN.Type.Operator));
                            RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { temp, powerDerivative }, new RPN.Token("*", 2, RPN.Type.Operator));

                            node.Replace(power.Parent, multiply);
                            //Delete self from the tree
                            node.Remove();

                            Derive(powerDerivative, variable);
                        }
                        else
                        {
                            if (debug)
                            {
                                string b = baseNode.ToInfix();
                                string p = power.ToInfix();
                                Write($"\td/d{v}[ {b}^{p} ] -> {b}^{p} * d/d{v}[ {b} * ln( {p} ) ]");
                            }
                            else
                            {
                                Write("\td/dx[ f(x)^g(x) ] -> f(x)^g(x) * d/dx[ g(x) * ln( f(x) ) ]");
                            }
                            RPN.Node exponent = Clone(baseNode.Parent);
                            RPN.Node ln = new RPN.Node(GenerateNextID(), new[] { Clone( baseNode ) }, new RPN.Token("ln", 1, RPN.Type.Function));
                            RPN.Node temp = new RPN.Node(GenerateNextID(), new[] { Clone(power), ln }, new RPN.Token("*", 2, RPN.Type.Operator));
                            RPN.Node derive = new RPN.Node(GenerateNextID(), new[] { temp }, _derive);
                            RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { exponent, derive }, new RPN.Token("*",2,RPN.Type.Operator));

                            node.Replace(power.Parent, multiply);
                            //Delete self from the tree
                            node.Remove();

                            Derive(derive, variable);
                        }
                    }
                    #region Trig
                    else if (node.Children[0].Token.Value == "sin")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ sin({expr}) ] -> cos({expr}) * d/d{v}[ {expr} ]");
                        }
                        else
                        {
                            Write("\td/dx[ sin(g(x)) ] -> cos(g(x)) * d/dx[ g(x) ]");
                        }
                        RPN.Node body = node.Children[0].Children[0];

                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);

                        RPN.Node cos = new RPN.Node(GenerateNextID(), new[] { body }, new RPN.Token("cos", 1, RPN.Type.Function));

                        RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { cos, bodyDerive }, new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], multiply);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "cos")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ cos({expr}) ] -> -sin({expr}) * d/d{v}[ {expr} ]");
                        }
                        else
                        {
                            Write("\td/dx[ cos(g(x)) ] -> -sin(g(x)) * d/dx[ g(x) ]");
                        }
                        RPN.Node body = node.Children[0].Children[0];
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);

                        RPN.Node sin = new RPN.Node(GenerateNextID(), new[] { body }, new RPN.Token("sin", 1, RPN.Type.Function));
                        RPN.Node negativeOneMultiply = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), -1), sin }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { negativeOneMultiply, bodyDerive }, new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], multiply);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "tan")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ tan({expr}) ] -> sec({expr})^2 * d/d{v}[ {expr} ]");
                        }
                        else
                        {
                            Write("\td/dx[ tan(g(x)) ] -> sec(g(x))^2 * d/dx[ g(x) ]");
                        }
                        RPN.Node body = node.Children[0].Children[0];
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);

                        RPN.Node sec = new RPN.Node(GenerateNextID(), new[] { body }, new RPN.Token("sec", 1, RPN.Type.Function));
                        RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 2), sec }, new RPN.Token("^", 2, RPN.Type.Operator));

                        RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { exponent, bodyDerive }, new RPN.Token("*", 2, RPN.Type.Operator));
                        node.Replace(node.Children[0], multiply);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "sec")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ sec({expr}) ] -> tan({expr}) * sec({expr}) * d/d{v}[ {expr} ]");
                        }
                        else
                        {
                            Write("\td/dx[ sec(g(x)) ] -> tan(g(x)) * sec(g(x)) * d/dx[ g(x) ]");
                        }
                        RPN.Token multiplyToken = new RPN.Token("*", 2, RPN.Type.Operator);

                        RPN.Node body = node.Children[0].Children[0];
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);

                        RPN.Node sec = node.Children[0];
                        RPN.Node tan = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, new RPN.Token("tan", 1, RPN.Type.Function));
                        RPN.Node temp = new RPN.Node(GenerateNextID(), new[] { sec, tan }, multiplyToken);
                        RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { bodyDerive, temp }, multiplyToken);

                        node.Replace(node.Children[0], multiply);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "csc")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ csc({expr}) ] -> - cot({expr}) * csc({expr}) * d/d{v}[ {expr} ] ");
                        }
                        else
                        {
                            Write("\td/dx[ csc(g(x)) ] -> - cot(g(x)) * csc(g(x)) * d/dx[ g(x) ] ");
                        }
                        RPN.Token multiplyToken = new RPN.Token("*", 2, RPN.Type.Operator);

                        RPN.Node body = node.Children[0].Children[0];
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);
                        RPN.Node csc = node.Children[0];
                        RPN.Node cot = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, new RPN.Token("cot", 1, RPN.Type.Function));

                        RPN.Node temp = new RPN.Node(GenerateNextID(), new[] { csc, cot }, multiplyToken);
                        RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { temp, bodyDerive }, multiplyToken);

                        node.Replace(node.Children[0], new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), -1), multiply }, multiplyToken));
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "cot")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ cot({expr}) ] -> -csc({expr})^2 * d/d{v}[ {expr} ]");
                        }
                        else
                        {
                            Write("\td/dx[ cot(g(x)) ] -> -csc(g(x))^2 * d/dx[ g(x) ]");
                        }

                        RPN.Node body = node.Children[0].Children[0];
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);
                        RPN.Node csc = new RPN.Node(GenerateNextID(), new[] { body }, new RPN.Token("csc", 1, RPN.Type.Function));
                        RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 2), csc }, new RPN.Token("^", 2, RPN.Type.Operator));
                        RPN.Node temp = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), -1), exponent }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { bodyDerive, temp }, new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], multiply);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "arcsin")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ arcsin({expr}) ] -> d/d{v}[ {expr} ]/sqrt(1 - {expr}^2)");
                        }
                        else
                        {
                            Write("\td/dx[ arcsin(g(x)) ] -> d/dx[ g(x) ]/sqrt(1 - g(x)^2)");
                        }
                        RPN.Node body = Clone(node.Children[0].Children[0]);
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);

                        RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 2), body }, new RPN.Token("^", 2, RPN.Type.Operator));
                        RPN.Node subtraction = new RPN.Node(GenerateNextID(), new[] { exponent, new RPN.Node(GenerateNextID(), 1) }, new RPN.Token("-", 2, RPN.Type.Operator));
                        RPN.Node sqrt = new RPN.Node(GenerateNextID(), new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                        RPN.Node division = new RPN.Node(GenerateNextID(), new[] { sqrt, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], division);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "arccos")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ arccos({expr}) ] -> -1 * d/d{v}[ {expr} ]/sqrt(1 - {expr}^2)");
                        }
                        else
                        {
                            Write("\td/dx[ arccos(g(x)) ] -> -1 * d/dx[ g(x) ]/sqrt(1 - g(x)^2)");
                        }
                        RPN.Node body = Clone(node.Children[0].Children[0]);
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);

                        RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 2), body }, new RPN.Token("^", 2, RPN.Type.Operator));
                        RPN.Node subtraction = new RPN.Node(GenerateNextID(), new[] { exponent, new RPN.Node(GenerateNextID(), 1) }, new RPN.Token("-", 2, RPN.Type.Operator));
                        RPN.Node sqrt = new RPN.Node(GenerateNextID(), new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                        RPN.Node division = new RPN.Node(GenerateNextID(), new[] { sqrt, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                        RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), -1), division }, new RPN.Token("*", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], multiplication);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "arctan")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ arctan({expr}) ] -> d/d{v}[ {expr} ]/(1 + {expr}^2)");
                        }
                        else
                        {
                            Write("\td/dx[ arctan(g(x)) ] -> d/dx[ g(x) ]/(1 + g(x)^2)");
                        }
                        RPN.Node body = Clone(node.Children[0].Children[0]);
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);

                        RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 2), body }, new RPN.Token("^", 2, RPN.Type.Operator));
                        RPN.Node add = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 1), exponent }, new RPN.Token("+", 2, RPN.Type.Operator));
                        RPN.Node division = new RPN.Node(GenerateNextID(), new[] { add, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], division);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "arccot")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ arccot({expr}) ] -> -1 * d/d{v}[ {expr} ]/(1 + {expr}^2)");
                        }
                        else
                        {
                            Write("\td/dx[ arccot(g(x)) ] -> -1 * d/dx[ g(x) ]/(1 + g(x)^2)");
                        }
                        RPN.Node body = Clone(node.Children[0].Children[0]);
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);

                        RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 2), body }, new RPN.Token("^", 2, RPN.Type.Operator));
                        RPN.Node add = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 1), exponent }, new RPN.Token("+", 2, RPN.Type.Operator));
                        RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), -1) , bodyDerive }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node division = new RPN.Node(GenerateNextID(), new[] { add, multiplication }, new RPN.Token("/", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], division);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "arcsec")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ arcsec({expr}) ] -> d/d{v}[ {expr} ]/( {expr} * sqrt({expr}^2 - 1 ) )");
                        }
                        else
                        {
                            Write("\td/dx[ arcsec(g(x)) ] -> d/dx[ g(x) ]/( g(x) * sqrt(g(x)^2 - 1 ) )");
                        }
                        RPN.Node body = Clone(node.Children[0].Children[0]);
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);

                        RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 2), body }, new RPN.Token("^", 2, RPN.Type.Operator));
                        RPN.Node subtraction = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 1), exponent }, new RPN.Token("-", 2, RPN.Type.Operator));
                        RPN.Node sqrt = new RPN.Node(GenerateNextID(), new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                        RPN.Node denominator = new RPN.Node(GenerateNextID(), new[] { sqrt, Clone(body) }, new RPN.Token("*", 2, RPN.Type.Operator));

                        RPN.Node division = new RPN.Node(GenerateNextID(), new[] { denominator, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], division);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].Token.Value == "arccsc")
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ arccsc({expr}) ] -> -1 * d/d{v}[ {expr} ]/( {expr} * sqrt({expr}^2 - 1 ) )");
                        }
                        else
                        {
                            Write("\td/dx[ arccsc(g(x)) ] -> -1 * d/dx[ g(x) ]/( g(x) * sqrt(g(x)^2 - 1 ) )");
                        }
                        RPN.Node body = Clone(node.Children[0].Children[0]);
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);

                        RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 2), body }, new RPN.Token("^", 2, RPN.Type.Operator));
                        RPN.Node subtraction = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 1), exponent }, new RPN.Token("-", 2, RPN.Type.Operator));
                        RPN.Node sqrt = new RPN.Node(GenerateNextID(), new[] { subtraction }, new RPN.Token("sqrt", 1, RPN.Type.Function));
                        RPN.Node denominator = new RPN.Node(GenerateNextID(), new[] { sqrt, Clone(body) }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node multiplication = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), -1), bodyDerive }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node division = new RPN.Node(GenerateNextID(), new[] { denominator, multiplication }, new RPN.Token("/", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], division);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    #endregion
                    else if (node.Children[0].IsSqrt())
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\tsqrt({expr}) -> {expr}^0.5");
                        }
                        else
                        {
                            Write("\tsqrt(g(x)) -> g(x)^0.5");
                        }
                        RPN.Node body = node.Children[0].Children[0];
                        RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), .5), body }, new RPN.Token("^", 2, RPN.Type.Operator));
                        node.Replace(node.Children[0], exponent);
                        Derive(node, variable);
                    }
                    else if (node.Children[0].IsLn())
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\td/d{v}[ ln({expr}) ] -> d/d{v}[ {expr} ]/{expr}");
                        }
                        else
                        {
                            Write("\td/dx[ ln(g(x)) ] -> d/dx[ g(x) ]/g(x)");
                        }
                        RPN.Node body = node.Children[0].Children[0];
                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);
                        RPN.Node division = new RPN.Node(GenerateNextID(), new[] { body, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], division);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].IsLog())
                    {
                        RPN.Token ln = new RPN.Token("ln", 1, RPN.Type.Function);

                        RPN.Node power = node.Children[0].Children[1];
                        RPN.Node body = node.Children[0].Children[0];

                        if (debug)
                        {
                            string b = body.ToInfix();
                            string p = power.ToInfix();
                            Write($"\td/d{v}[ log({b},{p}) ] -> d/d{v}[ {p} ]/({p} * ln({b}))");
                        }
                        else
                        {
                            Write("\td/dx[ log(b,g(x)) ] -> d/dx[ g(x) ]/(g(x) * ln(b))");
                        }

                        RPN.Node bodyDerive = new RPN.Node(GenerateNextID(), new[] { Clone(body) }, _derive);
                        RPN.Node multiply = new RPN.Node(GenerateNextID(), new[] { body, new RPN.Node(GenerateNextID(), new[] { power }, ln) }, new RPN.Token("*", 2, RPN.Type.Operator));
                        RPN.Node division = new RPN.Node(GenerateNextID(), new[] { multiply, bodyDerive }, new RPN.Token("/", 2, RPN.Type.Operator));

                        node.Replace(node.Children[0], division);
                        //Delete self from the tree
                        node.Remove();
                        //Chain Rule
                        Derive(bodyDerive, variable);
                    }
                    else if (node.Children[0].IsAbs())
                    {
                        if (debug)
                        {
                            string expr = node.Children[0].Children[0].ToInfix();
                            Write($"\tabs({expr}) -> sqrt( {expr}^2 )");
                        }
                        else
                        {
                            Write("\tabs(g(x)) -> sqrt( g(x)^2 )");
                        }

                        RPN.Node body = node.Children[0].Children[0];
                        RPN.Node exponent = new RPN.Node(GenerateNextID(), new[] { new RPN.Node(GenerateNextID(), 2), body }, new RPN.Token("^", 2, RPN.Type.Operator));
                        RPN.Node sqrt = new RPN.Node(GenerateNextID(), new[] { exponent }, new RPN.Token("sqrt", 1, RPN.Type.Function));

                        node.Replace(node.Children[0], sqrt);
                        Derive(node, variable);
                    }
                    else if (node.Children[0].Token.Value == "sum")
                    {
                        Write("\tExploding sum");
                        explode(node.Children[0]);
                        Derive(node, variable);
                    }
                    else if (node.Children[0].Token.Value == "avg")
                    {
                        Write("\tExploding avg");
                        explode(node.Children[0]);
                        Derive(node, variable);
                    }
                    else
                    {
                        throw new NotImplementedException($"Derivative of {node.Children[0].ToInfix()} not known at this time.");
                    }
                }
            }
            catch(IndexOutOfRangeException ex)
            {
                throw new InvalidOperationException("Invalid node child access violation", ex);
            }

            try
            {
                //Propagate down the tree
                for (int i = (node.Children.Length - 1); i >= 0; i--)
                {
                    Derive(node.Children[i], variable);
                }
            }
            catch(IndexOutOfRangeException ex)
            {
                throw new InvalidOperationException("Invalid node access propogation violation", ex);
            }
        }


        /// <summary>
        /// converts a vardiac function into a simpler form
        /// </summary>
        /// <param name="node"></param>
        private void explode(RPN.Node node)
        {
            RPN.Token add = new RPN.Token("+", 2, RPN.Type.Operator);
            RPN.Token division = new RPN.Token("/", 2, RPN.Type.Operator);

            //convert a sum to a series of additions
            if (node.Token.Value == "sum")
            {
                if (node.Children.Length == 1)
                {
                    node.Remove();
                    return;
                }

                List<RPN.Token> results = new List<RPN.Token>(node.Children.Length);
                results.AddRange(node.Children[0].ToPostFix());
                results.AddRange(node.Children[1].ToPostFix());
                results.Add(add);
                for (int i = 2; i < node.Children.Length; i++)
                {
                    results.AddRange(node.Children[i].ToPostFix());
                    results.Add(add);
                }
                Assign(node, Generate(results.ToArray()) );
            }
            //convert an avg to a series of additions and a division
            else if (node.Token.Value == "avg")
            {
                if (node.Children.Length == 1)
                {
                    node.Remove();
                    return;
                }

                List<RPN.Token> results = new List<RPN.Token>(node.Children.Length);

                for (int i = 0; i < node.Children.Length; i++)
                {
                    results.AddRange(node.Children[i].ToPostFix());
                }

                results.Add(new RPN.Token("sum", node.Children.Length, RPN.Type.Function));
                results.Add(new RPN.Token(node.Children.Length));
                results.Add(division);
                RPN.Node temp = Generate(results.ToArray());
                explode(temp.Children[1]);
                Assign(node, temp);
            }
        }

        private int GenerateNextID()
        {
            count++;
            return count;
        }

        private void GenerateDerivativeAndReplace(RPN.Node child)
        {
            RPN.Node temp = new RPN.Node()
            {
                Children = new RPN.Node[] {child},
                ID = GenerateNextID(),
                Parent = child.Parent,
                Token = new RPN.Token("derive",1,RPN.Type.Function)
            };

            child.Parent?.Replace(child, temp);

            child.Parent = temp;
        }

        private RPN.Node Clone(RPN.Node node)
        {
            return Generate(node.ToPostFix().ToArray());
        }

        /// <summary>
        /// This is the preferred method of setting the Root
        /// when you are simplifying and not creating a NaN root. 
        /// </summary>
        /// <param name="node"></param>
        private void SetRoot(RPN.Node node)
        {
            node.Parent = null;
            Root = node;
        }

        private void Assign(RPN.Node node, RPN.Node assign)
        {
            if (node.isRoot)
            {
                SetRoot(assign);
            }
            else
            {
                node.Parent.Replace(node, assign);
            }
        }

        private void Write(string message)
        {
            Logger?.Invoke(this, message.Alias());
        }

        private void stdout(string message)
        {
            Output?.Invoke(this, message.Alias());
        }
    }
}