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
            RPN rpn = new RPN("ln(2) + ln(1/3)").Compute();
            Assert.AreEqual("2 3 / ln", rpn.Polish.Print());

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

        [Test]
        public void ZeroMultiplicationDivision()
        {
            RPN rpn = new RPN("0(1/z)").Compute();
            Assert.AreEqual("0 z /", rpn.Polish.Print());

            rpn.SetEquation("(1/z)0").Compute();
            Assert.AreEqual("0 z /", rpn.Polish.Print());
        }

        [Test]
        public void SubtractionDivision()
        {
            RPN rpn = new RPN("cos(x)/z - cos(x)/z").Compute();
            Assert.AreEqual("0 z /", rpn.Polish.Print());
        }

        [Test]
        public void SubtractionCancelation()
        {
            RPN rpn = new RPN("(cos(x)^2)-(-1*(sin(x)^2)) ").Compute();
            Assert.AreEqual("1", rpn.Polish.Print());

            rpn.SetEquation("sin(x) - (-2)").Compute();
            Assert.AreEqual("x sin 2 +", rpn.Polish.Print());
        }

        [Test]
        public void CosSinToCot() { 
            RPN rpn = new RPN("cos(x^3)/(x^2 * sin(x^3))").Compute();
            Assert.AreEqual("x 3 ^ cot x 2 ^ /", rpn.Polish.Print());

            rpn.SetEquation("cos(x^3)/(sin(x^3) * x^2)").Compute();
            Assert.AreEqual("x 3 ^ cot x 2 ^ /", rpn.Polish.Print());
        }

        [Test]
        public void LnPowerRule()
        {
            RPN rpn = new RPN("ln(x^2)").Compute();
            Assert.AreEqual("2 x ln *", rpn.Polish.Print());
        }
    }
}