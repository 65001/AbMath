using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbMath.Calculator.Functions
{
    public class Sin : RPN.Node
    {
        private static RPN.Token sin = new RPN.Token("sin", 1, RPN.Type.Function);

        public Sin(RPN.Node node) : base(new RPN.Node[] { node }, sin) { }
    }
}
