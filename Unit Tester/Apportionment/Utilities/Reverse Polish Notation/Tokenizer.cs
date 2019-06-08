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
            RPN test = new RPN("-pi");
            test.Compute();
            if ("-1 pi *" != test.Polish.Print() && "pi -1 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ComplexFunction()
        {
            RPN test = new RPN("sin(16pi)");
            test.Compute();
            if ("16 pi * sin" != test.Polish.Print() && "pi 16 * sin" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ConstantFunction()
        {
            RPN test = new RPN("2e");
            test.Compute();
            if ("2 e *" != test.Polish.Print() && "e 2 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ConstantFunctionRight()
        {
            RPN test = new RPN("pi(2)");
            test.Compute();
            if ("pi 2 *" != test.Polish.Print() && "2 pi *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermMultiply()
        {
            RPN test = new RPN("(30.1)2.5(278)");
            test.Compute();

            if ("30.1 2.5 * 278 *" != test.Polish.Print() && "278 30.1 2.5 * *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableAdd()
        {
            RPN test = new RPN("2+x");
            test.Compute();

            if ("2 x +" != test.Polish.Print() && "2 1 x 1 ^ * +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void SimpleAdd()
        {
            RPN test = new RPN("3 + 2");
            test.Compute();

            if ("3 2 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermAdd()
        {
            RPN test = new RPN("2 + 3 + 2");
            test.Compute();

            if ("2 3 + 2 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermAddNoSpace()
        {
            RPN test = new RPN("2+3+2");
            test.Compute();

            if ("2 3 + 2 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void SimpleSubtract()
        {
            RPN test = new RPN("4 - 2");
            test.Compute();
            if ("4 2 -" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Wikipedia()
        {
            RPN test = new RPN("3 + 4 * 2 / ( 1 - 5 ) ^ 2 ^ 3");
            test.Compute();
            if ("3 4 2 * 1 5 - 2 3 ^ ^ / +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Functions()
        {
            RPN test = new RPN("sin ( max ( 2 , 3 ) / 3 * 3.1415 )");
            test.Compute();
            if ("2 3 max 3 / 3.1415 * sin" != test.Polish.Print() && "3.1415 2 3 max 3 / * sin" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Variables()
        {
            RPN test = new RPN("2 * x");
            test.Compute();
            if ("2 x *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void CompositeMax()
        {
            RPN test = new RPN("max(sqrt(16),100)");
            test.Compute();
            if ("16 sqrt 100 max" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableMultiplication()
        {
            RPN test = new RPN("v + a * t");
            test.Compute();
            if ("v a t * +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ArityConstantMax()
        {
            RPN test = new RPN("max(1, pi)");
            test.Compute();
            if ("1 pi max" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableExponents()
        {
            RPN test = new RPN("x^2");
            test.Compute();
            if ("x 2 ^" != test.Polish.Print() && "1 x 2 ^ *" != test.Polish.Print() && "1 x * 2 ^" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableChainMultiplication()
        {
            RPN test = new RPN("x2sin(x) + x3sin(x)");
            test.Compute();
            if ("x 2 * x sin * 3 x * x sin * +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Aliasing()
        {
            RPN test = new RPN("7÷2");
            test.Compute();
            if ("7 2 /" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void UnaryStart()
        {
            RPN test = new RPN("-2 + 4");
            test.Compute();
            if ("-2 4 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ComplexExpression()
        {
            RPN test = new RPN("x >= 0 && x <= 5");
            test.Logger += Write;
            test.Compute();
            
            if ("x 0 >= x 5 <= &&" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MixedDivisionMultiplication()
        {
            RPN test = new RPN("1/2x");
            test.Compute();
            if ("1 2 x * /" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableContains()
        {
            RPN test = new RPN("x * 2");
            test.Compute();
            if ("x 2 *" != test.Polish.Print() && "1 x 1 ^ * 2 *" != test.Polish.Print() || test.Data.ContainsVariables == false)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void DoubleTokenize()
        {
            RPN test = new RPN("x * 2");
            test.Compute();
            if ("x 2 *" != test.Polish.Print() || test.Data.ContainsVariables == false)
            {
                Assert.Fail();
            }

            test.SetEquation("2x + 2");
            test.Compute();
            if ("2 x * 2 +" != test.Polish.Print() && "2 2 x * +" != test.Polish.Print())
            {
                Assert.Fail();
            }

        }

        public void Write(object sender, string Event)
        {
            Console.WriteLine(Event);
        }
    }
}
