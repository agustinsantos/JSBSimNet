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

    using JSBSim;
    using CommonUtils.MathLib;
    using System.IO;

    /// <summary>
    /// Some JSBSim  models, engine and scripts file load Tests.
    /// </summary>
    [TestFixture]
    public class LoadTests
    {
        private const String aircraft_737 = "737";
        private const String aircraft_MK82 = "mk82";
        private const String aircraft_F16 = "f16";
        private const String aircraft_747 = "747";
        private const String aircraft_p51d = "p51d";
        private const String aircraft_A320 = "A320";
        private const String aircraft_ball = "ball";
        private const String aircraft_c172x = "c172x";

        // Models directory is just at the same level as the code
        // Visual studio generate the code at bin/Debug
        private String AircraftPath = "../../../Models/aircraft";
        private String EnginePath = "../../../Models/engine";

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            var dir = TestContext.CurrentContext.TestDirectory; // Path.GetDirectoryName(typeof(FDMExecutive).Assembly.Location);
            AircraftPath = Path.GetFullPath(dir + AircraftPath);
            EnginePath = Path.GetFullPath(dir + EnginePath);
        }

        [Test]
        public void CheckLoad_Ball()
        {
            FDMExecutive fdm = new FDMExecutive();

            fdm.LoadModel(AircraftPath, EnginePath, null, aircraft_ball, true);

            Assert.AreEqual(aircraft_ball, fdm.ModelName);
            Assert.AreEqual("BALL", fdm.Aircraft.AircraftName);
            Assert.AreEqual(1.0, fdm.Aircraft.WingArea);
            Assert.AreEqual(1, fdm.Aircraft.WingSpan);
            Assert.AreEqual(0, fdm.Aircraft.WingChord);
            Assert.AreEqual(0.0, fdm.Aircraft.HTailArea);
            Assert.AreEqual(0, fdm.Aircraft.HTailArm);
            Assert.AreEqual(0, fdm.Aircraft.VTailArea);
            Assert.AreEqual(0, fdm.Aircraft.VTailArm);
            Assert.AreEqual(new Vector3D(0, 0, 0), fdm.Aircraft.EyepointXYZ);
            Assert.AreEqual(new Vector3D(0, 0, 0), fdm.Aircraft.AeroRefPointXYZ);
            Assert.AreEqual(new Vector3D(0.0, 0.0, 0.0), fdm.Aircraft.VisualRefPointXYZ);
        }

        [Test]
        public void CheckLoad_737()
        {
            FDMExecutive fdm = new FDMExecutive();

            fdm.LoadModel(AircraftPath, EnginePath, null, aircraft_737, true);

            Assert.AreEqual(aircraft_737, fdm.ModelName);
            Assert.AreEqual("737", fdm.Aircraft.AircraftName);
            Assert.AreEqual(1171.0, fdm.Aircraft.WingArea);
            Assert.AreEqual(94.7, fdm.Aircraft.WingSpan);
            Assert.AreEqual(12.31, fdm.Aircraft.WingChord);
            Assert.AreEqual(348.0, fdm.Aircraft.HTailArea);
            Assert.AreEqual(48.04, fdm.Aircraft.HTailArm);
            Assert.AreEqual(297.00, fdm.Aircraft.VTailArea);
            Assert.AreEqual(44.50, fdm.Aircraft.VTailArm);
            Assert.AreEqual(new Vector3D(80.0, -30.0, 70.0), fdm.Aircraft.EyepointXYZ);
            Assert.AreEqual(new Vector3D(625.0, 0.0, 24.0), fdm.Aircraft.AeroRefPointXYZ);
            Assert.AreEqual(new Vector3D(0.0, 0.0, 0.0), fdm.Aircraft.VisualRefPointXYZ);
        }

        [Test]
        public void CheckLoad_747()
        {
            FDMExecutive fdm = new FDMExecutive();
            fdm.LoadModel(AircraftPath, EnginePath, null, aircraft_747, true);

            Assert.AreEqual(aircraft_747, fdm.ModelName);
            Assert.AreEqual("1970 Boeing 747 100B", fdm.Aircraft.AircraftName);
            Assert.AreEqual(5500.0, fdm.Aircraft.WingArea);
            Assert.AreEqual(195.7, fdm.Aircraft.WingSpan);
            Assert.AreEqual(27.3, fdm.Aircraft.WingChord);
            Assert.AreEqual(1470.0, fdm.Aircraft.HTailArea);
            Assert.AreEqual(99.0, fdm.Aircraft.HTailArm);
            Assert.AreEqual(830.0, fdm.Aircraft.VTailArea);
            Assert.AreEqual(0.0, fdm.Aircraft.VTailArm);
            Assert.AreEqual(new Vector3D(236.0, -40.0, 118.0), fdm.Aircraft.EyepointXYZ);
            Assert.AreEqual(new Vector3D(1340.0, 0.0, -39.0), fdm.Aircraft.AeroRefPointXYZ);
            Assert.AreEqual(new Vector3D(1340.0, 0.0, -39.0), fdm.Aircraft.VisualRefPointXYZ);
        }

        [Test]
        public void CheckLoad_f16()
        {
            FDMExecutive fdm = new FDMExecutive();
            fdm.LoadModel(AircraftPath, EnginePath, null, aircraft_F16, true);

            /*
             <metrics>
                <wingarea unit="FT2"> 300 </wingarea>
                <wingspan unit="FT"> 30 </wingspan>
                <chord unit="FT"> 11.32 </chord>
                <htailarea unit="FT2"> 63.7 </htailarea>
                <htailarm unit="FT"> 16.46 </htailarm>
                <vtailarea unit="FT2"> 54.75 </vtailarea>
                <vtailarm unit="FT"> 0 </vtailarm>
                <location name="AERORP" unit="IN">
                    <x> -189.5 </x>
                    <y> 0 </y>
                    <z> 3.9 </z>
                </location>
                <location name="EYEPOINT" unit="IN">
                    <x> -336.2 </x>
                    <y> 0 </y>
                    <z> 29.5 </z>
                </location>
                <location name="VRP" unit="IN">
                    <x> 0 </x>
                    <y> 0 </y>
                    <z> 0 </z>
                </location>
            </metrics>
             */
            Assert.AreEqual(aircraft_F16, fdm.ModelName);
            Assert.AreEqual("General Dynamics F-16A", fdm.Aircraft.AircraftName);
            Assert.AreEqual(300, fdm.Aircraft.WingArea);
            Assert.AreEqual(30, fdm.Aircraft.WingSpan);
            Assert.AreEqual(11.32, fdm.Aircraft.WingChord);
            Assert.AreEqual(63.7, fdm.Aircraft.HTailArea);
            Assert.AreEqual(16.46, fdm.Aircraft.HTailArm);
            Assert.AreEqual(54.75, fdm.Aircraft.VTailArea);
            Assert.AreEqual(0.0, fdm.Aircraft.VTailArm);
            Assert.AreEqual(new Vector3D(-336.2, 0.0, 29.5), fdm.Aircraft.EyepointXYZ);
            Assert.AreEqual(new Vector3D(-189.5, 0.0, 3.9), fdm.Aircraft.AeroRefPointXYZ);
            Assert.AreEqual(new Vector3D(0, 0, 0), fdm.Aircraft.VisualRefPointXYZ);
        }

        [Test]
        public void CheckLoad_p51d()
        {
            FDMExecutive fdm = new FDMExecutive();
            fdm.LoadModel(AircraftPath, EnginePath, null, aircraft_p51d, true);

            Assert.AreEqual(aircraft_p51d, fdm.ModelName);
            Assert.AreEqual("P51D", fdm.Aircraft.AircraftName);
            Assert.AreEqual(240, fdm.Aircraft.WingArea);
            Assert.AreEqual(37.1, fdm.Aircraft.WingSpan);
            Assert.AreEqual(6.6, fdm.Aircraft.WingChord);
            Assert.AreEqual(41, fdm.Aircraft.HTailArea);
            Assert.AreEqual(15, fdm.Aircraft.HTailArm);
            Assert.AreEqual(20, fdm.Aircraft.VTailArea);
            Assert.AreEqual(0.0, fdm.Aircraft.VTailArm);
            Assert.AreEqual(new Vector3D(128, 0.0, 30), fdm.Aircraft.EyepointXYZ);
            Assert.AreEqual(new Vector3D(99, 0.0, -26.5), fdm.Aircraft.AeroRefPointXYZ);
            Assert.AreEqual(new Vector3D(98, 0, -9), fdm.Aircraft.VisualRefPointXYZ);
        }

        [Test]
        public void CheckLoad_A320()
        {
            FDMExecutive fdm = new FDMExecutive();
            fdm.LoadModel(AircraftPath, EnginePath, null, aircraft_A320, true);

            Assert.AreEqual(aircraft_A320, fdm.ModelName);
            Assert.AreEqual("A320-200", fdm.Aircraft.AircraftName);
            Assert.AreEqual(1317, fdm.Aircraft.WingArea);
            Assert.AreEqual(111.3, fdm.Aircraft.WingSpan);
            Assert.AreEqual(14.1, fdm.Aircraft.WingChord);
            Assert.AreEqual(333.6, fdm.Aircraft.HTailArea);
            Assert.AreEqual(44.4, fdm.Aircraft.HTailArm);
            Assert.AreEqual(231.3, fdm.Aircraft.VTailArea);
            Assert.AreEqual(0.0, fdm.Aircraft.VTailArm);
            Assert.AreEqual(new Vector3D(80, -30.0, 70), fdm.Aircraft.EyepointXYZ);
            Assert.AreEqual(new Vector3D(672, 0.0, 20), fdm.Aircraft.AeroRefPointXYZ);
            Assert.AreEqual(new Vector3D(661.1, 0, -37), fdm.Aircraft.VisualRefPointXYZ);
        }

        [Test]
        public void CheckLoad_C172x()
        {
            FDMExecutive fdm = new FDMExecutive();
            fdm.LoadModel(AircraftPath, EnginePath, null, aircraft_c172x, true);

            Assert.AreEqual(aircraft_c172x, fdm.ModelName);
            Assert.AreEqual("Cessna C-172 Skyhawk", fdm.Aircraft.AircraftName);
            Assert.AreEqual(174.0, fdm.Aircraft.WingArea);
            Assert.AreEqual(35.8, fdm.Aircraft.WingSpan);
            Assert.AreEqual(4.9, fdm.Aircraft.WingChord);
            Assert.AreEqual(21.9, fdm.Aircraft.HTailArea);
            Assert.AreEqual(15.7, fdm.Aircraft.HTailArm);
            Assert.AreEqual(16.5, fdm.Aircraft.VTailArea);
            Assert.AreEqual(15.7, fdm.Aircraft.VTailArm);
            Assert.AreEqual(new Vector3D(37, 0.0, 48), fdm.Aircraft.EyepointXYZ);
            Assert.AreEqual(new Vector3D(43.2, 0.0, 59.4), fdm.Aircraft.AeroRefPointXYZ);
            Assert.AreEqual(new Vector3D(42.6, 0, 38.5), fdm.Aircraft.VisualRefPointXYZ);
        }


        [Test]
        public void CheckLoad_MK82()
        {
            FDMExecutive fdm = new FDMExecutive();

            fdm.LoadModel(AircraftPath, EnginePath, null, aircraft_MK82, true);

            Assert.AreEqual(aircraft_MK82, fdm.ModelName);
            Assert.AreEqual("MK-82", fdm.Aircraft.AircraftName);
        }

        [Test]
        public void CheckLoad2_MK82()
        {
            FDMExecutive fdm = new FDMExecutive();
            fdm.AircraftPath = AircraftPath;
            fdm.EnginePath = EnginePath;
            fdm.LoadModel(aircraft_MK82, true);

            Assert.AreEqual(aircraft_MK82, fdm.ModelName);
            Assert.AreEqual("MK-82", fdm.Aircraft.AircraftName);
        }

        [Test]
        public void CheckLoadInitCond_MK82()
        {
            FDMExecutive fdm = new FDMExecutive();
            fdm.AircraftPath = AircraftPath;
            fdm.EnginePath = EnginePath;
            InitialCondition IC = fdm.GetIC();

            fdm.LoadModel(aircraft_MK82, true);

            IC.Load("reset00", true);

            Assert.AreEqual(aircraft_MK82, fdm.ModelName);
            Assert.AreEqual("MK-82", fdm.Aircraft.AircraftName);
        }
    }
}
