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
            RPN rpn = new RPN("sin(x)sin(x)sin(x)");
            rpn.Compute();
            string tokens = rpn.Polish.Print();
            if ("x sin 3 ^" != tokens)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ComplexIncreaseExponent()
        {
            RPN rpn = new RPN("(x(x + 1))(x(x + 1))(x(x + 1))");
            rpn.Compute();
            string tokens = rpn.Polish.Print();
            if ("x 1 x + * 3 ^" != tokens)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void TrigIdentiySinAndCos()
        {
            RPN rpn = new RPN("sin(x)sin(x) + cos(x)cos(x)");
            rpn.Compute();
            if ("1" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Simplification()
        {
            RPN rpn = new RPN("3sin(x) - 4sin(x) + sin(x)");
            rpn.Compute();
            if ("0" != rpn.Polish.Print())
            {
               Assert.Fail();
            }
        }

        [Test]
        public void LogExponentMultiply()
        {
            RPN rpn = new RPN("log(2,3^x)");
            rpn.Compute();
            if ("x 2 3 log *" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LnExponentMultiply()
        {
            RPN rpn = new RPN("ln(2^x)");
            rpn.Compute();
            if ("x 2 ln *" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LogAddOrSub()
        {
            RPN rpn = new RPN("log(b,R) + log(b,S)");
            rpn.Compute();
            if("b R S * log" != rpn.Polish.Print())
            {
                Assert.Fail();
            }

            rpn.SetEquation("log(b,R) - log(b,S)");
            rpn.Compute();
            if("b R S / log" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void LnAddOrSub()
        {
            RPN rpn = new RPN("ln(2) + ln(1/2)").Compute();
            if ("1 ln" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
            rpn.SetEquation("ln(2) - ln(3)").Compute();
            if("2 3 / ln" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ExpressionTimesDivision()
        {
            RPN rpn = new RPN("1(3/4)").Compute();
            if ("3 4 /" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void DivisionTimesDivision()
        {
            RPN rpn = new RPN("(3/4)(1/4)").Compute();
            if ("3 4 2 ^ /" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void DivisionFlip()
        {
            RPN rpn = new RPN("(5/x)/(x/3)").Compute();
            if ("5 3 * x 2 ^ /" != rpn.Polish.Print())
            {
                Assert.Fail();
            }
        }
    }
}