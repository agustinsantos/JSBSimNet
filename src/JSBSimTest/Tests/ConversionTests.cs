#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2020 Agustin Santos Mendez
/// 
/// JSBSim was developed by Jon S. Berndt, Tony Peden, and
/// David Megginson. 
/// Agustin Santos Mendez implemented and maintains this C# version.
/// 
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
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
/// 
/// Further information about the GNU Lesser General Public License can also be found on
/// the world wide web at http://www.gnu.org.
#endregion

namespace JSBSim.Tests
{
    using CommonUtils.MathLib;
    using NUnit.Framework;

    /// <summary>
    /// Test units conversions
    /// </summary>
    [TestFixture]
    public class ConversionTests
    {
        private const double tolerance = 10E-16;

        [Test]
        public void TestCASConversion()
        {
            double p = 2116.228;
            Assert.AreEqual(0.0, Conversion.VcalibratedFromMach(-0.1, p));
            Assert.AreEqual(0.0, Conversion.VcalibratedFromMach(0, p));
            Assert.AreEqual(558.2243, Conversion.VcalibratedFromMach(0.5, p), 1E-4);
            Assert.AreEqual(1116.4486, Conversion.VcalibratedFromMach(1.0, p), 1E-4);
            Assert.AreEqual(1674.6728, Conversion.VcalibratedFromMach(1.5, p), 1E-4);
            Assert.AreEqual(0.0, Conversion.MachFromVcalibrated(0.0, p));
            Assert.AreEqual(0.5, Conversion.MachFromVcalibrated(558.2243, p), 1E-4);
            Assert.AreEqual(1.0, Conversion.MachFromVcalibrated(1116.4486, p), 1E-4);
            Assert.AreEqual(1.5, Conversion.MachFromVcalibrated(1674.6728, p), 1E-4);
        }

        [Test]
        public void TestTemperatureConversion()
        {
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.KelvinToFahrenheit(0.0), -459.4));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.KelvinToFahrenheit(288.15), 59.27));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.CelsiusToRankine(0.0), 491.67));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.CelsiusToRankine(15.0), 518.67));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.RankineToCelsius(491.67), 0.0));
            Assert.AreEqual(Conversion.RankineToCelsius(518.67), 15.0, 1E-8);
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.KelvinToRankine(0.0), 0.0));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.KelvinToRankine(288.15), 518.67));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.RankineToKelvin(0.0), 0.0));
            Assert.AreEqual(Conversion.RankineToKelvin(518.67), 288.15, 1E-8);
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.CelsiusToFahrenheit(0.0), 32.0));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.CelsiusToFahrenheit(15.0), 59.0));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.FahrenheitToCelsius(32.0), 0.0));
            Assert.AreEqual(Conversion.FahrenheitToCelsius(59.0), 15.0, 1E-8);
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.KelvinToCelsius(0.0), -273.15));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.KelvinToCelsius(288.15), 15.0));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.CelsiusToKelvin(-273.15), 0.0));
            Assert.IsTrue(MathExt.EqualToRoundoff(Conversion.CelsiusToKelvin(15.0), 288.15));
        }
    }
}
