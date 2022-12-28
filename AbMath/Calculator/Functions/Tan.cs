using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbMath.Calculator.Functions
{
    public class Tan : RPN.Node
    {
        private static RPN.Token tan = new RPN.Token("tan", 1, RPN.Type.Function);

        public Tan(RPN.Node node) : base(new RPN.Node[] { node }, tan) { }
    }
}
