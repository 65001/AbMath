using System;
using System.Collections.Generic;
using System.Text;

namespace AbMath.Utilities
{
    public partial class RPN
    {
        private void DefaultFunctions()
        {
            AddFunction("sin", new Functions
            {
                Arguments = 1,
                Compute = new Run(DoFunctions.Sin)
            });

            AddFunction("cos", new Functions
            {
                Arguments = 1,
                Compute = new Run(DoFunctions.Cos)
            });

            AddFunction("tan", new Functions
            {
                Arguments = 1,
                Compute = new Run(DoFunctions.Tan)
            });

            AddFunction("max", new Functions
            {
                Arguments = 2,
                Compute = new Run(DoFunctions.Max)
            });

            AddFunction("min", new Functions
            {
                Arguments = 2,
                Compute = new Run(DoFunctions.Min)
            });

            AddFunction("sqrt", new Functions
            {
                Arguments = 1,
                Compute = new Run(DoFunctions.Sqrt)
            });

            AddFunction("round", new Functions
            {
                Arguments = 2,
                Compute = new Run(DoFunctions.Round)
            });

            AddFunction("ln", new Functions
            {
                Arguments = 1,
                Compute = new Run(DoFunctions.ln)
            });

            AddFunction("log", new Functions
            {
                Arguments = 2,
                Compute = new Run(DoFunctions.Log)
            });

            AddFunction("pi", new Functions
            {
                Arguments = 0,
                Compute = new Run(DoFunctions.Pi)
            });

            AddFunction("e", new Functions
            {
                Arguments = 0,
                Compute = new Run(DoFunctions.E)
            });
        }
    }
}
