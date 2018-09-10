using System;
using AbMath.Utilities;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class TokenizerTest
    {
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
        public void MultiTermMultiply()
        {
            RPN Test = new RPN("(30.1)2.5(278)");
            Test.Logger += Write;
            Test.Compute();

            if ("30.1 2.5 * 278 *" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableAdd()
        {
            RPN Test = new RPN("2+x");
            Test.Compute();

            if ("2 x +" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void SimpleAdd()
        {
            RPN Test = new RPN("2 + 2");
            Test.Logger += Write;
            Test.Compute();

            if ("2 2 +" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermAdd()
        {
            RPN Test = new RPN("2 + 2 + 2");
            Test.Compute();

            if ("2 2 + 2 +" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void MultiTermAddNoSpace()
        {
            RPN Test = new RPN("2+2+2");
            Test.Compute();

            if ("2 2 + 2 +" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void SimpleSubtract()
        {
            RPN Test = new RPN("4 - 2");
            Test.Compute();
            if ("4 2 -" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Wikipedia()
        {
            RPN Test = new RPN("3 + 4 * 2 / ( 1 - 5 ) ^ 2 ^ 3");
            Test.Logger += Write;
            Test.Compute();
            Console.WriteLine(Test.Polish.Print());
            if ("3 4 2 * 1 5 - 2 3 ^ ^ / +" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Functions()
        {
            RPN Test = new RPN("sin ( max ( 2 , 3 ) / 3 * 3.1415 )");
            Test.Logger += Write;
            Test.Compute();
            Console.WriteLine(Test.Polish.Print());
            if ("2 3 max 3 / 3.1415 * sin" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Variables()
        {
            RPN Test = new RPN("2 * x");
            Test.Logger += Write;
            Test.Compute();
            Console.WriteLine(Test.Polish.Print());
            if ("2 x *" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void CompositeMax()
        {
            RPN Test = new RPN("max(sqrt(16),100)");
            Test.Logger += Write;
            Test.Compute();
            Console.WriteLine(Test.Polish.Print());
            if ("16 sqrt 100 max" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableMultiplication()
        {
            RPN Test = new RPN("v + a * t");
            Test.Logger += Write;
            Test.Compute();
            Console.WriteLine(Test.Polish.Print());
            if ("v a t * +" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableExponents()
        {
            RPN Test = new RPN("x^2");
            Test.Logger += Write;
            Test.Compute();
            Console.WriteLine(Test.Polish.Print());
            if ("x 2 ^" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void Aliasing()
        {
            RPN Test = new RPN("4÷2");
            Test.Logger += Write;
            Test.Compute();
            Console.WriteLine(Test.Polish.Print());
            if ("4 2 /" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void UniaryStart()
        {
            RPN Test = new RPN("-2 + 4");
            Test.Logger += Write;
            Test.Compute();
            if ("-2 4 +" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [Test]
        public void VariableContains()
        {
            RPN Test = new RPN("x * 2");
            Test.Logger += Write;
            Test.Compute();
            if ("x 2 *" != Test.Polish.Print() || Test.data.ContainsVariables == false)
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
