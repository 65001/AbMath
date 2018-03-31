using System;
using AbMath.Discrete.Apportionment;
using AbMath.Utilities;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestHarness
{
    [TestClass]
    public class RPNTest
    {
        [TestMethod]
        public void VariableAdd()
        {
            RPN Test = new RPN("2+x");
            Test.Compute();

            if ("2 x +" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [TestMethod]
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

        [TestMethod]
        public void MultiTermAdd()
        {
            RPN Test = new RPN("2 + 2 + 2");
            Test.Compute();

            if ("2 2 + 2 +" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void SimpleSubtract()
        {
            RPN Test = new RPN("4 - 2");
            Test.Compute();
            if ("4 2 -" != Test.Polish.Print())
            {
                Assert.Fail();
            }
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        public void Write(object sender, string Event)
        {
            Console.WriteLine(Event);
        }
    }
}
