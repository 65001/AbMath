using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbMath.Calculator.Functions
{
    class Derive : RPN.Node {
        private static RPN.Token _derive = new RPN.Token("derive", 1, RPN.Type.Function);

        public Derive(RPN.Node node) : base(new RPN.Node[] { node }, _derive) { }
    }
}
