using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class AST
    {
        [Test]
        public void IncreaseExponent()
        {
            RPN rpn = new RPN("sin(x)sin(x)sin(x)").Compute();
            Assert.AreEqual("x sin 3 ^", rpn.Polish.Print());
        }

        [Test]
        public void ComplexIncreaseExponent()
        {
            RPN rpn = new RPN("(x(x + 1))(x(x + 1))(x(x + 1))").Compute();
            Assert.AreEqual("x 1 + x * 3 ^", rpn.Polish.Print());
        }

        [Test]
        public void OneRaisedExponent()
        {
            RPN rpn = new RPN("1^x").Compute();
            Assert.AreEqual("1", rpn.Polish.Print());
        }

        [Test]
        public void TrigIdentiySinAndCos()
        {
            RPN rpn = new RPN("sin(x)sin(x) + cos(x)cos(x)").Compute();
            Assert.AreEqual("1", rpn.Polish.Print());
        }

        [Test]
        public void Simplification()
        {
            RPN rpn = new RPN("3sin(x) - 4sin(x) + sin(x)").Compute();
            Assert.AreEqual("0", rpn.Polish.Print());
        }

        [Test]
        public void LogExponentMultiply()
        {
            RPN rpn = new RPN("log(2,3^x)").Compute();
            Assert.AreEqual("x 2 3 log *",rpn.Polish.Print());
        }

        [Test]
        public void LnExponentMultiply()
        {
            RPN rpn = new RPN("ln(2^x)").Compute();
            Assert.AreEqual("x 2 ln *", rpn.Polish.Print());
        }

        [Test]
        public void LogAddOrSub()
        {
            RPN rpn = new RPN("log(b,R) + log(b,S)").Compute();
            Assert.AreEqual("b R S * log", rpn.Polish.Print());

            rpn.SetEquation("log(b,R) - log(b,S)").Compute();
            Assert.AreEqual("b R S / log", rpn.Polish.Print());
        }

        [Test]
        public void LnAddOrSub()
        {
            RPN rpn = new RPN("ln(2) + ln(1/2)").Compute();
            Assert.AreEqual("1 ln", rpn.Polish.Print());

            rpn.SetEquation("ln(2) - ln(3)").Compute();
            Assert.AreEqual("2 3 / ln", rpn.Polish.Print());
        }

        [Test]
        public void ExpressionTimesDivision()
        {
            RPN rpn = new RPN("1(3/4)").Compute();
            Assert.AreEqual("3 4 /", rpn.Polish.Print());
        }

        [Test]
        public void DivisionTimesDivision()
        {
            RPN rpn = new RPN("(3/4)(1/4)").Compute();
            Assert.AreEqual("3 4 2 ^ /", rpn.Polish.Print());
        }

        [Test]
        public void DivisionFlip()
        {
            RPN rpn = new RPN("(5/x)/(x/3)").Compute();
            Assert.AreEqual("5 3 * x 2 ^ /", rpn.Polish.Print());

            rpn.SetEquation("[f/g]/h").Compute();
            Assert.AreEqual("f g h * /", rpn.Polish.Print());
        }

        [Test]
        public void DivisionConstantShared()
        {
            RPN rpn = new RPN("(3x^3)/3").Compute();
            Assert.AreEqual("x 3 ^", rpn.Polish.Print());

            rpn.SetEquation("( (x^3) 3)/3 ").Compute();
            Assert.AreEqual("x 3 ^", rpn.Polish.Print());
        }

        [Test]
        public void Cot()
        {
            RPN rpn = new RPN("cos(x^2)/sin(x^2)").Compute();
            Assert.AreEqual("x 2 ^ cot", rpn.Polish.Print());

            rpn.SetEquation("[(2x) * cos(x^2)]/sin(x^2)").Compute();
            Assert.AreEqual("2 x * x 2 ^ cot *", rpn.Polish.Print());
        }

        [Test]
        public void Tan()
        {
            RPN rpn = new RPN("sin(x^2)/cos(x^2)").Compute();
            Assert.AreEqual("x 2 ^ tan", rpn.Polish.Print());
        }

        [Test]
        public void PowerReduction()
        {
            RPN rpn = new RPN("x^10/x^5").Compute();
            Assert.AreEqual("x 6 ^ x /", rpn.Polish.Print());
        }
    }
}