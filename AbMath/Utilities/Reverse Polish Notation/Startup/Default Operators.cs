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
                Compute = new Run(DoOperators.Power)
            });
            
            AddOperator("!", new Operators
            {
                Assoc = Assoc.Left,
                weight = 4,
                Arguments = 1,
                Compute = new Run(DoOperators.Factorial)
            });
            
            AddOperator("%", new Operators
            {
                Assoc = Assoc.Left,
                weight = 3,
                Arguments = 2,
                Compute = new Run(DoOperators.Mod)
            });

            AddOperator("/", new Operators
            {
                Assoc = Assoc.Left,
                weight = 3,
                Arguments = 2,
                Compute = new Run(DoOperators.Divide)
            });

            AddOperator("*", new Operators
            {
                Assoc = Assoc.Left,
                weight = 3,
                Arguments = 2,
                Compute = new Run(DoOperators.Multiply)
            });

            AddOperator("+", new Operators
            {
                Assoc = Assoc.Left,
                weight = 2,
                Arguments = 2,
                Compute = new Run(DoOperators.Add)
            });

            AddOperator("−", new Operators
            {
                Assoc = Assoc.Left,
                weight = 2,
                Arguments = 2,
                Compute = new Run(DoOperators.Subtract)
            });

            AddOperator("-", new Operators
            {
                Assoc = Assoc.Left,
                weight = 2,
                Arguments = 2,
                Compute = new Run(DoOperators.Subtract)
            });

            AddOperator(">", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoOperators.GreateerThan)
            });

            AddOperator("<", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoOperators.LessThan)
            });

            AddOperator("=", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoOperators.Equals)
            });

            AddOperator(">=", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoOperators.GreaterThanOrEquals)
            });

            AddOperator("<=", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoOperators.LessThanOrEquals)
            });

            AddOperator("!=", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoOperators.NotEquals)
            });

            AddOperator("&&", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoOperators.And)
            });

            AddOperator("||", new Operators
            {
                Assoc = Assoc.Left,
                weight = 1,
                Arguments = 2,
                Compute = new Run(DoOperators.Or)
            });
        }
    }
}
