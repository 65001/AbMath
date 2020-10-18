using System;
using System.Collections.Generic;
using System.Text;
using AbMath.Calculator;
using NUnit.Framework;

namespace AbMath.Tests
{
    [TestFixture]
    public class List
    {
        [Test]
        public void SingleElement()
        {
            RPN test = new RPN("5{3}").Compute();
            Assert.AreEqual("5 3 *", test.Polish.Print());
        }
    }
}
