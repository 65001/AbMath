using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class ImplicitShunting
    {
        [Test]
        public void Left()
        {
            RPN test = new RPN("4sin(2)").Compute();
            Assert.AreEqual("4 2 sin *", test.Polish.Print());
        }

        [Test]
        public void LeftBracket()
        {
            RPN test = new RPN("4(2)").Compute();
            Assert.AreEqual("4 2 *", test.Polish.Print());
        }

        [Test]
        public void LeftEOS()
        {
            RPN test = new RPN("2x").Compute();
            Assert.AreEqual("2 x *", test.Polish.Print());
        }

        [Test]
        public void LeftVariable()
        {
            RPN test = new RPN("x2").Compute();
            Assert.AreEqual("2 x *", test.Polish.Print());
        }

        [Test]
        public void Mix()
        {
            RPN test = new RPN("12(3) + 8(1.01)").Compute();
            Assert.AreEqual("12 3 * 8 1.01 * +", test.Polish.Print());
        }

        [Test]
        public void Right()
        {
            RPN test = new RPN("sin(2)4").Compute();
            Assert.AreEqual("4 2 sin *", test.Polish.Print());
        }

        [Test]
        public void RightBracket()
        {
            RPN test = new RPN("(2)4").Compute();
            Assert.AreEqual("2 4 *", test.Polish.Print());
        }

        [Test]
        public void VariableLeft()
        {
            RPN test = new RPN("x(y)").Compute();
            Assert.AreEqual("x y *", test.Polish.Print());
        }

        [Test]
        public void VariableRight()
        {
            RPN test = new RPN("(x)(y)").Compute();
            Assert.AreEqual("x y *", test.Polish.Print());
        }

        [Test]
        public void MultipleFunctions()
        {
            RPN test = new RPN("sin(x)cos(x)").Compute();
            Assert.AreEqual("x cos x sin *", test.Polish.Print());
        }

        [Test]
        public void Unary()
        {
            RPN test = new RPN("-(3^2)").Compute();
            Assert.AreEqual("-1 3 2 ^ *", test.Polish.Print());
        }
    }
}
