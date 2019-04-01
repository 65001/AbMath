﻿using System;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class PostFixTest
    {
        private RPN test;

        [OneTimeSetUp]
        public void StartUp()
        {
            test = new RPN("");
        }

        [Test]
        public void Add()
        {
            test.SetEquation("2 + 2 + 2");
            test.Compute();

            PostFix math = new PostFix(test.Data);
            Assert.AreEqual(6, math.Compute());
        }

        [Test]
        public void ComplexIncrement()
        {
            test.SetEquation("2++ + 2 + 2");
            test.Compute();

            PostFix math = new PostFix(test.Data);
            Assert.AreEqual(7, math.Compute());
        }

        [Test]
        public void Increment()
        {
            test.SetEquation("7++");
            test.Compute();

            PostFix math = new PostFix(test.Data);
            Assert.AreEqual(8, math.Compute());
        }

        [Test]
        public void Mod()
        {
            test.SetEquation("5 % 2");
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void UnarySubtract()
        {
            test.SetEquation("-2 + 4");
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(2, math.Compute());
        }

        [Test]
        public void UnarySubtract2()
        {
            test.SetEquation("5 + -2");
            
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(3, math.Compute());
        }

        [Test]
        public void UnaryDecimal()
        {
            test.SetEquation("-.5 + .5");

            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void UnaryEOS()
        {
            test.SetEquation("-.5 + -.5");

            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(-1, math.Compute());

            test.SetEquation("-.5 + -.5 ");
            test.Compute();
            math = new PostFix(test);
            Assert.AreEqual(-1, math.Compute());
        }

        [Test]
        public void Sin()
        {
            test.SetEquation("sin(pi/2)");
            
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void SinOfe()
        {
            test.SetEquation("sin(e/2)");
            
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(Math.Sin(Math.E/2), math.Compute());
        }

        [Test]
        public void Cos()
        {
            test.SetEquation("cos(pi)");
            
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(-1, math.Compute());
        }

        [Test]
        public void Functions()
        {
            test.SetEquation("sin( max( 2 , 3 )/3 * 3.1415 )");
            
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(Math.Sin(3.1415), math.Compute());
        }

        [Test]
        public void CompositeFunctions()
        {
            test.SetEquation("max( sqrt( 16 ) , 100)");
            
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(100, math.Compute());
        }

        [Test]
        public void Factorial()
        {
            test.SetEquation("5!");
            
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(120, math.Compute());
        }

        [Test]
        public void Round()
        {
            test.SetEquation("round(pi,2)");
            
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(3.14, math.Compute());
        }

        [Test]
        public void NotEqual()
        {
            test.SetEquation("5 != 2");
            
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void Log()
        {
            test.SetEquation("log(16,2)");
            
            test.Compute();
            PostFix math = new PostFix(test);
            Assert.AreEqual(4, math.Compute());
        }

        [Test]
        public void Reset()
        {
            test.SetEquation("x^2");
            
            test.Compute();
            
            PostFix math = new PostFix(test);
            math.SetVariable("x", 2);
            Assert.AreEqual(4, math.Compute());

            math.Reset();
            math.SetVariable("x", 3);
            Assert.AreEqual(9, math.Compute());

            math.Reset();
            math.SetVariable("x", 4);
            Assert.AreEqual(16, math.Compute());
        }

        [Test]
        public void ComplexReset()
        {
            test.SetEquation("x^2");
            
            test.Compute();

            PostFix math = new PostFix(test);
            math.SetVariable("x", 2);
            Assert.AreEqual(4, math.Compute());

            test.SetEquation("2x");
            test.Compute();

            math = new PostFix(test);
            math.SetVariable("x", 3);
            Assert.AreEqual(6, math.Compute());

        }

        [Test]
        public void VardiacMax()
        {
            test.SetEquation("max(1, 2, 3)");

            
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(3, math.Compute());
        }

        [Test]
        public void VardiacMin()
        {
            test.SetEquation("min(1, 2, 3)");

            
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void VardiacComposite()
        {
            test.SetEquation("sin(min (0, 1) )");

            
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void Max()
        {
            test.SetEquation("max(0, 1)");
            
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(1, math.Compute());
        }

        [Test]
        public void Min()
        {
            test.SetEquation("min(0, 1)");
            
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(0, math.Compute());
        }

        [Test]
        public void Arcsin()
        {
            test.SetEquation("arcsin( sin(pi/2) )");
            
            test.Compute();

            PostFix math = new PostFix(test);
            Assert.AreEqual(Math.PI / 2, math.Compute());
        }

        [Test]
        public void SwapStackOverflow()
        {
            test.SetEquation("x^p + x - x^p - x + x^2 + x - x^2 - x");
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
