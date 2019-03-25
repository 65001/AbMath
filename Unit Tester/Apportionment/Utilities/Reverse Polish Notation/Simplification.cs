using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class SimplificationTest
    {
        [Test]
        public void VariableSimplification()
        {
            RPN rpn = new RPN("x - x + 2 - 2");
            rpn.Compute();
            RPN.Simplify SI = new RPN.Simplify(rpn.Data);
            Console.WriteLine(SI.Apply(rpn.Tokens).ToArray().Print());

            if ("0 + 0" != SI.Apply(rpn.Tokens).ToArray().Print())
            {
                Assert.Fail();
            }
        }
    }
}
