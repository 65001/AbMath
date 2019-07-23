using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class TokenizerTest
    {
        [Test]
        public void DebugMode()
        {
            Assert.IsFalse(new RPN("").Data.DebugMode);
        }

        [Test]
        public void UnaryFunction()
        {
            RPN test = new RPN("-pi").Compute();
            Assert.AreEqual("-1 pi *", test.Polish.Print());
        }

        [Test]
        public void ComplexFunction()
        {
            RPN test = new RPN("sin(16pi)").Compute();
            Assert.AreEqual("16 pi * sin", test.Polish.Print());
        }

        [Test]
        public void ConstantFunction()
        {
            RPN test = new RPN("2e").Compute();
            Assert.AreEqual("2 e *", test.Polish.Print());
        }

        [Test]
        public void ConstantFunctionRight()
        {
            RPN test = new RPN("pi(2)").Compute();
            Assert.AreEqual("2 pi *", test.Polish.Print());
        }

        [Test]
        public void MultiTermMultiply()
        {
            RPN test = new RPN("(30.1)2.5(278)").Compute();
            Assert.AreEqual("278 30.1 2.5 * *", test.Polish.Print());
        }

        [Test]
        public void VariableAdd()
        {
            RPN test = new RPN("2+x").Compute();
            Assert.AreEqual("x 2 +", test.Polish.Print());
        }

        [Test]
        public void SimpleAdd()
        {
            RPN test = new RPN("3 + 2").Compute();
            Assert.AreEqual("3 2 +", test.Polish.Print());
        }

        [Test]
        public void MultiTermAdd()
        {
            RPN test = new RPN("2 + 3 + 2").Compute();
            Assert.AreEqual("2 3 + 2 +", test.Polish.Print());
        }

        [Test]
        public void MultiTermAddNoSpace()
        {
            RPN test = new RPN("2+3+2").Compute();
            Assert.AreEqual("2 3 + 2 +", test.Polish.Print());
        }

        [Test]
        public void SimpleSubtract()
        {
            RPN test = new RPN("4 - 2").Compute();
            Assert.AreEqual("4 2 -", test.Polish.Print());
        }

        [Test]
        public void Wikipedia()
        {
            RPN test = new RPN("3 + 4 * 2 / ( 1 - 5 ) ^ 2 ^ 3").Compute();
            Assert.AreEqual("4 2 * 1 5 - 2 3 ^ ^ / 3 +", test.Polish.Print());
        }

        [Test]
        public void Functions()
        {
            RPN test = new RPN("sin ( max ( 2 , 3 ) / 3 * 3.1415 )").Compute();
            Assert.AreEqual("3.1415 2 3 max * 3 / sin", test.Polish.Print());
        }

        [Test]
        public void Variables()
        {
            RPN test = new RPN("2 * x").Compute();
            Assert.AreEqual("2 x *", test.Polish.Print());
        }

        [Test]
        public void CompositeMax()
        {
            RPN test = new RPN("max(sqrt(16),100)").Compute();
            Assert.AreEqual("16 sqrt 100 max", test.Polish.Print());
        }

        [Test]
        public void VariableMultiplication()
        {
            RPN test = new RPN("v + a * t").Compute();
            Assert.AreEqual("a t * v +", test.Polish.Print());
        }

        [Test]
        public void ArityConstantMax()
        {
            RPN test = new RPN("max(1, pi)").Compute();
            Assert.AreEqual("1 pi max", test.Polish.Print());
        }

        [Test]
        public void VariableExponents()
        {
            RPN test = new RPN("x^2").Compute();
            Assert.AreEqual("x 2 ^", test.Polish.Print());
        }

        [Test]
        public void VariableChainMultiplication()
        {
            RPN test = new RPN("x2sin(x) + x3sin(x)").Compute();
            Assert.AreEqual("x 2 * x sin * x 3 * x sin * +", test.Polish.Print());
        }

        [Test]
        public void Aliasing()
        {
            RPN test = new RPN("7÷2").Compute();
            Assert.AreEqual("7 2 /", test.Polish.Print());
        }

        [Test]
        public void UnaryStart()
        {
            RPN test = new RPN("-2 + 4").Compute();
            Assert.AreEqual("-2 4 +", test.Polish.Print());
        }

        [Test]
        public void ComplexExpression()
        {
            RPN test = new RPN("x >= 0 && x <= 5").Compute();
            Assert.AreEqual("x 0 >= x 5 <= &&", test.Polish.Print());
        }

        [Test]
        public void MixedDivisionMultiplication()
        {
            RPN test = new RPN("1/2x").Compute();
            Assert.AreEqual("1 2 x * /", test.Polish.Print());
        }

        [Test]
        public void VariableContains()
        {
            RPN test = new RPN("x * 2").Compute();
            Assert.AreEqual("x 2 *", test.Polish.Print());
            Assert.AreEqual(true, test.Data.ContainsVariables);
        }

        [Test]
        public void DoubleTokenize()
        {
            RPN test = new RPN("x * 2").Compute();
            Assert.AreEqual("x 2 *", test.Polish.Print());
            Assert.AreEqual(true, test.Data.ContainsVariables);

            test.SetEquation("2x + 2").Compute();
            Assert.AreEqual("2 x * 2 +", test.Polish.Print());
        }
    }
}
