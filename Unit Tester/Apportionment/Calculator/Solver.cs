using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    [Parallelizable]
    public class Solver
    {
        [Test]
        public void SimpleAdd()
        {
            RPN rpn = new RPN("solve(x + 2,4)").Compute();
            Assert.AreEqual("x 2 =", rpn.Polish.Print());

            rpn.SetEquation("solve(2 + x,4)").Compute();
            Assert.AreEqual("x 2 =", rpn.Polish.Print());
        }

        [Test]
        public void SimpleSubtraction()
        {
            RPN rpn = new RPN("solve(x - 2,4)").Compute();
            Assert.AreEqual("x 6 =", rpn.Polish.Print());

            rpn.SetEquation("solve(2 - x,4)").Compute();
            Assert.AreEqual("x -2 =", rpn.Polish.Print());
        }

        [Test]
        public void SimpleMultiplication()
        {
            RPN rpn = new RPN("solve(x/2,10)").Compute();
            Assert.AreEqual("x 20 =", rpn.Polish.Print());
        }
    }
}
