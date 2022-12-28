using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbMath.Calculator.Functions
{
    public class Cot : RPN.Node
    {
        private static RPN.Token cot = new RPN.Token("cot", 1, RPN.Type.Function);

        public Cot(RPN.Node node) : base(new RPN.Node[] { node }, cot) { }
    }
}
