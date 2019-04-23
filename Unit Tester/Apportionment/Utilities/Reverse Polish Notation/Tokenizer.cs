using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class TokenizerTest
    {
        private RPN test;

        [OneTimeSetUp]
        public void StartUp()
        {
            test = new RPN("");
        }

        [Test]
        public void DebugMode()
        {
            Assert.IsFalse(test.Data.DebugMode);
        }

        [Test]
        public void UnaryFunction()
        {
            test.SetEquation("-pi");
            test.Compute();
            if ("-1 pi *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ComplexFunction()
        {
            test.SetEquation("sin(16pi)");
            test.Compute();
            if ("16 pi * sin" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ConstantFunction()
        {
            test.SetEquation("2e");
            test.Compute();
            if ("2 e *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ConstantFunctionRight()
        {
            test.SetEquation("pi(2)");
            test.Compute();
            if ("pi 2 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermMultiply()
        {
            test.SetEquation("(30.1)2.5(278)");
            test.Compute();

            if ("30.1 2.5 * 278 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableAdd()
        {
            test.SetEquation("2+x");
            test.Compute();

            if ("2 x +" != test.Polish.Print() && "2 1 x 1 ^ * +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void SimpleAdd()
        {
            test.SetEquation("2 + 2");
            test.Compute();

            if ("2 2 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermAdd()
        {
            test.SetEquation("2 + 2 + 2");
            test.Compute();

            if ("2 2 + 2 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermAddNoSpace()
        {
            test.SetEquation("2+2+2");
            test.Compute();

            if ("2 2 + 2 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void SimpleSubtract()
        {
            test.SetEquation("4 - 2");
            test.Compute();
            if ("4 2 -" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Wikipedia()
        {
            test.SetEquation("3 + 4 * 2 / ( 1 - 5 ) ^ 2 ^ 3");
            test.Compute();
            if ("3 4 2 * 1 5 - 2 3 ^ ^ / +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Functions()
        {
            test.SetEquation("sin ( max ( 2 , 3 ) / 3 * 3.1415 )");
            test.Compute();
            if ("2 3 max 3 / 3.1415 * sin" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Variables()
        {
            test.SetEquation("2 * x");
            test.Compute();
            if ("2 x *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void CompositeMax()
        {
            test.SetEquation("max(sqrt(16),100)");
            test.Compute();
            if ("16 sqrt 100 max" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableMultiplication()
        {
            test.SetEquation("v + a * t");
            test.Compute();
            if ("v a t * +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ArityConstantMax()
        {
            test.SetEquation("max(1, pi)");
            test.Compute();
            if ("1 pi max" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableExponents()
        {
            test.SetEquation("x^2");
            test.Compute();
            if ("x 2 ^" != test.Polish.Print() && "1 x 2 ^ *" != test.Polish.Print() && "1 x * 2 ^" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Aliasing()
        {
            test.SetEquation("4÷2");
            test.Compute();
            if ("4 2 /" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void UnaryStart()
        {
            test.SetEquation("-2 + 4");
            test.Compute();
            if ("-2 4 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ComplexExpression()
        {
            test.SetEquation("x >= 0 && x <= 5");
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
            test.SetEquation("1/2x");
            test.Compute();
            if ("1 2 x * /" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableContains()
        {
            test.SetEquation("x * 2");
            test.Compute();
            if ("x 2 *" != test.Polish.Print() && "1 x 1 ^ * 2 *" != test.Polish.Print() || test.Data.ContainsVariables == false)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void DoubleTokenize()
        {
            test.SetEquation("x * 2");
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
