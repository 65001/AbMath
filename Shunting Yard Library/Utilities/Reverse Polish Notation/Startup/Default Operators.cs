using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public partial class RPN
    {
        private void DefaultOperators()
        {
            AddOperator("^", new Operators
            {
                Assoc = Assoc.Right,
                weight = 4,
                Arguments = 2,
                Compute = new Run(DoMath.Power)
            });
            
            AddOperator("!", new Operators
            {
                Assoc = Assoc.Left,
                weight = 4,
                Arguments = 1,
                Compute = new Run(DoMath.Factorial)
            });
            
            AddOperator("%", new Operators
            {
                Assoc = Assoc.Left,
                weight = 3,
                Arguments = 2,
                Compute = new Run(DoMath.Mod)
            });

            AddOperator("/", new Operators
            {
                Assoc = Assoc.Left,
                weight = 3,
                Arguments = 2,
                Compute = new Run(DoMath.Divide)
            });

            AddOperator("÷", new Operators
            {
                Assoc = Assoc.Left,
                weight = 3,
                Arguments = 2,
                Compute = new Run(DoMath.Divide)
            });

            AddOperator("*", new Operators
            {
                Assoc = Assoc.Left,
                weight = 3,
                Arguments = 2,
                Compute = new Run(DoMath.Multiply)
            });

            AddOperator("+", new Operators
            {
                Assoc = Assoc.Left,
                weight = 2,
                Arguments = 2,
                Compute = new Run(DoMath.Add)
            });

            AddOperator("−", new Operators
            {
                Assoc = Assoc.Left,
                weight = 2,
                Arguments = 2,
                Compute = new Run(DoMath.Subtract)
            });

            AddOperator("-", new Operators
            {
                Assoc = Assoc.Left,
                weight = 2,
                Arguments = 2,
                Compute = new Run(DoMath.Subtract)
            });

            AddOperator(">", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoMath.GreateerThan)
            });

            AddOperator("<", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoMath.LessThan)
            });

            AddOperator("=", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoMath.Equals)
            });

            AddOperator("!=", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoMath.NotEquals)
            });

            AddOperator("&&", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoMath.And)
            });

            AddOperator("||", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoMath.Or)
            });
        }
    }
}
