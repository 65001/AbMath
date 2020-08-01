using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace AbMath.Tests
{
    [TestFixture]
    public class SubtractionTest
    {
        [Test]
        public void DistributiveSimple()
        {
            RPN rpn = new RPN("f - (g - h)").Compute();
            Assert.AreEqual("f h + g -", rpn.Polish.Print());
        }
    }
}
