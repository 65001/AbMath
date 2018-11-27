using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class PostFixTest
    {
        [Test]
        public void Add()
        {
            RPN test = new RPN("2 + 2 + 2");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test.Data);
            Assert.AreEqual(6, math.Compute());
        }

        [Test]
        public void ComplexIncrement()
        {
            RPN test = new RPN("2++ + 2 + 2");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test.Data);
            Assert.AreEqual(7, math.Compute());
        }

        [Test]
        public void Increment()
        {
            RPN test = new RPN("7++");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test.Data);
            Assert.AreEqual(8, math.Compute());
        }

        [Test]
        public void Mod()
        {
            RPN test = new RPN("5 % 2");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void UniarySubtract()
        {
            RPN test = new RPN("-2 + 4");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(2, math.Compute());
        }

        [Test]
        public void UnarySubtract2()
        {
            RPN test = new RPN("5 + -2");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(3, math.Compute());
        }

        [Test]
        public void Sin()
        {
            RPN test = new RPN("sin(pi/2)");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void SinOfe()
        {
            RPN test = new RPN("sin(e/2)");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(Math.Sin(Math.E/2), math.Compute());
        }

        [Test]
        public void Cos()
        {
            RPN test = new RPN("cos(pi)");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(-1, math.Compute());
        }

        [Test]
        public void Functions()
        {
            RPN test = new RPN("sin( max( 2 , 3 )/3 * 3.1415 )");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(Math.Sin(3.1415), math.Compute());
        }

        [Test]
        public void CompositeFunctions()
        {
            RPN test = new RPN("max( sqrt( 16 ) , 100)");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(100, math.Compute());
        }

        [Test]
        public void Factorial()
        {
            RPN test = new RPN("5!");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(120, math.Compute());
        }

        [Test]
        public void Round()
        {
            RPN test = new RPN("round(pi,2)");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(3.14, math.Compute());
        }

        [Test]
        public void NotEqual()
        {
            RPN test = new RPN("5 != 2");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void Log()
        {
            RPN test = new RPN("log(16,2)");
            test.Logger += Write;
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(4, math.Compute());
        }

        [Test]
        public void Reset()
        {
            RPN test = new RPN("x^2");
            test.Logger += Write;
            test.Compute();
            
            PostFix math = new PostFix(test);
            math.SetVariable("x", "2");
            Assert.AreEqual(4, math.Compute());

            math.Reset();
            math.SetVariable("x", "3");
            Assert.AreEqual(9, math.Compute());

            math.Reset();
            math.SetVariable("x", "4");
            Assert.AreEqual(16, math.Compute());
        }

        [Test]
        public void ComplexReset()
        {
            RPN test = new RPN("x^2");
            test.Logger += Write;
            test.Compute();

            PostFix math = new PostFix(test);
            math.SetVariable("x", "2");
            Assert.AreEqual(4, math.Compute());

            test.SetEquation("2x");
            test.Compute();

            math = new PostFix(test);
            math.SetVariable("x", "3");
            Assert.AreEqual(6, math.Compute());

        }

        [Test]
        public void VardiacMax()
        {
            RPN test = new RPN("max(1, 2, 3)");

            test.Logger += Write;
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(3, math.Compute());
        }

        [Test]
        public void VardiacMin()
        {
            RPN test = new RPN("min(1, 2, 3)");

            test.Logger += Write;
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void VardiacComposite()
        {
            RPN test = new RPN("sin(min (0, 1) )");

            test.Logger += Write;
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void Max()
        {
            RPN test = new RPN("max(0, 1)");
            test.Logger += Write;
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void Min()
        {
            RPN test = new RPN("min(0, 1)");
            test.Logger += Write;
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        public void Write(object sender,string Event)
        {
            Console.WriteLine(Event);
        }
    }
}
