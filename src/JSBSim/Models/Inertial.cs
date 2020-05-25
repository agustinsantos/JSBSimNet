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

namespace JSBSim.Models
{
    using System;

    // Import log4net classes.
    using log4net;

    using CommonUtils.MathLib;
    using JSBSim.InputOutput;

    /// <summary>
    ///  Models inertial forces (e.g. centripetal and coriolis accelerations).
    ///  Starting conversion to WGS84.
    /// </summary>
    public class Inertial : Model
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

        public Inertial(FDMExecutive fdmex) : base(fdmex)
        {
            RadiusReference = 20925646.32546;

            Name = "Inertial";

            // Earth defaults
            double RotationRate = 0.00007292115;
            //  RotationRate    = 0.000072921151467;
            GM = 14.0764417572E15;   // WGS84 value
            C2_0 = -4.84165371736E-04; // WGS84 value for the C2,0 coefficient
            J2 = 1.08262982E-03;     // WGS84 value for J2
            a = 20925646.32546;     // WGS84 semimajor axis length in feet
                                    //  a               = 20902254.5305;      // Effective Earth radius for a sphere
            b = 20855486.5951;      // WGS84 semiminor axis length in feet
            gravType = eGravType.gtWGS84;

            // Lunar defaults
            /*
            double RotationRate    = 0.0000026617;
            GM              = 1.7314079E14;         // Lunar GM
            RadiusReference = 5702559.05;           // Equatorial radius
            C2_0            = 0;                    // value for the C2,0 coefficient
            J2              = 2.033542482111609E-4; // value for J2
            a               = 5702559.05;           // semimajor axis length in feet
            b               = 5695439.63;           // semiminor axis length in feet
            */

            vOmegaPlanet = new Vector3D(0.0, 0.0, RotationRate);
            GroundCallback = new DefaultGroundCallback(RadiusReference, RadiusReference);

            Bind();

            Debug(0);
        }

        /// <summary>
        /// Runs the Inertial model; called by the Executive
        /// Can pass in a value indicating if the executive is directing the
        /// simulation to Hold.
        /// </summary>
        /// <param name="Holding">if true, the executive has been directed to hold the sim
        /// from advancing time.Some models may ignore this flag, such
        /// as the Input model, which may need to be active to listen
        /// on a socket for the "Resume" command to be given.</param>
        /// <returns>false if no error</returns>
        public override bool Run(bool Holding)
        {
            // Fast return if we have nothing to do ...
            if (base.Run(Holding)) return true;
            if (Holding) return false;

            // Gravitation accel
            switch (gravType)
            {
                case eGravType.gtStandard:
                    {
                        double radius = inputs.Position.Radius;
                        vGravAccel = -(GetGAccel(radius) / radius) * (Vector3D)inputs.Position;
                    }
                    break;
                case eGravType.gtWGS84:
                    vGravAccel = GetGravityJ2(inputs.Position);
                    break;
            }

            return false;
        }

        public static double GetStandardGravity() { return gAccelReference; }
        public Vector3D GetGravity() { return vGravAccel; }
        public Vector3D GetOmegaPlanet() { return vOmegaPlanet; }
        public void SetOmegaPlanet(double rate)
        {
            vOmegaPlanet = new Vector3D(0.0, 0.0, rate);
        }
        public double GetRefRadius() { return RadiusReference; }
        public double GetSemimajor() { return a; }
        public double GetSemiminor() { return b; }

        /* Functions that rely on the ground callback
           The following functions allow to set and get the vehicle position above
           the ground. The ground level is obtained by interrogating an instance of
           FGGroundCallback. A ground callback must therefore be set with
           SetGroundCallback() before calling any of these functions. */

        /// <summary>
        /// Get terrain contact point information below the current location.
        /// </summary>
        /// <param name="location">Location at which the contact point is evaluated.</param>
        /// <param name="contact">Contact point location</param>
        /// <param name="normal">Terrain normal vector in contact point    (ECEF frame)</param>
        /// <param name="velocity">Terrain linear velocity in contact point  (ECEF frame)</param>
        /// <param name="ang_velocity">Terrain angular velocity in contact point (ECEF frame)</param>
        /// <returns>altitude above contact point (AGL) in feet.</returns>
        /// <see cref="SetGroundCallback"/>
        public double GetContactPoint(Location location, out Location contact,
                        out Vector3D normal, out Vector3D velocity,
                        out Vector3D ang_velocity)
        {
            return GroundCallback.GetAGLevel(location, out contact, out normal, out velocity,
                                              out ang_velocity);
        }

        /// <summary>
        ///  Get the altitude above ground level.
        /// </summary>
        /// <param name="location">Location at which the AGL is evaluated.</param>
        /// <returns>the altitude AGL in feet.</returns>
        /// <see cref="SetGroundCallback"/>
        public double GetAltitudeAGL(Location location)
        {
            Location lDummy;
            Vector3D vDummy;
            return GroundCallback.GetAGLevel(location, out lDummy, out vDummy, out vDummy, out vDummy);
        }

        /// <summary>
        ///  Set the altitude above ground level.
        /// </summary>
        /// <param name="location">Location at which the AGL is set.</param>
        /// <param name="altitudeAGL">Altitude above Ground Level in feet.</param>
        /// <see cref="SetGroundCallback"/>
        public void SetAltitudeAGL(Location location, double altitudeAGL)
        {
            Location contact;
            Vector3D vDummy;
            GroundCallback.GetAGLevel(location, out contact, out vDummy, out vDummy, out vDummy);
            double groundHeight = contact.GeodAltitude;
            double longitude = location.Longitude;
            double geodLat = location.GeodLatitudeRad;
            location.SetPositionGeodetic(longitude, geodLat,
                                         groundHeight + altitudeAGL);
        }

        /// <summary>
        /// Set the terrain elevation above sea level.
        /// </summary>
        /// <param name="h">Terrain elevation in ft.</param>
        /// <see cref="SetGroundCallback"/>
        public void SetTerrainElevation(double h)
        {
            GroundCallback.SetTerrainElevation(h);
        }

        /// <summary>
        /// Set the simulation time.
        /// The elapsed time can be used by the ground callbck to assess the planet
        /// rotation or the movement of objects.
        /// </summary>
        /// <param name="time">elapsed time in seconds since the simulation started.</param>
        public void SetTime(double time)
        {
            GroundCallback.SetTime(time);
        }

        /// <summary>
        /// Sets the ground callback pointer.
        /// FGInertial will take ownership of the pointer which must therefore be
        /// located in the heap.
        /// </summary>
        /// <param name="gc">A pointer to a ground callback object</param>
        /// <see cref="FGGroundCallback"/>
        public void SetGroundCallback(GroundCallback gc) { GroundCallback = gc; }

        public struct Inputs
        {
            public Location Position;
        }

        public Inputs inputs = new Inputs();

        /// These define the indices use to select the gravitation models.
        public enum eGravType
        {
            /// Evaluate gravity using Newton's classical formula assuming the Earth is
            /// spherical
            gtStandard,
            /// Evaluate gravity using WGS84 formulas that take the Earth oblateness
            /// into account
            gtWGS84
        };

        // Standard gravity (9.80665 m/s^2) in ft/s^2 which is the gravity at 45 deg.
        // of latitude (see ISA 1976 and Steven & Lewis)
        // It includes the centripetal acceleration.
        private static readonly double gAccelReference = 9.80665 / Constants.fttom;

        private Vector3D vOmegaPlanet;
        private Vector3D vGravAccel;
        private double RadiusReference;
        private double GM;
        private double C2_0; // WGS84 value for the C2,0 coefficient
        private double J2;   // WGS84 value for J2
        private double a;    // WGS84 semimajor axis length in feet 
        private double b;    // WGS84 semiminor axis length in feet
        private eGravType gravType;
        private GroundCallback GroundCallback;

        private double GetGAccel(double r)
        {
            return GM / (r * r);
        }

        /// <summary>
        /// Calculate the WGS84 gravitation value in ECEF frame. Pass in the ECEF
        /// position via the position parameter. The J2Gravity value returned is in ECEF
        /// frame, and therefore may need to be expressed (transformed) in another frame,
        /// depending on how it is used. See Stevens and Lewis eqn. 1.4-16.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector3D GetGravityJ2(Location position)
        {
            Vector3D J2Gravity = new Vector3D();

            // Gravitation accel
            double r = position.Radius;
            double sinLat = position.SinLatitude;

            double adivr = a / r;
            double preCommon = 1.5 * J2 * adivr * adivr;
            double xy = 1.0 - 5.0 * (sinLat * sinLat);
            double z = 3.0 - 5.0 * (sinLat * sinLat);
            double GMOverr2 = GM / (r * r);

            J2Gravity.X = -GMOverr2 * ((1.0 + (preCommon * xy)) * position.X / r);
            J2Gravity.Y = -GMOverr2 * ((1.0 + (preCommon * xy)) * position.Y / r);
            J2Gravity.Z = -GMOverr2 * ((1.0 + (preCommon * z)) * position.Z / r);

            return J2Gravity;
        }

        public override void Bind()
        {
            //propertyManager.Tie("inertial/sea-level-radius_ft", &in.Position,
            //                     &FGLocation::GetSeaLevelRadius);
            //propertyManager.Tie("simulation/gravity-model", &gravType);
        }
        protected override void Debug(int from)
        {
            if (log.IsDebugEnabled)
                log.Debug("Instantiated: Inertial.");
        }

    }
}
