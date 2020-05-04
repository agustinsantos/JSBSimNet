#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2006 Agustin Santos Mendez
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
#endregion

namespace JSBSim.Tests
{
	using System;

	using NUnit.Framework;

    // Import log4net classes.
    using log4net;

	using JSBSim;
	using CommonUtils.MathLib;
    using System.IO;

    /// <summary>
    /// Some JSBSim models run Tests.
    /// </summary>
    [TestFixture]
	public class RunTests
	{
        /// <summary>
        /// Define a static logger variable so that it references the
        ///	Logger instance.
        /// 
        /// NOTE that using System.Reflection.MethodBase.GetCurrentMethod().DeclaringType
        /// is equivalent to typeof(LoggingExample) but is more portable
        /// i.e. you can copy the code directly into another class without
        /// needing to edit the code.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private const double tolerance = 10E-4;

        private const String aircraft_MK82 = "mk82";
		private const String aircraft_Ball = "ball";

        private String AircraftPath = "../../../Models/aircraft";
        private String EnginePath = "../../../Models/engine";

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            var dir = TestContext.CurrentContext.TestDirectory;
            AircraftPath = Path.GetFullPath(dir + AircraftPath);
            EnginePath = Path.GetFullPath(dir + EnginePath);
        }

        [Test]
		public void CheckRun_MK82()
		{
            FDMExecutive exec = LoadAndRunModel(aircraft_MK82, "reset00");
			Assert.AreEqual("MK-82", exec.Aircraft.AircraftName);
            Assert.AreEqual(26.299999999999148, exec.State.SimTime, tolerance);
            Assert.AreEqual(757.97926252631783, exec.Auxiliary.VcalibratedFPS, tolerance);
            Assert.AreEqual(0.0019131959891723579, exec.Auxiliary.EarthPositionAngle, tolerance);
		}

		[Test]
		public void CheckRun_Ball()
		{
			FDMExecutive exec = LoadAndRunModel(aircraft_Ball, "reset00");
		
			Assert.AreEqual("BALL", exec.Aircraft.AircraftName);
		}

        private FDMExecutive LoadAndRunModel(string modelFileName, string IcFileName)
        {
            FDMExecutive fdm = new FDMExecutive();
            fdm.AircraftPath = AircraftPath;
            fdm.EnginePath = EnginePath;

            fdm.LoadModel(modelFileName, true);

            InitialCondition IC = fdm.GetIC;
            IC.Load(IcFileName, true);

            Trim fgt = new Trim(fdm, TrimMode.Full);
            if (!fgt.DoTrim())
            {
                log.Debug("Trim Failed");
            }
            fgt.Report();

            bool result = fdm.Run();
            int count = 0;
            while (result && !(fdm.Holding() || fdm.State.IsIntegrationSuspended) && count < 10000)
            {
                result = fdm.Run();
                count++;
                if (count > 120 && log.IsDebugEnabled)
                {
                    count = 0;
                    log.Debug("Time: " + fdm.State.SimTime);
                }
            }

            log.Debug("Final Time: " + fdm.State.SimTime);
            return fdm;
        }
	}
}
