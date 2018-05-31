using System;
using AbMath.Utilities;
using System.Collections.Generic;
using NUnit.Framework;

namespace TestHarness
{
    [TestFixture]
    public class PostFixTest
    {
        [Test]
        public void Add()
        {
            RPN Test = new RPN("2 + 2 + 2");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test.data);
            Assert.AreEqual(6, Math.Compute());
        }

        [Test]
        public void Mod()
        {
            RPN Test = new RPN("5 % 2");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(1, Math.Compute());
        }

        [Test]
        public void UniarySubtract()
        {
            RPN Test = new RPN("-2 + 4");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(2, Math.Compute());
        }

        [Test]
        public void UniarySubtract2()
        {
            RPN Test = new RPN("5 + -2");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(3, Math.Compute());
        }

        [Test]
        public void Sin()
        {
            RPN Test = new RPN("sin(pi/2)");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(1, Math.Compute());
        }

        [Test]
        public void Cos()
        {
            RPN Test = new RPN("cos(pi)");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(-1, Math.Compute());
        }

        [Test]
        public void Functions()
        {
            RPN Test = new RPN("sin( max( 2 , 3 )/3 * 3.1415 )");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(System.Math.Sin(3.1415), Math.Compute());
        }

        [Test]
        public void CompositeFunctions()
        {
            RPN Test = new RPN("max( sqrt( 16 ) , 100)");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(100, Math.Compute());
        }

        [Test]
        public void Factorial()
        {
            RPN Test = new RPN("5!");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(120, Math.Compute());
        }

        [Test]
        public void Round()
        {
            RPN Test = new RPN("round(pi,2)");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(3.14, Math.Compute());
        }

        [Test]
        public void NotEqual()
        {
            RPN Test = new RPN("5 != 2");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(1, Math.Compute());
        }

        [Test]
        public void Log()
        {
            RPN Test = new RPN("log(16,2)");
            Test.Logger += Write;
            Test.Compute();
            PostFix Math = new PostFix(Test);
            Assert.AreEqual(4, Math.Compute());
        }

        [Test]
        public void Reset()
        {
            RPN Test = new RPN("x^2");
            Test.Logger += Write;
            Test.Compute();
            
            PostFix Math = new PostFix(Test);
            Math.SetVariable("x", "2");
            Assert.AreEqual(4, Math.Compute());
            Math.Reset();
            Math.SetVariable("x", "3");
            Assert.AreEqual(9, Math.Compute());
        }

        public void Write(object sender,string Event)
        {
            Console.WriteLine(Event);
        }
    }
}
