using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbMath.Calculator.Functions
{
    public class Cos : RPN.Node
    {
        private static RPN.Token cos = new RPN.Token("cos", 1, RPN.Type.Function);

        public Cos(RPN.Node node) : base(new RPN.Node[] { node }, cos) { }
    }
}
