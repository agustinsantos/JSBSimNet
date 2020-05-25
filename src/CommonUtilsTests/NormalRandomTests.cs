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
    /// // Check that the order statistics of the normal distribution are distributed as expected.
    /// </summary>
    [TestFixture]
    public class NormalRandomTests
    {
        public const double tolerance = 0.5;
        public const int maxNumbers = 10000;

        [Test]
        public void CheckOrderStatistics01()
        {
            double mean = 0;
            double stdDev = 1;

            NormalRandom rand = new NormalRandom(mean, stdDev, new Random());

            double[] values = new double[maxNumbers];
            for (int i = 0; i < maxNumbers; i++)
            {
                values[i] = rand.Next();
            }

            double average = values.Average();
            Assert.AreEqual(mean, average, tolerance);
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / values.Length);
            Assert.AreEqual(stdDev, sd, tolerance);
        }

        [Test]
        public void CheckOrderStatistics02()
        {
            double mean = -10;
            double stdDev = 0.5;

            NormalRandom rand = new NormalRandom(mean, stdDev, new Random());

            double[] values = new double[maxNumbers];
            for (int i = 0; i < maxNumbers; i++)
            {
                values[i] = rand.Next();
            }

            double average = values.Average();
            Assert.AreEqual(mean, average, tolerance * 10);
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / values.Length);
            Assert.AreEqual(stdDev, sd, tolerance);
        }

        [Test]
        public void CheckOrderStatistics03()
        {
            double mean = 100;
            double stdDev = 25;

            NormalRandom rand = new NormalRandom(mean, stdDev, new Random());

            double[] values = new double[maxNumbers];
            for (int i = 0; i < maxNumbers; i++)
            {
                values[i] = rand.Next();
            }

            double average = values.Average();
            Assert.AreEqual(mean, average, tolerance * mean);
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / values.Length);
            Assert.AreEqual(stdDev, sd, tolerance);
        }

    }
}
