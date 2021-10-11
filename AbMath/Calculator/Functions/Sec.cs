using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbMath.Calculator.Functions
{
    public class Sec : RPN.Node
    {
        private static RPN.Token sec = new RPN.Token("sec", 1, RPN.Type.Function);

        public Sec(RPN.Node node) : base(new RPN.Node[] { node }, sec) { }
    }
}
