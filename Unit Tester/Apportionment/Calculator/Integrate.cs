using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class Integrate
    {
        [Test]
        public void Constants()
        {
            RPN rpn = new RPN("integrate(y,x,a,b)").Compute();
            Assert.AreEqual("y b a - *", rpn.Polish.Print());

            rpn.SetEquation("integrate(sin(y),x,a,b)").Compute();
            Assert.AreEqual("y sin b a - *", rpn.Polish.Print());

            rpn.SetEquation("integrate(sin(y)cos(y),x,a,b)").Compute();
            Assert.AreEqual("y cos y sin * b a - *", rpn.Polish.Print());
        }

        [Test]
        public void Coefficient()
        {
            RPN rpn = new RPN("integrate(c*x,x,a,b)").Compute();
            Assert.AreEqual("c b 2 ^ a 2 ^ - * 2 /", rpn.Polish.Print());

            rpn.SetEquation("integrate(5*x,x,a,b)").Compute();
            Assert.AreEqual("5 b 2 ^ a 2 ^ - * 2 /", rpn.Polish.Print());
        }

        [Test]
        public void SingleVariable()
        {
            RPN rpn = new RPN("integrate(x,x,a,b)").Compute();
            Assert.AreEqual("b 2 ^ a 2 ^ - 2 /", rpn.Polish.Print());
        }

    }
}
