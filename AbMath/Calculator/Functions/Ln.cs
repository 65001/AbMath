using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbMath.Calculator.Functions
{
    public class Ln : RPN.Node
    {
        private static RPN.Token ln = new RPN.Token("ln", 1, RPN.Type.Function);

        public Ln(RPN.Node node) : base(new RPN.Node[] { node }, ln) { }
    }
}
