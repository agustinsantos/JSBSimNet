#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2006 Agustin Santos Mendez
/// 
/// JSBSim was developed by Jon S. Berndt, Tony Peden, and
/// David Megginson. 
/// Agustin Santos Mendez implemented and maintains this C# version.
/// 
/// This program is free software; you can redistribute it and/or
///  modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///  
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/// GNU General Public License for more details.
///  
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
#endregion
namespace CommonUtils.Tests
{
    using System;

    using NUnit.Framework;

    using CommonUtils.MathLib;
    using System.Linq;

    /// <summary>
    /// // Check that the order statistics of the uniform distribution are distributed as expected.
    /// </summary>
    [TestFixture]
    public class UniformRandomTests
    {
        public const double tolerance = 0.1;
        public const int maxNumbers = 10000;

        [Test]
        public void CheckOrderStatistics01()
        {
            double lower = 0;
            double upper = 1;

            UniformRandom rand = new UniformRandom(lower, upper, new Random());

            double[] values = new double[maxNumbers];
            for (int i = 0; i < maxNumbers; i++)
            {
                values[i] = rand.Next();
                Assert.LessOrEqual(lower, values[i], "The value should be higher than lower");
                Assert.GreaterOrEqual(upper, values[i], "The value should be lower than higher");
            }

            double average = values.Average();
            Assert.AreEqual((lower + upper) / 2, average, tolerance);
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double variance = sumOfSquaresOfDifferences / values.Length ;
            Assert.AreEqual((upper - lower) * (upper - lower) / 12, variance, tolerance);
        }

        [Test]
        public void CheckOrderStatistics02()
        {
            double lower = -10;
            double upper = 10;

            UniformRandom rand = new UniformRandom(lower, upper, new Random());

            double[] values = new double[maxNumbers];
            for (int i = 0; i < maxNumbers; i++)
            {
                values[i] = rand.Next();
                Assert.LessOrEqual(lower, values[i], "The value should be higher than lower");
                Assert.GreaterOrEqual(upper, values[i], "The value should be lower than higher");
            }

            double average = values.Average();
            Assert.AreEqual((lower + upper) / 2, average, tolerance);
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double variance = sumOfSquaresOfDifferences / values.Length;
            Assert.AreEqual((upper - lower) * (upper - lower) / 12, variance, tolerance);
        }

        [Test]
        public void CheckOrderStatistics03()
        {
            double lower = -10;
            double upper = 0;

            UniformRandom rand = new UniformRandom(lower, upper, new Random());

            double[] values = new double[maxNumbers];
            for (int i = 0; i < maxNumbers; i++)
            {
                values[i] = rand.Next();
                Assert.LessOrEqual(lower, values[i], "The value should be higher than lower");
                Assert.GreaterOrEqual(upper, values[i], "The value should be lower than higher");
            }

            double average = values.Average();
            Assert.AreEqual((lower + upper) / 2, average, tolerance);
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double variance = sumOfSquaresOfDifferences / values.Length;
            Assert.AreEqual((upper - lower) * (upper - lower) / 12, variance, tolerance);
        }

    }
}
