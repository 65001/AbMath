using System;
using AbMath.Utilities;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class TokenizerTest
    {

        [Test]
        public void UnaryFunction()
        {
            RPN test = new RPN("-pi");
            test.Logger += Write;
            test.Compute();
            if ("-1 pi *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ComplexFunction()
        {
            RPN test = new RPN("sin(16pi)");
            test.Logger += Write;
            test.Compute();
            if ("16 pi * sin" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void ConstantFunction()
        {
            RPN test = new RPN("2e");
            test.Logger += Write;
            test.Compute();
            if ("2 e *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermMultiply()
        {
            RPN test = new RPN("(30.1)2.5(278)");
            test.Logger += Write;
            test.Compute();

            if ("30.1 2.5 * 278 *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableAdd()
        {
            RPN test = new RPN("2+x");
            test.Compute();

            if ("2 x +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void SimpleAdd()
        {
            RPN test = new RPN("2 + 2");
            test.Logger += Write;
            test.Compute();

            if ("2 2 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermAdd()
        {
            RPN test = new RPN("2 + 2 + 2");
            test.Compute();

            if ("2 2 + 2 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermAddNoSpace()
        {
            RPN test = new RPN("2+2+2");
            test.Compute();

            if ("2 2 + 2 +" != test.Polish.Print())
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
            test.Logger += Write;
            test.Compute();
            Console.WriteLine(test.Polish.Print());
            if ("3 4 2 * 1 5 - 2 3 ^ ^ / +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Functions()
        {
            RPN test = new RPN("sin ( max ( 2 , 3 ) / 3 * 3.1415 )");
            test.Logger += Write;
            test.Compute();
            Console.WriteLine(test.Polish.Print());
            if ("2 3 max 3 / 3.1415 * sin" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Variables()
        {
            RPN test = new RPN("2 * x");
            test.Logger += Write;
            test.Compute();
            Console.WriteLine(test.Polish.Print());
            if ("2 x *" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void CompositeMax()
        {
            RPN test = new RPN("max(sqrt(16),100)");
            test.Logger += Write;
            test.Compute();
            Console.WriteLine(test.Polish.Print());
            if ("16 sqrt 100 max" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableMultiplication()
        {
            RPN test = new RPN("v + a * t");
            test.Logger += Write;
            test.Compute();
            Console.WriteLine(test.Polish.Print());
            if ("v a t * +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableExponents()
        {
            RPN test = new RPN("x^2");
            test.Logger += Write;
            test.Compute();
            Console.WriteLine(test.Polish.Print());
            if ("x 2 ^" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Aliasing()
        {
            RPN test = new RPN("4÷2");
            test.Logger += Write;
            test.Compute();
            Console.WriteLine(test.Polish.Print());
            if ("4 2 /" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void UnaryStart()
        {
            RPN test = new RPN("-2 + 4");
            test.Logger += Write;
            test.Compute();
            if ("-2 4 +" != test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableContains()
        {
            RPN test = new RPN("x * 2");
            test.Logger += Write;
            test.Compute();
            if ("x 2 *" != test.Polish.Print() || test.data.ContainsVariables == false)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void DoubleTokenize()
        {
            RPN test = new RPN("x * 2");
            test.Logger += Write;
            test.Compute();
            if ("x 2 *" != test.Polish.Print() || test.data.ContainsVariables == false)
            {
                Assert.Fail();
            }

            test.SetEquation("2x + 2");
            test.Compute();
            if ("2 x * 2 +" != test.Polish.Print())
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
