using System;
using CLI;

namespace AbMath.Calculator
{
    public static partial class MetaCommands
    {
        public static string Table(RPN rpn, RPN.Node expression, RPN.Node variable, RPN.Node a, RPN.Node b, RPN.Node frequencey)
        {
            //A regular function
            PostFix math = new PostFix(rpn);

            double start = math.Compute(a.ToPostFix().ToArray());
            double end = math.Compute(b.ToPostFix().ToArray());
            double freq = math.Compute(frequencey.ToPostFix().ToArray());
            double PrevAnswer = 0;

            math.SetPolish(expression.ToPostFix().ToArray());

            double DeltaX = end - start;
            double n = DeltaX / freq;
            int max = (int)Math.Ceiling(n);

            Tables<double> table = new Tables<double>(new Config()
            {
                Format = rpn.Data.DefaultFormat,
                Title = "Table"
            });
            table.Add(new Schema {Column = $"{variable.Token.Value}", Width = 26});
            table.Add(new Schema { Column = $"f({variable.Token.Value})", Width = 26 });

            for (int x = 0; x <= max; x++)
            {
                double RealX = start + x * DeltaX / n;
                math.SetVariable("ans", PrevAnswer);
                math.SetVariable(variable.Token.Value, RealX);
                double answer = math.Compute();
                table.Add(new double[] {RealX, answer});
                PrevAnswer = answer;
                math.Reset();
            }


            return table.ToString();
        }
    }
}
