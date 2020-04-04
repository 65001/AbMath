using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class SumTest
    {
        [Test]
        public void ConstantDivisionFactorial()
        {
            RPN test = new RPN("sum(x/50,x,0,b)").Compute();
            Assert.AreEqual("b b 1 + * 2 50 * /", test.Polish.Print());
        }
    }
}
