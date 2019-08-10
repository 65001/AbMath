using System;
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
            RPN rpn = new RPN("x - x + 2 - 2").Compute();
            Assert.AreEqual("0", rpn.Polish.Print());
        }

        [Test]
        public void VariableSubtraction()
        {
            RPN rpn = new RPN("2x - 3x").Compute();
            Assert.AreEqual("-1 x *", rpn.Polish.Print());

            rpn.SetEquation("2x - x").Compute();
            Assert.AreEqual("x", rpn.Polish.Print());
        }

        [Test]
        public void VariableAddition()
        {
            RPN rpn = new RPN("2x + 3x").Compute();
            Assert.AreEqual("5 x *", rpn.Polish.Print());
        }

        [Test]
        public void VariableAdditionExponent()
        {
            //TODO: Normalize
            RPN rpn = new RPN("x - x^2").Compute();
            Assert.AreEqual("x x 2 ^ -", rpn.Polish.Print());
        }

        [Test]
        public void VariableAdditionComplexExponent()
        {
            RPN rpn = new RPN("2x + 3x^2").Compute();
            Assert.AreEqual("3 x 2 ^ * 2 x * +", rpn.Polish.Print());
        }

        [Test]
        public void VariableParanthesisReduction()
        {
            RPN rpn = new RPN("3(x^2 - x^2)").Compute();
            Assert.AreEqual("0", rpn.Polish.Print());
        }

        [Test]
        public void VariableExponentVariable()
        {
            RPN rpn = new RPN("x^@ - x^@").Compute();
            Assert.AreEqual("0", rpn.Polish.Print());
        }

        [Test]
        public void VariableRaisedToZero()
        {
            RPN rpn = new RPN("x^0").Compute();
            Assert.AreEqual("1", rpn.Polish.Print());
        }

        [Test]
        public void Swap()
        {
            //TODO: Normalize
            RPN rpn = new RPN("x^@ + x - x^@ - x").Compute();
            Assert.AreEqual("x @ ^ x + x @ ^ - x -", rpn.Polish.Print());
        }

        [Test]
        public void IllegalSwap()
        {
            RPN rpn = new RPN("x^2 + x^3/x").Compute();
            Assert.AreEqual("x 2 ^ x 3 ^ x / +", rpn.Polish.Print());
        }

        [Test]
        public void Power()
        {
            RPN rpn = new RPN("2x(3x^2)").Compute();
            Assert.AreEqual("2 3 * x 3 ^ *", rpn.Polish.Print());
        }
    }

    [TestFixture]
    public class PostSimplification
    {
        [Test]
        public void Log_Base_Power()
        {
            RPN rpn = new RPN("log(b,b)").Compute();
            Assert.AreEqual("1", rpn.Polish.Print());
        }


        [Test]
        public void Log_Power()
        {
            RPN rpn = new RPN("log(b,1)").Compute();
            Assert.AreEqual("0", rpn.Polish.Print());

            rpn.SetEquation("log(x^2,1)").Compute();
            Assert.AreEqual("0", rpn.Polish.Print());
        }

        [Test]
        public void Exponent_Log_Power()
        {
            RPN rpn = new RPN("b^log(b,x)").Compute();
            Assert.AreEqual("x", rpn.Polish.Print());

            rpn.SetEquation("(2x)^log(2x,2)").Compute();
            Assert.AreEqual("2", rpn.Polish.Print());
        }

        [Test]
        public void ZeroSimplification()
        {
            RPN rpn = new RPN("0(x)").Compute();
            Assert.AreEqual("0", rpn.Polish.Print());
        }

        [Test]
        public void Sqrt_to_abs()
        {
            RPN rpn = new RPN("sqrt(x^2)").Compute();
            Assert.AreEqual("x abs", rpn.Polish.Print());
        }
    }
}
