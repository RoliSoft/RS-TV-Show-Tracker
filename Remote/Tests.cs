namespace RoliSoft.TVShowTracker.Remote.Tests
{
    using System;
    using System.Linq;

    using NUnit.Framework;

    using RoliSoft.TVShowTracker.Remote.Objects;

    /// <summary>
    /// Tests the method proxy for the lab.rolisoft.net API.
    /// </summary>
    public class ArithmeticTests
    {
        /// <summary>
        /// Tests the <c>Add()</c> method proxy by calling it with two random numbers.
        /// </summary>
        [Test]
        public void Add()
        {
            var rnd = new Random();
            var i1  = rnd.NextDouble();
            var i2  = rnd.NextDouble();

            var res = API.Add(i1, i2);

            if (!string.IsNullOrWhiteSpace(res.Error))
            {
                Assert.Fail("The API call failed with the following internal error: " + res.Error);
            }

            Assert.AreEqual(i1 + i2, res.Result, 0.0000000001);
        }

        /// <summary>
        /// Tests the <c>Mean()</c> method proxy by calling it with five random numbers.
        /// </summary>
        [Test]
        public void Mean()
        {
            var rnd  = new Random();
            var nums = new[] { rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble() };

            var res = API.Mean(nums);

            if (!string.IsNullOrWhiteSpace(res.Error))
            {
                Assert.Fail("The API call failed with the following internal error: " + res.Error);
            }

            Assert.AreEqual(nums.Sum() / nums.Count(), res.Result, 0.0000000001);
        }

        /// <summary>
        /// Tests the <c>Add()</c> encrypted method proxy by calling it with two random numbers.
        /// </summary>
        [Test]
        public void AddSecure()
        {
            var rnd = new Random();
            var i1 = rnd.NextDouble();
            var i2 = rnd.NextDouble();

            var res = API.InvokeSecureRemoteMethod<Generic<double>>("Add", i1, i2);

            if (!string.IsNullOrWhiteSpace(res.Error))
            {
                Assert.Fail("The API call failed with the following internal error: " + res.Error);
            }

            Assert.AreEqual(i1 + i2, res.Result, 0.0000000001);
        }

        /// <summary>
        /// Tests the <c>Mean()</c> encrypted method proxy by calling it with five random numbers.
        /// </summary>
        [Test]
        public void MeanSecure()
        {
            var rnd = new Random();
            var nums = new[] { rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble() };

            var res = API.InvokeSecureRemoteMethod<Generic<double>>("Mean", nums);

            if (!string.IsNullOrWhiteSpace(res.Error))
            {
                Assert.Fail("The API call failed with the following internal error: " + res.Error);
            }

            Assert.AreEqual(nums.Sum() / nums.Count(), res.Result, 0.0000000001);
        }
    }
}
