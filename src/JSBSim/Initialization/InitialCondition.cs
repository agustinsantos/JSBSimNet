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
namespace JSBSim
{
    using System;
    using System.IO;
    using System.Xml;
    using CommonUtils.IO;
    using CommonUtils.MathLib;
    using JSBSim.Format;
    using JSBSim.InputOutput;
    using JSBSim.Models;
    using JSBSim.Script;
    // Import log4net classes.
    using log4net;

    public enum SpeedSet { setvt, setvc, setve, setmach, setuvw, setned, setvg };
    public enum AltitudeSet { setasl, setagl };
    public enum LatitudeSet { setgeoc, setgeod };
    public enum WindSet { setwned, setwmd, setwhc };

    /// <summary>
    /// Initializes the simulation run.
    /// Takes a set of initial conditions(IC) and provide a kinematically
    /// consistent set of body axis velocity components, euler angles, and altitude.
    /// This class does not attempt to trim the model i.e.the sim will most likely
    /// start in a very dynamic state(unless, of course, you have chosen your IC's
    /// 
    /// wisely, or started on the ground) even after setting it up with this class.
    /// 
    /// <h3>Usage Notes</h3>
    /// 
    /// With a valid object of FGFDMExec and an aircraft model loaded:
    /// 
    /// @code
    /// FGInitialCondition* fgic = FDMExec.GetIC();
    /// 
    /// // Reset the initial conditions and set VCAS and altitude
    /// fgic.InitializeIC();
    /// fgic.SetVcalibratedKtsIC(vcas);
    /// fgic.SetAltitudeAGLFtIC(altitude);
    /// 
    /// // directly into Run
    /// FDMExec.GetPropagate.SetInitialState(fgic);
    ///  
    /// FDMExec.Run();
    /// 
    /// //or to loop the sim w/o integrating
    /// FDMExec.RunIC();
    /// @endcode
    /// 
    /// Alternatively, you can load initial conditions from an XML file:
    /// 
    /// @code
    /// FGInitialCondition  fgic = FDMExec.GetIC();
    /// fgic.Load(IC_file);
    /// @endcode
    /// 
    /// <h3> Speed</h3>
    /// 
    /// 
    /// Since vc, ve, vt, and mach all represent speed, the remaining
    /// 
    /// three are recalculated each time one of them is set (using the
    /// current altitude).  The most recent speed set is remembered so
    /// that if and when altitude is reset, the last set speed is used
    /// to recalculate the remaining three.Setting any of the body
    /// components forces a recalculation of vt and vt then becomes the
    /// most recent speed set.
    /// 
    /// <h3>Alpha, Gamma, and Theta</h3>
    /// 
    /// This class assumes that it will be used to set up the sim for a
    /// steady, zero pitch rate condition.Since any two of those angles
    /// specifies the third gamma(flight path angle) is favored when setting
    /// alpha and theta and alpha is favored when setting gamma.i.e.
    /// 
    /// - set alpha : recalculate theta using gamma as currently set
    /// - set theta : recalculate alpha using gamma as currently set
    /// - set gamma : recalculate theta using alpha as currently set
    /// 
    /// The idea being that gamma is most interesting to pilots(since it
    /// is indicative of climb rate).
    /// 
    /// Setting climb rate is, for the purpose of this discussion,
    /// considered equivalent to setting gamma.
    /// 
    /// These are the items that can be set in an initialization file:
    /// 
    /// - ubody (velocity, ft/sec)
    /// - vbody (velocity, ft/sec)
    /// - wbody (velocity, ft/sec)
    /// - vnorth (velocity, ft/sec)
    /// - veast (velocity, ft/sec)
    /// - vdown (velocity, ft/sec)
    /// - latitude (position, degrees)
    /// - longitude (position, degrees)
    /// - phi (orientation, degrees)
    /// - theta (orientation, degrees)
    /// - psi (orientation, degrees)
    /// - alpha (angle, degrees)
    /// - beta (angle, degrees)
    /// - gamma (angle, degrees)
    /// - roc (vertical velocity, ft/sec)
    /// - elevation (local terrain elevation, ft)
    /// - altitude (altitude AGL, ft)
    /// - altitudeAGL (altitude AGL, ft)
    /// - altitudeMSL (altitude MSL, ft)
    /// - winddir (wind from-angle, degrees)
    /// - vwind (magnitude wind speed, ft/sec)
    /// - hwind (headwind speed, knots)
    /// - xwind (crosswind speed, knots)
    /// - vc (calibrated airspeed, ft/sec)
    /// - mach (mach)
    /// - vground (ground speed, ft/sec)
    /// - trim (0 for no trim, 1 for ground trim, 'Longitudinal', 'Full', 'Ground', 'Pullup', 'Custom', 'Turn')
    /// - running (-1 for all engines, 0 ... n-1 for specific engines)
    /// 
    /// <h3>Properties</h3>
    /// @property ic/vc-kts (read/write) Calibrated airspeed initial condition in knots
    /// @property ic/ve-kts (read/write) Knots equivalent airspeed initial condition
    /// @property ic/vg-kts (read/write) Ground speed initial condition in knots
    /// @property ic/vt-kts (read/write) True airspeed initial condition in knots
    /// @property ic/mach (read/write) Mach initial condition
    /// @property ic/roc-fpm (read/write) Rate of climb initial condition in feet/minute
    /// @property ic/gamma-deg (read/write) Flightpath angle initial condition in degrees
    /// @property ic/alpha-deg (read/write) Angle of attack initial condition in degrees
    /// @property ic/beta-deg (read/write) Angle of sideslip initial condition in degrees
    /// @property ic/theta-deg (read/write) Pitch angle initial condition in degrees
    /// @property ic/phi-deg (read/write) Roll angle initial condition in degrees
    /// @property ic/psi-true-deg (read/write) Heading angle initial condition in degrees
    /// @property ic/lat-gc-deg (read/write) Latitude initial condition in degrees
    /// @property ic/long-gc-deg (read/write) Longitude initial condition in degrees
    /// @property ic/h-sl-ft (read/write) Height above sea level initial condition in feet
    /// @property ic/h-agl-ft (read/write) Height above ground level initial condition in feet
    /// @property ic/sea-level-radius-ft (read/write) Radius of planet at sea level in feet
    /// @property ic/terrain-elevation-ft (read/write) Terrain elevation above sea level in feet
    /// @property ic/vg-fps (read/write) Ground speed initial condition in feet/second
    /// @property ic/vt-fps (read/write) True airspeed initial condition in feet/second
    /// @property ic/vw-bx-fps (read/write) Wind velocity initial condition in Body X frame in feet/second
    /// @property ic/vw-by-fps (read/write) Wind velocity initial condition in Body Y frame in feet/second
    /// @property ic/vw-bz-fps (read/write) Wind velocity initial condition in Body Z frame in feet/second
    /// @property ic/vw-north-fps (read/write) Wind northward velocity initial condition in feet/second
    /// @property ic/vw-east-fps (read/write) Wind eastward velocity initial condition in feet/second
    /// @property ic/vw-down-fps (read/write) Wind downward velocity initial condition in feet/second
    /// @property ic/vw-mag-fps (read/write) Wind velocity magnitude initial condition in feet/sec.
    /// @property ic/vw-dir-deg (read/write) Wind direction initial condition, in degrees from north
    /// @property ic/roc-fps (read/write) Rate of climb initial condition, in feet/second
    /// @property ic/u-fps (read/write) Body frame x-axis velocity initial condition in feet/second
    /// @property ic/v-fps (read/write) Body frame y-axis velocity initial condition in feet/second
    /// @property ic/w-fps (read/write) Body frame z-axis velocity initial condition in feet/second
    /// @property ic/vn-fps (read/write) Local frame x-axis (north) velocity initial condition in feet/second
    /// @property ic/ve-fps (read/write) Local frame y-axis (east) velocity initial condition in feet/second
    /// @property ic/vd-fps (read/write) Local frame z-axis (down) velocity initial condition in feet/second
    /// @property ic/gamma-rad (read/write) Flight path angle initial condition in radians
    /// @property ic/alpha-rad (read/write) Angle of attack initial condition in radians
    /// @property ic/theta-rad (read/write) Pitch angle initial condition in radians
    /// @property ic/beta-rad (read/write) Angle of sideslip initial condition in radians
    /// @property ic/phi-rad (read/write) Roll angle initial condition in radians
    /// @property ic/psi-true-rad (read/write) Heading angle initial condition in radians
    /// @property ic/lat-gc-rad (read/write) Geocentric latitude initial condition in radians
    /// @property ic/long-gc-rad (read/write) Longitude initial condition in radians
    /// @property ic/p-rad_sec (read/write) Roll rate initial condition in radians/second
    /// @property ic/q-rad_sec (read/write) Pitch rate initial condition in radians/second
    /// @property ic/r-rad_sec (read/write) Yaw rate initial condition in radians/second
    /// 
    /// @author Tony Peden
    /// 
    /// </summary>
    public class InitialCondition
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


        public InitialCondition(FDMExecutive fdmex)
        {
            this.fdmex = fdmex;

            InitializeIC();

            if (fdmex != null)
            {
                Atmosphere = fdmex.Atmosphere;
                Aircraft = fdmex.Aircraft;
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("InitialCondition: This class requires a pointer to a valid FDMExecutive object");
            }
        }

        /// <summary>
        /// Set calibrated airspeed initial condition in knots.
        /// </summary>
        /// <param name="vcas">Calibrated airspeed in knots</param>
        public void SetVcalibratedKtsIC(double vcas)
        {
            double altitudeASL = position.GeodAltitude;
            double pressure = Atmosphere.GetPressure(altitudeASL);
            double mach = Conversion.MachFromVcalibrated(Math.Abs(vcas) * Constants.ktstofps, pressure);
            double soundSpeed = Atmosphere.GetSoundSpeed(altitudeASL);

            SetVtrueFpsIC(mach * soundSpeed);
            lastSpeedSet = SpeedSet.setvc;
        }

        /// <summary>
        /// Set equivalent airspeed initial condition in knots.
        /// </summary>
        /// <param name="ve">Equivalent airspeed in knots</param>
        public void SetVequivalentKtsIC(double ve)
        {
            double altitudeASL = position.GeodAltitude;
            double rho = Atmosphere.GetDensity(altitudeASL);
            double rhoSL = Atmosphere.GetDensitySL();
            SetVtrueFpsIC(ve * Constants.ktstofps * Math.Sqrt(rhoSL / rho));
            lastSpeedSet = SpeedSet.setve;
        }

        /// <summary>
        /// Set true airspeed initial condition in knots.
        /// </summary>
        /// <param name="vtrue">True airspeed in knots</param>
        public void SetVtrueKtsIC(double vtrue) { SetVtrueFpsIC(vtrue * Constants.ktstofps); }

        /// <summary>
        ///  Set ground speed initial condition in knots.
        /// </summary>
        /// <param name="vg">Ground speed in knots</param>
        public void SetVgroundKtsIC(double vg) { SetVgroundFpsIC(vg * Constants.ktstofps); }

        /// <summary>
        ///  Set mach initial condition.
        /// </summary>
        /// <param name="mach">Mach number</param>
        public void SetMachIC(double mach)
        {
            double altitudeASL = position.GeodAltitude;
            double soundSpeed = Atmosphere.GetSoundSpeed(altitudeASL);
            SetVtrueFpsIC(mach * soundSpeed);
            lastSpeedSet = SpeedSet.setmach;
        }

        /// <summary>
        /// Sets angle of attack initial condition in degrees.
        /// </summary>
        /// <param name="a">Alpha in degrees</param>
        public void SetAlphaDegIC(double a) { SetAlphaRadIC(a * Constants.degtorad); }

        /// <summary>
        /// Sets angle of sideslip initial condition in degrees.
        /// </summary>
        /// <param name="b">Beta in degrees</param>
        public void SetBetaDegIC(double b) { SetBetaRadIC(b * Constants.degtorad); }

        /// <summary>
        /// Sets pitch angle initial condition in degrees.
        /// </summary>
        /// <param name="theta">Theta (pitch) angle in degrees</param>
        public void SetThetaDegIC(double theta) { SetThetaRadIC(theta * Constants.degtorad); }

        /// <summary>
        /// Resets the IC data structure to new values
        /// </summary>
        public void ResetIC(double u0, double v0, double w0,
                                 double p0, double q0, double r0,
                                 double alpha0, double beta0,
                                 double phi0, double theta0, double psi0,
                                 double latRad0, double lonRad0, double altAGLFt0,
                                 double gamma0)
        {
            double calpha = Math.Cos(alpha0), cbeta = Math.Cos(beta0);
            double salpha = Math.Sin(alpha0), sbeta = Math.Sin(beta0);

            InitializeIC();

            vPQR_body = new Vector3D(p0, q0, r0);
            alpha = alpha0; beta = beta0;

            position.Longitude = lonRad0;
            position.Latitude = latRad0;
            fdmex.GetInertial().SetAltitudeAGL(position, altAGLFt0);
            lastLatitudeSet = LatitudeSet.setgeoc;
            lastAltitudeSet = AltitudeSet.setagl;

            orientation = new Quaternion(phi0, theta0, psi0);
            Matrix3D Tb2l = orientation.GetTInv();

            vUVW_NED = Tb2l * new Vector3D(u0, v0, w0);
            vt = vUVW_NED.Magnitude();
            lastSpeedSet = SpeedSet.setuvw;

            Tw2b = new Matrix3D(
                            calpha * cbeta, -calpha * sbeta, -salpha,
                            sbeta, cbeta, 0.0,
                            salpha * cbeta, -salpha * sbeta, calpha);
            Tb2w = Tw2b.Transposed();

            SetFlightPathAngleRadIC(gamma0);
        }

        /// <summary>
        /// Sets the roll angle initial condition in degrees.
        /// </summary>
        /// <param name="phi">roll angle in degrees</param>
        public void SetPhiDegIC(double phi) { SetPhiRadIC(phi * Constants.degtorad); }

        /// <summary>
        /// Sets the heading angle initial condition in degrees.
        /// </summary>
        /// <param name="psi">Heading angle in degrees</param>
        public void SetPsiDegIC(double psi) { SetPsiRadIC(psi * Constants.degtorad); }

        /// <summary>
        /// Sets the climb rate initial condition in feet/minute.
        /// </summary>
        /// <param name="roc">Rate of Climb in feet/minute</param>
        public void SetClimbRateFpmIC(double roc) { SetClimbRateFpsIC(roc / 60.0); }

        /// <summary>
        /// Sets the flight path angle initial condition in degrees.
        /// </summary>
        /// <param name="gamma">Flight path angle in degrees</param>
        public void SetFlightPathAngleDegIC(double gamma)
        { SetClimbRateFpsIC(vt * Math.Sin(gamma * Constants.degtorad)); }

        /// <summary>
        /// Sets the altitude above sea level initial condition in feet.
        /// If the airspeed has been previously set with parameters
        /// that are atmosphere dependent (Mach, VCAS, VEAS) then the true airspeed is
        /// modified to keep the last set speed to its previous value.
        /// </summary>
        /// <param name="alt">Altitude above sea level in feet</param>
        public void SetAltitudeASLFtIC(double alt)
        {
            double altitudeASL = position.GeodAltitude;
            double pressure = Atmosphere.GetPressure(altitudeASL);
            double soundSpeed = Atmosphere.GetSoundSpeed(altitudeASL);
            double rho = Atmosphere.GetDensity(altitudeASL);
            double rhoSL = Atmosphere.GetDensitySL();

            double mach0 = vt / soundSpeed;
            double vc0 = Conversion.VcalibratedFromMach(mach0, pressure);
            double ve0 = vt * Math.Sqrt(rho / rhoSL);

            double geodLatitude = position.GeodLatitudeRad;
            double longitude = position.Longitude;
            altitudeASL = alt;
            position.SetPositionGeodetic(longitude, geodLatitude, alt);

            soundSpeed = Atmosphere.GetSoundSpeed(altitudeASL);
            rho = Atmosphere.GetDensity(altitudeASL);
            pressure = Atmosphere.GetPressure(altitudeASL);

            switch (lastSpeedSet)
            {
                case SpeedSet.setvc:
                    mach0 = Conversion.MachFromVcalibrated(vc0, pressure);
                    SetVtrueFpsIC(mach0 * soundSpeed);
                    break;
                case SpeedSet.setmach:
                    SetVtrueFpsIC(mach0 * soundSpeed);
                    break;
                case SpeedSet.setve:
                    SetVtrueFpsIC(ve0 * Math.Sqrt(rhoSL / rho));
                    break;
                default: // Make the compiler stop complaining about missing enums
                    break;
            }

            lastAltitudeSet = AltitudeSet.setasl;
        }

        /// <summary>
        /// Sets the initial Altitude above ground level.
        /// </summary>
        /// <param name="agl">Altitude above ground level in feet</param>
        public void SetAltitudeAGLFtIC(double agl)
        {
            fdmex.GetInertial().SetAltitudeAGL(position, agl);
            lastAltitudeSet = AltitudeSet.setagl;
        }

        /// <summary>
        /// Sets the initial terrain elevation.
        /// </summary>
        /// <param name="elev">Initial terrain elevation in feet</param>
        public void SetTerrainElevationFtIC(double elev)
        {
            double agl = GetAltitudeAGLFtIC();
            fdmex.GetInertial().SetTerrainElevation(elev);

            if (lastAltitudeSet == AltitudeSet.setagl)
                SetAltitudeAGLFtIC(agl);
        }

        /// <summary>
        /// Sets the initial latitude.
        /// </summary>
        /// <param name="lat">Initial latitude in degrees</param>
        public void SetLatitudeDegIC(double lat) { SetLatitudeRadIC(lat * Constants.degtorad); }

        /// <summary>
        /// Sets the initial geodetic latitude.
        /// This method modifies the geodetic altitude in order to keep the altitude
        /// above sea level unchanged.
        /// </summary>
        /// <param name="glat">Initial geodetic latitude in degrees</param>
        public void SetGeodLatitudeDegIC(double glat)
        { SetGeodLatitudeRadIC(glat * Constants.degtorad); }

        /// <summary>
        /// Sets the initial longitude.
        /// </summary>
        /// <param name="lon">Initial longitude in degrees</param>
        public void SetLongitudeDegIC(double lon) { SetLongitudeRadIC(lon * Constants.degtorad); }

        /// <summary>
        /// Gets the initial calibrated airspeed.
        /// </summary>
        /// <returns>Initial calibrated airspeed in knots</returns>
        public double GetVcalibratedKtsIC()
        {
            double altitudeASL = position.GeodAltitude;
            double pressure = Atmosphere.GetPressure(altitudeASL);
            double soundSpeed = Atmosphere.GetSoundSpeed(altitudeASL);
            double mach = vt / soundSpeed;

            return Constants.fpstokts * Conversion.VcalibratedFromMach(mach, pressure);
        }

        /// <summary>
        /// Gets the initial equivalent airspeed.
        /// </summary>
        /// <returns>Initial equivalent airspeed in knots</returns>
        public double GetVequivalentKtsIC()
        {
            double altitudeASL = position.GeodAltitude;
            double rho = Atmosphere.GetDensity(altitudeASL);
            double rhoSL = Atmosphere.GetDensitySL();
            return Constants.fpstokts * vt * Math.Sqrt(rho / rhoSL);
        }

        /// <summary>
        /// Gets the initial ground speed.
        /// </summary>
        /// <returns>Initial ground speed in knots</returns>
        public double GetVgroundKtsIC() { return GetVgroundFpsIC() * Constants.fpstokts; }

        /// <summary>
        /// Gets the initial true velocity.
        /// </summary>
        /// <returns>Initial true airspeed in knots.</returns>
        public double GetVtrueKtsIC()
        {
            return vt * Constants.fpstokts;
        }

        /// <summary>
        /// Gets the initial mach.
        /// </summary>
        /// <returns>Initial mach number</returns>
        public double GetMachIC()
        {
            double altitudeASL = position.GeodAltitude;
            double soundSpeed = Atmosphere.GetSoundSpeed(altitudeASL);
            return vt / soundSpeed;
        }

        /// <summary>
        /// Gets the initial climb rate.
        /// </summary>
        /// <returns>Initial climb rate in feet/minute</returns>
        public double GetClimbRateFpmIC()
        { return GetClimbRateFpsIC() * 60; }

        /// <summary>
        /// Gets the initial flight path angle
        /// </summary>
        /// <returns>Initial flight path angle in degrees</returns>
        public double GetFlightPathAngleDegIC()
        { return GetFlightPathAngleRadIC() * Constants.radtodeg; }

        /// <summary>
        /// Gets the initial angle of attack.
        /// </summary>
        /// <returns>Initial alpha in degrees</returns>
        public double GetAlphaDegIC() { return alpha * Constants.radtodeg; }

        /// <summary>
        /// Gets the initial sideslip angle.
        /// </summary>
        /// <returns>Initial beta in degrees</returns>
        public double GetBetaDegIC() { return beta * Constants.radtodeg; }

        /// <summary>
        /// Gets the initial pitch angle.
        /// </summary>
        /// <returns>Initial pitch angle in degrees</returns>
        public double GetThetaDegIC() { return orientation.GetEulerDeg(Quaternion.EulerAngles.eTht); }

        /// <summary>
        /// Gets the initial roll angle.
        /// </summary>
        /// <returns>Initial phi in degrees</returns>
        public double GetPhiDegIC() { return orientation.GetEulerDeg(Quaternion.EulerAngles.ePhi); }

        /// <summary>
        /// Gets the initial heading angle.
        /// </summary>
        /// <returns>Initial psi in degrees</returns>
        public double GetPsiDegIC() { return orientation.GetEulerDeg(Quaternion.EulerAngles.ePsi); }

        /// <summary>
        /// Gets the initial latitude.
        /// </summary>
        /// <returns>Initial geocentric latitude in degrees</returns>
        public double GetLatitudeDegIC() { return position.LatitudeDeg; }

        /// <summary>
        /// Gets the initial geodetic latitude.
        /// </summary>
        /// <returns>Initial geodetic latitude in degrees</returns>
        public double GetGeodLatitudeDegIC()
        { return position.GeodLatitudeDeg; }

        /// <summary>
        /// Gets the initial longitude.
        /// </summary>
        /// <returns>Initial longitude in degrees</returns>
        public double GetLongitudeDegIC() { return position.LongitudeDeg; }

        /// <summary>
        /// Gets the initial altitude above sea level.
        /// </summary>
        /// <returns>Initial altitude in feet.</returns>
        public double GetAltitudeASLFtIC()
        {
            return position.GeodAltitude;
        }

        /// <summary>
        /// Gets the initial altitude above ground level.
        /// </summary>
        /// <returns>Initial altitude AGL in feet</returns>
        public double GetAltitudeAGLFtIC()
        {
            return fdmex.GetInertial().GetAltitudeAGL(position);
        }

        /// <summary>
        /// Gets the initial terrain elevation.
        /// </summary>
        /// <returns>Initial terrain elevation in feet</returns>
        public double GetTerrainElevationFtIC()
        {
            Location contact;
            Vector3D normal, v, w;
            fdmex.GetInertial().GetContactPoint(position, out contact, out normal, out v, out w);
            return contact.GeodAltitude;
        }

        /// <summary>
        /// Gets the initial Earth position angle. 
        /// Caution it sets the vertical velocity to zero to
        /// keep backward compatibility.
        /// </summary>
        /// <returns>Initial Earth position angle in radians.</returns>
        public double GetEarthPositionAngleIC() { return epa; }

        /// <summary>
        ///  Sets the initial ground speed.
        /// </summary>
        /// <param name="vg">Initial ground speed in feet/second</param>
        public void SetVgroundFpsIC(double vg)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;

            vUVW_NED.U = vg * orientation.GetCosEuler(Quaternion.EulerAngles.ePsi);
            vUVW_NED.V = vg * orientation.GetSinEuler(Quaternion.EulerAngles.ePsi);
            vUVW_NED.W = 0.0;
            _vt_NED = vUVW_NED + _vWIND_NED;
            vt = _vt_NED.Magnitude();

            calcAeroAngles(_vt_NED);

            lastSpeedSet = SpeedSet.setvg;
        }

        /// <summary>
        /// Sets the initial true airspeed.
        /// The amplitude of the airspeed is modified but its
        /// direction is kept unchanged. If there is no wind, the same is true for the
        /// ground velocity. If there is some wind, the airspeed direction is unchanged
        /// but this may result in the ground velocity direction being altered. This is
        /// for backward compatibility.
        /// </summary>
        /// <param name="vtrue">Initial true airspeed in feet/second</param>
        public void SetVtrueFpsIC(double vtrue)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;

            if (vt > 0.1)
                _vt_NED *= vtrue / vt;
            else
                _vt_NED = Tb2l * Tw2b * new Vector3D(vtrue, 0.0, 0.0);

            vt = vtrue;
            vUVW_NED = _vt_NED - _vWIND_NED;

            calcAeroAngles(_vt_NED);

            lastSpeedSet = SpeedSet.setvt;
        }

        /// <summary>
        /// Sets the initial body axis X velocity.
        /// </summary>
        /// <param name="ubody">Initial X velocity in feet/second</param>
        public void SetUBodyFpsIC(double ubody) { SetBodyVelFpsIC(VelocityType.eU, ubody); }


        /// <summary>
        /// /* Sets the initial body axis Y velocity.
        /// </summary>
        /// <param name="vbody">Initial Y velocity in feet/second</param>
        public void SetVBodyFpsIC(double vbody) { SetBodyVelFpsIC(VelocityType.eV, vbody); }

        /// <summary>
        /// Sets the initial body axis Z velocity.
        /// </summary>
        /// <param name="wbody">Initial Z velocity in feet/second</param>
        public void SetWBodyFpsIC(double wbody) { SetBodyVelFpsIC(VelocityType.eW, wbody); }

        /// <summary>
        /// Initial Z velocity in feet/second
        /// </summary>
        /// <param name="vn">Initial north velocity in feet/second</param>
        public void SetVNorthFpsIC(double vn) { SetNEDVelFpsIC(VelocityType.eU, vn); }

        /// <summary>
        /// Sets the initial local axis east velocity.
        /// </summary>
        /// <param name="ve">Initial east velocity in feet/second</param>
        public void SetVEastFpsIC(double ve) { SetNEDVelFpsIC(VelocityType.eV, ve); }

        /// <summary>Sets the initial local axis down velocity.
        /// </summary>
        /// <param name="vd">Initial down velocity in feet/second</param>
        public void SetVDownFpsIC(double vd) { SetNEDVelFpsIC(VelocityType.eW, vd); }

        /// <summary>
        /// Sets the initial body axis roll rate.
        /// </summary>
        /// <param name="P">Initial roll rate in radians/second</param>
        public void SetPRadpsIC(double P) { vPQR_body.P = P; }

        /// <summary>
        /// Sets the initial body axis pitch rate.
        /// </summary>
        /// <param name="Q">Initial pitch rate in radians/second</param>
        public void SetQRadpsIC(double Q) { vPQR_body.Q = Q; }

        /// <summary>
        /// Sets the initial body axis yaw rate.
        /// </summary>
        /// <param name="R">initial yaw rate in radians/second</param>
        public void SetRRadpsIC(double R) { vPQR_body.R = R; }

        /// <summary>
        /// Sets the initial wind velocity.
        /// The aircraft velocity
        /// with respect to the ground is not changed but the true airspeed is.
        /// </summary>
        /// <param name="wN">Initial wind velocity in local north direction, feet/second</param>
        /// <param name="wE">Initial wind velocity in local east direction, feet/second</param>
        /// <param name="wD">Initial wind velocity in local down direction, feet/second</param>
        public void SetWindNEDFpsIC(double wN, double wE, double wD)
        {
            Vector3D _vt_NED = vUVW_NED + new Vector3D(wN, wE, wD);
            vt = _vt_NED.Magnitude();

            calcAeroAngles(_vt_NED);
        }

        /// <summary>
        /// Sets the initial total wind speed.
        /// Modifies the wind velocity (in knots) while keeping its direction unchanged.
        /// The vertical component (in local NED frame) is unmodified. The aircraft
        /// velocity with respect to the ground is not changed but the true airspeed is.
        /// </summary>
        /// <param name="mag">Initial wind velocity magnitude in knots</param>
        public void SetWindMagKtsIC(double mag)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;
            Vector3D _vHEAD = new Vector3D(_vWIND_NED.U, _vWIND_NED.V, 0.0);
            double windMag = _vHEAD.Magnitude();

            if (windMag > 0.001)
                _vHEAD *= (mag * Constants.ktstofps) / windMag;
            else
                _vHEAD = new Vector3D(mag * Constants.ktstofps, 0.0, 0.0);

            _vWIND_NED.U = _vHEAD.U;
            _vWIND_NED.V = _vHEAD.V;
            _vt_NED = vUVW_NED + _vWIND_NED;
            vt = _vt_NED.Magnitude();

            calcAeroAngles(_vt_NED);
        }

        /// <summary>
        /// Sets the initial wind direction.
        /// Modifies the wind direction while keeping its velocity unchanged. The vertical
        /// component (in local NED frame) is unmodified. The aircraft velocity with
        /// respect to the ground is not changed but the true airspeed is.
        /// </summary>
        /// <param name="dir">Initial direction wind is coming from in degrees</param>
        public void SetWindDirDegIC(double dir)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;
            double mag = _vWIND_NED.GetMagnitude((int)VelocityType.eU - 1, (int)VelocityType.eV - 1);
            Vector3D _vHEAD = new Vector3D(mag * Math.Cos(dir * Constants.degtorad), mag * Math.Sin(dir * Constants.degtorad), 0.0);

            _vWIND_NED.U = _vHEAD.U;
            _vWIND_NED.V = _vHEAD.V;
            _vt_NED = vUVW_NED + _vWIND_NED;
            vt = _vt_NED.Magnitude();

            calcAeroAngles(_vt_NED);
        }

        /// <summary>
        /// Sets the initial headwind velocity.
        /// </summary>
        /// <param name="head">Initial headwind speed in knots</param>
        public void SetHeadWindKtsIC(double head)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;
            // This is a head wind, so the direction vector for the wind
            // needs to be set opposite to the heading the aircraft
            // is taking. So, the cos and sin of the heading (psi)
            // are negated in the line below.
            Vector3D _vHEAD = new Vector3D(-orientation.GetCosEuler().Psi, -orientation.GetSinEuler().Psi, 0.0);

            // Gram-Schmidt process is used to remove the existing head wind component
            _vWIND_NED -= Vector3D.Dot(_vWIND_NED, _vHEAD) * _vHEAD;
            // Which is now replaced by the new value. The input head wind is expected
            // in knots, so first convert to fps, which is the internal unit used.
            _vWIND_NED += (head * Constants.ktstofps) * _vHEAD;
            _vt_NED = vUVW_NED + _vWIND_NED;

            vt = _vt_NED.Magnitude();

            calcAeroAngles(_vt_NED);
        }

        /// <summary>
        /// Sets the initial crosswind speed.
        /// Set the cross wind velocity (in knots). Here, 'cross wind' means perpendicular
        /// to the aircraft heading and parallel to the ground. The aircraft velocity
        /// with respect to the ground is not changed but the true airspeed is.
        /// </summary>
        /// <param name="cross">Initial crosswind speed, positive from left to right</param>
        public void SetCrossWindKtsIC(double cross)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;
            Vector3D _vCROSS = new Vector3D(-orientation.GetSinEuler(Quaternion.EulerAngles.ePsi),
                                                orientation.GetCosEuler(Quaternion.EulerAngles.ePsi),
                                                0.0);

            // Gram-Schmidt process is used to remove the existing cross wind component
            _vWIND_NED -= Vector3D.Dot(_vWIND_NED, _vCROSS) * _vCROSS;
            // Which is now replaced by the new value. The input cross wind is expected
            // in knots, so first convert to fps, which is the internal unit used.
            _vWIND_NED += (cross * Constants.ktstofps) * _vCROSS;
            _vt_NED = vUVW_NED + _vWIND_NED;
            vt = _vt_NED.Magnitude();

            calcAeroAngles(_vt_NED);
        }

        /// <summary>
        /// Sets the initial wind downward speed.
        /// Set the vertical wind velocity (in knots). The 'vertical' is taken in the
        /// local NED frame. The aircraft velocity with respect to the ground is not
        /// changed but the true airspeed is.
        /// </summary>
        /// <param name="wD">Initial downward wind speed in knots</param>
        public void SetWindDownKtsIC(double wD)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);

            _vt_NED.W = vUVW_NED.W + wD;
            vt = _vt_NED.Magnitude();

            calcAeroAngles(_vt_NED);
        }

        /// <summary>
        /// Sets the initial climb rate.
        /// When the climb rate is modified, we need to update the angles theta and beta
        /// to keep the true airspeed amplitude, the AoA and the heading unchanged.
        /// Beta will be modified if the aircraft roll angle is not null.
        /// </summary>
        /// <param name="hdot">Initial Rate of climb in feet/second</param>
        public void SetClimbRateFpsIC(double hdot)
        {
            if (Math.Abs(hdot) > vt)
            {
                log.Error("The climb rate cannot be higher than the true speed.");
                return;
            }

            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _WIND_NED = _vt_NED - vUVW_NED;
            double hdot0 = -_vt_NED.W;

            if (Math.Abs(hdot0) < vt)
            { // Is this check really needed ?
                double scale = Math.Sqrt((vt * vt - hdot * hdot) / (vt * vt - hdot0 * hdot0));
                _vt_NED.U *= scale;
                _vt_NED.V *= scale;
            }
            _vt_NED.W = -hdot;
            vUVW_NED = _vt_NED - _WIND_NED;

            // Updating the angles theta and beta to keep the true airspeed amplitude
            calcThetaBeta(alpha, _vt_NED);
        }

        /// <summary>
        /// Gets the initial ground velocity.
        /// </summary>
        /// <returns>Initial ground velocity in feet/second</returns>
        public double GetVgroundFpsIC() { return vUVW_NED.GetMagnitude((int)VelocityType.eU - 1, (int)VelocityType.eV - 1); }

        /// <summary>
        /// Gets the initial true velocity.
        /// </summary>
        /// <returns>Initial true velocity in feet/second</returns>
        public double GetVtrueFpsIC() { return vt; }

        /// <summary>
        /// Gets the initial body axis X wind velocity.
        /// </summary>
        /// <returns>Initial body axis X wind velocity in feet/second</returns>
        public double GetWindUFpsIC() { return GetBodyWindFpsIC(VelocityType.eU); }

        /// <summary>
        /// Gets the initial body axis Y wind velocity.
        /// </summary>
        /// <returns>Initial body axis Y wind velocity in feet/second</returns>
        public double GetWindVFpsIC() { return GetBodyWindFpsIC(VelocityType.eV); }

        /// <summary>
        /// Gets the initial body axis Z wind velocity.
        /// </summary>
        /// <returns>Initial body axis Z wind velocity in feet/second</returns>
        public double GetWindWFpsIC() { return GetBodyWindFpsIC(VelocityType.eW); }

        /// <summary>
        /// Gets the initial wind velocity in the NED local frame
        /// </summary>
        /// <returns>Initial wind velocity in NED frame in feet/second</returns>
        public Vector3D GetWindNEDFpsIC()
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            return _vt_NED - vUVW_NED;
        }

        /// <summary>
        /// Gets the initial wind velocity in local frame.
        /// </summary>
        /// <returns>Initial wind velocity toward north in feet/second</returns>
        public double GetWindNFpsIC() { return GetNEDWindFpsIC(VelocityType.eU); }

        /// <summary>
        /// Gets the initial wind velocity in local frame.
        /// </summary>
        /// <returns>Initial wind velocity eastwards in feet/second</returns>
        public double GetWindEFpsIC() { return GetNEDWindFpsIC(VelocityType.eV); }

        /// <summary>
        /// Gets the initial wind velocity in local frame.
        /// </summary>
        /// <returns>Initial wind velocity downwards in feet/second</returns>
        public double GetWindDFpsIC() { return GetNEDWindFpsIC(VelocityType.eW); }

        /// <summary>
        /// Gets the initial total wind velocity in feet/sec.
        /// </summary>
        /// <returns>Initial wind velocity in feet/second</returns>
        public double GetWindFpsIC()
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;

            return _vWIND_NED.GetMagnitude((int)VelocityType.eU - 1, (int)VelocityType.eV - 1);
        }

        /// <summary>
        /// Gets the initial wind direction.
        /// </summary>
        /// <returns>Initial wind direction in feet/second</returns>
        public double GetWindDirDegIC()
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;

            return _vWIND_NED.V == 0.0 ? 0.0
                                         : Math.Atan2(_vWIND_NED.V, _vWIND_NED.U) * Constants.radtodeg;
        }

        /// <summary>
        /// Gets the initial climb rate.
        /// </summary>
        /// <returns>Initial rate of climb in feet/second</returns>
        public double GetClimbRateFpsIC()
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            return _vt_NED.W;
        }

        /// <summary>
        /// Gets the initial body velocity
        /// </summary>
        /// <returns>Initial body velocity in feet/second.</returns>
        public Vector3D GetUVWFpsIC()
        {
            Matrix3D Tl2b = orientation.GetT();
            return Tl2b * vUVW_NED;
        }

        /// <summary>
        /// Gets the initial body axis X velocity.
        /// </summary>
        /// <returns>Initial body axis X velocity in feet/second.</returns>
        public double GetUBodyFpsIC() { return GetBodyVelFpsIC(VelocityType.eU); }

        /// <summary>
        /// Gets the initial body axis Y velocity.
        /// </summary>
        /// <returns>Initial body axis Y velocity in feet/second.</returns>
        public double GetVBodyFpsIC() { return GetBodyVelFpsIC(VelocityType.eV); }

        /// <summary>
        /// Gets the initial body axis Z velocity.
        /// </summary>
        /// <returns>Initial body axis Z velocity in feet/second.</returns>
        public double GetWBodyFpsIC() { return GetBodyVelFpsIC(VelocityType.eW); }

        /// <summary>
        /// Gets the initial local frame X (North) velocity.
        /// </summary>
        /// <returns>Initial local frame X (North) axis velocity in feet/second.</returns>
        public double GetVNorthFpsIC() { return vUVW_NED.U; }

        /// <summary>
        /// Gets the initial local frame Y (East) velocity.
        /// </summary>
        /// <returns>Initial local frame Y (East) axis velocity in feet/second.</returns>
        public double GetVEastFpsIC() { return vUVW_NED.V; }

        /// <summary>
        /// Gets the initial local frame Z (Down) velocity.
        /// </summary>
        /// <returns>Initial local frame Z (Down) axis velocity in feet/second.</returns>
        public double GetVDownFpsIC() { return vUVW_NED.W; }

        /// <summary>
        /// Gets the initial body rotation rate
        /// </summary>
        /// <returns>Initial body rotation rate in radians/second</returns>
        public Vector3D GetPQRRadpsIC() { return vPQR_body; }

        /// <summary>
        /// Gets the initial body axis roll rate.
        /// </summary>
        /// <returns>Initial body axis roll rate in radians/second</returns>
        public double GetPRadpsIC() { return vPQR_body.P; }

        /// <summary>
        ///  Gets the initial body axis pitch rate.
        /// </summary>
        /// <returns>Initial body axis pitch rate in radians/second</returns>
        public double GetQRadpsIC() { return vPQR_body.Q; }

        /// <summary>
        /// Gets the initial body axis yaw rate.
        /// </summary>
        /// <returns>Initial body axis yaw rate in radians/second</returns>
        public double GetRRadpsIC() { return vPQR_body.R; }

        /// <summary>
        /// Sets the initial flight path angle.
        /// </summary>
        /// <param name="gamma">Initial flight path angle in radians</param>
        public void SetFlightPathAngleRadIC(double gamma)
        { SetClimbRateFpsIC(vt * Math.Sin(gamma)); }

        /// <summary>
        /// Sets the initial angle of attack.
        /// When the AoA is modified, we need to update the angles theta and beta to
        /// keep the true airspeed amplitude, the climb rate and the heading unchanged.
        /// Beta will be modified if the aircraft roll angle is not null.
        /// </summary>
        /// <param name="alpha">Initial angle of attack in radians</param>
        public void SetAlphaRadIC(double alpha)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            calcThetaBeta(alpha, _vt_NED);
        }

        /// <summary>
        /// Sets the initial sideslip angle.
        /// When the beta angle is modified, we need to update the angles theta and psi
        /// to keep the true airspeed (amplitude and direction - including the climb rate)
        /// and the alpha angle unchanged. This may result in the aircraft heading (psi)
        /// being altered especially if there is cross wind.
        /// </summary>
        /// <param name="beta">Initial angle of sideslip in radians.</param>
        public void SetBetaRadIC(double bta)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D vOrient = orientation.GetEuler();

            beta = bta;
            double calpha = Math.Cos(alpha), salpha = Math.Sin(alpha);
            double cbeta = Math.Cos(beta), sbeta = Math.Sin(beta);
            double cphi = orientation.GetCosEuler().Phi, sphi = orientation.GetSinEuler().Phi;
            Matrix3D TphiInv = new Matrix3D(1.0, 0.0, 0.0,
                     0.0, cphi, -sphi,
                     0.0, sphi, cphi);

            Tw2b = new Matrix3D(
                calpha * cbeta, -calpha * sbeta, -salpha,
                  sbeta, cbeta, 0.0,
           salpha * cbeta, -salpha * sbeta, calpha);
            Tb2w = Tw2b.Transposed();

            Vector3D vf = TphiInv * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D v0xy = new Vector3D(_vt_NED.X, _vt_NED.Y, 0.0);
            Vector3D v1xy = new Vector3D(Math.Sqrt(v0xy.X * v0xy.X + v0xy.Y * v0xy.Y - vf.Y * vf.Y), vf.Y, 0.0);
            v0xy.Normalize();
            v1xy.Normalize();

            if (vf.X < 0.0) v0xy.X *= -1.0;

            double sinPsi = (v1xy * v0xy).Z;
            double cosPsi = Vector3D.Dot(v0xy, v1xy);
            vOrient.Psi = Math.Atan2(sinPsi, cosPsi);
            Matrix3D Tpsi = new Matrix3D(cosPsi, sinPsi, 0.0,
                 -sinPsi, cosPsi, 0.0,
                     0.0, 0.0, 1.0);

            Vector3D v2xz = Tpsi * _vt_NED;
            Vector3D vfxz = vf;
            v2xz.V = vfxz.V = 0.0;
            v2xz.Normalize();
            vfxz.Normalize();
            double sinTheta = (v2xz * vfxz).Y;
            vOrient.Theta = -Math.Asin(sinTheta);

            orientation = new Quaternion(vOrient);
        }

        /// <summary>
        /// Sets the initial roll angle.
        /// </summary>
        /// <param name="phi">Initial roll angle in radians</param>
        public void SetPhiRadIC(double phi) { SetEulerAngleRadIC((int)EulerAngleType.ePhi, phi); }

        /// <summary>
        /// Sets the initial pitch angle.
        /// </summary>
        /// <param name="theta">Initial pitch angle in radians</param>
        public void SetThetaRadIC(double theta) { SetEulerAngleRadIC((int)EulerAngleType.eTht, theta); }

        /// <summary>
        /// Sets the initial heading angle.
        /// </summary>
        /// <param name="psi">Initial heading angle in radians</param>
        public void SetPsiRadIC(double psi) { SetEulerAngleRadIC((int)EulerAngleType.ePsi, psi); }

        /// <summary>
        /// Sets the initial latitude.
        /// </summary>
        /// <param name="lat">Initial latitude in radians</param>
        public void SetLatitudeRadIC(double lat)
        {
            double altitude;

            lastLatitudeSet = LatitudeSet.setgeoc;

            switch (lastAltitudeSet)
            {
                case AltitudeSet.setagl:
                    altitude = GetAltitudeAGLFtIC();
                    position.Latitude = lat;
                    SetAltitudeAGLFtIC(altitude);
                    break;
                default:
                    position.Latitude = lat;
                    break;
            }
        }

        /// <summary>
        /// Sets the initial geodetic latitude.
        /// This method modifies the geodetic altitude in order to keep the altitude
        /// above sea level unchanged.
        /// </summary>
        /// <param name="glat">Initial geodetic latitude in radians</param>
        public void SetGeodLatitudeRadIC(double glat)
        {
            double h = position.GeodAltitude;
            double lon = position.Longitude;

            position.SetPositionGeodetic(lon, glat, h);
            lastLatitudeSet = LatitudeSet.setgeod;
        }

        /// <summary>
        /// Sets the initial longitude.
        /// </summary>
        /// <param name="lon">Initial longitude in radians</param>
        public void SetLongitudeRadIC(double lon)
        {
            double altitude;

            switch (lastAltitudeSet)
            {
                case AltitudeSet.setagl:
                    altitude = GetAltitudeAGLFtIC();
                    position.Longitude = lon;
                    SetAltitudeAGLFtIC(altitude);
                    break;
                default:
                    position.Longitude = lon;
                    break;
            }
        }

        /// <summary>
        /// Sets the target normal load factor.
        /// </summary>
        /// <param name="nlf">Normal load factor</param>
        public void SetTargetNlfIC(double nlf) { targetNlfIC = nlf; }

        /// <summary>
        /// Gets the initial flight path angle.
        /// If total velocity is zero, this function returns zero.
        /// </summary>
        /// <returns>Initial flight path angle in radians</returns>
        public double GetFlightPathAngleRadIC()
        { return (vt == 0.0) ? 0.0 : Math.Asin(GetClimbRateFpsIC() / vt); }

        /// <summary>
        /// Gets the initial angle of attack.
        /// </summary>
        /// <returns>Initial alpha in radians</returns>
        public double GetAlphaRadIC() { return alpha; }

        /// <summary>
        /// Gets the initial angle of sideslip.
        /// </summary>
        /// <returns>Initial sideslip angle in radians</returns>
        public double GetBetaRadIC() { return beta; }

        /// <summary>
        /// Gets the initial position
        /// </summary>
        /// <returns>Initial location</returns>
        public Location GetPosition() { return position; }

        /// <summary>
        /// Gets the initial latitude.
        /// </summary>
        /// <returns>Initial latitude in radians</returns>
        public double GetLatitudeRadIC() { return position.Latitude; }

        /// <summary>
        /// Gets the initial geodetic latitude.
        /// </summary>
        /// <returns>Initial geodetic latitude in radians</returns>
        public double GetGeodLatitudeRadIC() { return position.GeodLatitudeRad; }

        /// <summary>
        /// Gets the initial longitude.
        /// </summary>
        /// <returns>Initial longitude in radians</returns>
        public double GetLongitudeRadIC() { return position.Longitude; }

        /// <summary>
        /// Gets the initial orientation
        /// </summary>
        /// <returns>Initial orientation</returns>
        public Quaternion GetOrientation() { return orientation; }

        /// <summary>
        /// Gets the initial roll angle.
        /// </summary>
        /// <returns>Initial roll angle in radians</returns>
        public double GetPhiRadIC() { return orientation.GetEuler(Quaternion.EulerAngles.ePhi); }

        /// <summary>
        /// Gets the initial pitch angle.
        /// </summary>
        /// <returns>Initial pitch angle in radians</returns>
        public double GetThetaRadIC() { return orientation.GetEuler(Quaternion.EulerAngles.eTht); }

        /// <summary>
        /// Gets the initial heading angle.
        /// </summary>
        /// <returns>Initial heading angle in radians</returns>
        public double GetPsiRadIC() { return orientation.GetEuler(Quaternion.EulerAngles.ePsi); }

        /// <summary>
        /// Gets the initial speedset.
        /// </summary>
        /// <returns>Initial speedset</returns>
        public SpeedSet GetSpeedSet() { return lastSpeedSet; }

        /// <summary>
        /// Gets the target normal load factor set from IC.
        /// </summary>
        /// <returns>target normal load factor set from IC</returns>
        public double GetTargetNlfIC() { return targetNlfIC; }

        /// <summary>
        /// Loads the initial conditions.
        /// </summary>
        /// <param name="rstfile">The name of an initial conditions file</param>
        /// <param name="useStoredPath">true if the stored path to the IC file should be used</param>
        /// <returns>true if successful</returns>
        public bool Load(string rstfile, bool useStoredPath = true)
        {
            string init_file_name;

            if (useStoredPath && !Path.IsPathRooted(rstfile))
            {
                init_file_name = Path.Combine(fdmex.GetFullAircraftPath(), rstfile);
            }
            else
            {
                init_file_name = rstfile;
            }

            try
            {
                XmlTextReader reader = new XmlTextReader(init_file_name);
                XmlDocument document = new XmlDocument();
                // load the data into the dom
                document.Load(reader);
                XmlElement xParentEle = document.ParentNode as XmlElement;
                return Load(xParentEle, useStoredPath);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error("Exception reading IC reset file: " + e);
                }
                return false;
            }
        }
        public bool Load(XmlElement element, bool useStoredPath = true)
        {
            double version = double.MaxValue;
            bool result = false;

            if (element.HasAttribute("version"))
                double.TryParse(element.Attributes["version"].Value, out version);

            if (version == double.MaxValue)
            {
                result = Load_v1(element); // Default to the old version
            }
            else if (version >= 3.0)
            {
                string msg = "Only initialization file formats 1 and 2 are currently supported";
                log.Error(msg);
                throw new Exception(msg);
            }
            else if (version >= 2.0)
            {
                result = Load_v2(element);
            }
            else if (version >= 1.0)
            {
                result = Load_v1(element);
            }
            // Check to see if any engines are specified to be initialized in a running state
            XmlNodeList childNodes = element.GetElementsByTagName("running");
            foreach (var child_elem in childNodes)
            {
                XmlElement running_elem = child_elem as XmlElement;
                if (running_elem == null) continue;
                int engineNumber = int.Parse(running_elem.InnerText);
                enginesRunning |= engineNumber == -1 ? engineNumber : 1 << engineNumber;
            }
            return true;
        }

        /// <summary>
        /// Is an engine running ?
        /// </summary>
        /// <param name="n">index of the engine to be checked</param>
        /// <returns>true if the engine is running</returns>
        public bool IsEngineRunning(int n) { return (enginesRunning & (1 << n)) != 0; }

        /// <summary>
        /// Does initialization file call for trim ?
        /// </summary>
        /// <returns>Trim type, if any requested (version 1).</returns>
        public TrimMode TrimRequested() { return trimRequested; }


        // -------------------------------------------------------------------------------- //
#if TODO

        /// <summary>
        /// sets/gets calibrated airspeed initial condition in knots.
        /// </summary>
        [ScriptAttribute("ic/vc-kts", "(read/write) Calibrated airspeed initial condition in knots")]
        public double VcalibratedKtsIC
        {
            get { return vc * Constants.fpstokts; }
            set
            {

                if (getMachFromVcas(ref mach, value * Constants.ktstofps))
                {
                    //cout << "Mach: " << mach << endl;
                    lastSpeedSet = SpeedSet.setvc;
                    vc = value * Constants.ktstofps;
                    vt = mach * fdmex.Atmosphere.SoundSpeed;
                    ve = vt * Math.Sqrt(fdmex.Atmosphere.DensityRatio);
                    //cout << "Vt: " << vt*fpstokts << " Vc: " << vc*fpstokts << endl;
                }
                else
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error("Failed to get Mach number for given Vc and altitude, Vc unchanged.");
                        log.Error("Please mail the set of initial conditions used to apeden@earthlink.net");
                    }
                }
            }
        }

        /// <summary>
        /// sets/gets equivalent airspeed initial condition in knots.
        /// </summary>
        [ScriptAttribute("ic/ve-kts", "(read/write) Knots equivalent airspeed initial condition")]
        public double VequivalentKtsIC
        {
            get { return ve * Constants.fpstokts; }
            set
            {
                ve = value * Constants.ktstofps;
                lastSpeedSet = SpeedSet.setve;
                vt = ve * 1 / Math.Sqrt(fdmex.Atmosphere.DensityRatio);
                mach = vt / fdmex.Atmosphere.SoundSpeed;
                vc = calcVcas(mach);
            }
        }

        /// <summary>
        /// sets/gets ground speed initial condition in knots.
        /// </summary>
        [ScriptAttribute("ic/vg-kts", "(read/write) Ground speed initial condition in knots")]
        public double VgroundKtsIC
        {
            get { return vg * Constants.fpstokts; }
            set { VgroundFpsIC = value * Constants.ktstofps; }
        }

        /// <summary>
        /// sets/gets true airspeed initial condition in knots.
        /// </summary>
        [ScriptAttribute("ic/vt-kts", "(read/write) True airspeed initial condition in knots")]
        public double VtrueKtsIC
        {
            get { return vt * Constants.fpstokts; }
            set { VtrueFpsIC = value * Constants.ktstofps; }
        }

        /// <summary>
        /// sets/gets mach initial condition.
        /// </summary>
        [ScriptAttribute("ic/mach", "(read/write) Mach initial condition")]
        public double MachIC
        {
            get { return mach; }
            set
            {
                mach = value;
                lastSpeedSet = SpeedSet.setmach;
                vt = mach * fdmex.Atmosphere.SoundSpeed;
                vc = calcVcas(mach);
                ve = vt * Math.Sqrt(fdmex.Atmosphere.DensityRatio);
            }
        }

        /// <summary>
        /// sets/gets angle of attack initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/alpha-deg", "(read/write) Angle of attack initial condition in degrees")]
        public double AlphaDegIC
        {
            get { return alpha * Constants.radtodeg; }
            set { AlphaRadIC = value * Constants.degtorad; }
        }

        /// <summary>
        /// sets/gets angle of sideslip initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/beta-deg", "(read/write) Angle of sideslip initial condition in degrees")]
        public double BetaDegIC
        {
            get { return beta * Constants.radtodeg; }
            set { BetaRadIC = value * Constants.degtorad; }
        }


        /// <summary>
        /// sets/gets pitch angle initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/theta-deg", "(read/write) Pitch angle initial condition in degrees")]
        public double ThetaDegIC
        {
            get { return theta * Constants.radtodeg; }
            set { ThetaRadIC = value * Constants.degtorad; }
        }

        /// <summary>
        /// sets/gets the roll angle initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/phi-deg", "(read/write) Roll angle initial condition in degree")]
        public double PhiDegIC
        {
            get { return phi * Constants.radtodeg; }
            set { SetRollAngleRadIC(value * Constants.degtorad); }
        }

        /// <summary>
        /// sets/gets the heading angle initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/psi-true-deg", "(read/write) Heading angle initial condition in degrees")]
        public double PsiDegIC
        {
            get { return psi * Constants.radtodeg; }
            set { SetTrueHeadingRadIC(value * Constants.degtorad); }
        }

        /// <summary>
        /// sets/gets the climb rate initial condition in feet/minute.
        /// </summary>
        [ScriptAttribute("ic/roc-fpm", "(read/write) Rate of climb initial condition in feet/minute")]
        public double ClimbRateFpmIC
        {
            get { return hdot * 60; }
            set { SetClimbRateFpsIC(value / 60.0); }
        }

        /// <summary>
        /// sets/gets the flight path angle initial condition in degrees.
        /// </summary>
        [ScriptAttribute("ic/gamma-deg", "(read/write) Flightpath angle initial condition in degrees")]
        public double FlightPathAngleDegIC
        {
            get { return gamma * Constants.radtodeg; }
            set { SetFlightPathAngleRadIC(value * Constants.degtorad); }
        }

        /// <summary>
        /// sets/gets the altitude initial condition in feet.
        /// </summary>
        [ScriptAttribute("ic/h-sl-ft", "(read/write) Height above sea level initial condition in feet")]
        public double AltitudeFtIC
        {
            get { return altitude; }
            set
            {
#if TODO
                altitude = value;
                fdmex.Propagate.Altitude = altitude;
                fdmex.Atmosphere.Run(false);
                //lets try to make sure the user gets what they intended

                switch (lastSpeedSet)
                {
                    case SpeedSet.setned:
                    case SpeedSet.setuvw:
                    case SpeedSet.setvt:
                        VtrueKtsIC = vt * Constants.fpstokts;
                        break;
                    case SpeedSet.setvc:
                        VcalibratedKtsIC = vc * Constants.fpstokts;
                        break;
                    case SpeedSet.setve:
                        VequivalentKtsIC = ve * Constants.fpstokts;
                        break;
                    case SpeedSet.setmach:
                        MachIC = mach;
                        break;
                    case SpeedSet.setvg:
                        VgroundFpsIC = vg;
                        break;
                }
#endif
                throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
            }
        }

        /// <summary>
        /// sets/gets the initial Altitude above ground level.
        /// </summary>
        [ScriptAttribute("ic/h-agl-ft", "(read/write) Height above ground level initial condition in feet")]
        public double AltitudeAGLFtIC
        {
            get { return altitude - terrain_altitude; }
            set
            {
#if TODO
                fdmex.Propagate.DistanceAGL = value;
                altitude = fdmex.Propagate.Altitude;
                AltitudeFtIC = altitude;
#endif
                throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
            }

        }

        /// <summary>
        /// sets/gets the initial sea level radius from planet center
        /// </summary>
        [ScriptAttribute("ic/sea-level-radius-ft", "(read/write) Height above ground level initial condition in feet")]
        public double SeaLevelRadiusFtIC
        {
            get { return sea_level_radius; }
            set { sea_level_radius = value; }
        }

        /// <summary>
        /// sets/gets the initial terrain elevation.
        /// </summary>
        [ScriptAttribute("ic/terrain-altitude-ft", "(read/write) Terrain elevation above sea level in feet")]
        public double TerrainAltitudeFtIC
        {
            get { return terrain_altitude; }
            set { terrain_altitude = value; }
        }

        /// <summary>
        /// sets/gets  the initial latitude in degrees.
        /// </summary>
        [ScriptAttribute("ic/lat-gc-deg", "(read/write) Latitude initial condition in degrees")]
        public double LatitudeDegIC
        {
            get { return latitude * Constants.radtodeg; }
            set { latitude = value * Constants.degtorad; }
        }


        /// <summary>
        /// sets/gets the initial longitude in degrees.
        /// </summary>
        [ScriptAttribute("ic/long-gc-deg", "(read/write) Longitude initial condition in degrees")]
        public double LongitudeDegIC
        {
            get { return longitude * Constants.radtodeg; }
            set { longitude = value * Constants.degtorad; }
        }

        /// <summary>
        /// sets/gets the initial ground speed in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vg-fps", "(read/write) Ground speed initial condition in feet/second")]
        public double VgroundFpsIC
        {
            get { return vg; }
            set
            {
                double ua, va, wa;
                double vxz;

                vg = value;
                lastSpeedSet = SpeedSet.setvg;
                vnorth = vg * Math.Cos(psi); veast = vg * Math.Sin(psi); vdown = 0;
                calcUVWfromNED();
                ua = u + uw; va = v + vw; wa = w + ww;
                vt = Math.Sqrt(ua * ua + va * va + wa * wa);
                alpha = beta = 0;
                vxz = Math.Sqrt(u * u + w * w);
                if (w != 0) alpha = Math.Atan2(w, u);
                if (vxz != 0) beta = Math.Atan2(v, vxz);
                mach = vt / fdmex.Atmosphere.SoundSpeed;
                vc = calcVcas(mach);
                ve = vt * Math.Sqrt(fdmex.Atmosphere.DensityRatio);
            }
        }

        /// <summary>
        /// sets/gets the initial true airspeed in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vt-fps", "(read/write) True airspeed initial condition in feet/second")]
        public double VtrueFpsIC
        {
            get { return vt; }
            set
            {
                vt = value;
                lastSpeedSet = SpeedSet.setvt;
                mach = vt / fdmex.Atmosphere.SoundSpeed;
                vc = calcVcas(mach);
                ve = vt * Math.Sqrt(fdmex.Atmosphere.DensityRatio);
            }
        }

        /// <summary>
        /// sets/gets the initial body axis X velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/u-fps", "Body frame x-axis velocity initial condition in feet/second")]
        public double UBodyFpsIC
        {
            get
            {
                if (lastSpeedSet == SpeedSet.setvg)
                    return u;
                else
                    return vt * calpha * cbeta - uw;
            }
            set
            {
                u = value;
                vt = Math.Sqrt(u * u + v * v + w * w);
                lastSpeedSet = SpeedSet.setuvw;
            }
        }

        /// <summary>
        /// sets/gets the initial body axis Y velocityin feet/second.
        /// </summary>
        [ScriptAttribute("ic/v-fps", "Body frame y-axis velocity initial condition in feet/second")]
        public double VBodyFpsIC
        {
            get
            {
                if (lastSpeedSet == SpeedSet.setvg)
                    return v;
                else
                {
                    return vt * sbeta - vw;
                }
            }
            set
            {
                v = value;
                vt = Math.Sqrt(u * u + v * v + w * w);
                lastSpeedSet = SpeedSet.setuvw;
            }
        }

        /// <summary>
        /// sets/gets the initial body axis Z velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/w-fps", "Body frame z-axis velocity initial condition in feet/second")]
        public double WBodyFpsIC
        {
            get
            {
                if (lastSpeedSet == SpeedSet.setvg)
                    return w;
                else
                    return vt * salpha * cbeta - ww;
            }
            set
            {
                w = value;
                vt = Math.Sqrt(u * u + v * v + w * w);
                lastSpeedSet = SpeedSet.setuvw;
            }
        }


        public void SetVnorthFpsIC(double tt)
        {
            vnorth = tt;
            calcUVWfromNED();
            vt = Math.Sqrt(u * u + v * v + w * w);
            lastSpeedSet = SpeedSet.setned;
        }

        public void SetVeastFpsIC(double tt)
        {
            veast = tt;
            calcUVWfromNED();
            vt = Math.Sqrt(u * u + v * v + w * w);
            lastSpeedSet = SpeedSet.setned;
        }

        public void SetVdownFpsIC(double tt)
        {
            vdown = tt;
            calcUVWfromNED();
            vt = Math.Sqrt(u * u + v * v + w * w);
            SetClimbRateFpsIC(-1 * vdown);
            lastSpeedSet = SpeedSet.setned;
        }
        /// <summary>
        /// Gets the initial body axis X wind velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-bx-fps", "(read) Wind velocity initial condition in Body X frame in feet/second")]
        public double WindUFpsIC { get { return uw; } }

        /// <summary>
        /// Gets the initial body axis Y wind velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-by-fps", "(read) Wind velocity initial condition in Body Y frame in feet/second")]
        public double WindVFpsIC { get { return vw; } }

        /// <summary>
        /// Gets the initial body axis Z wind velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-bz-fps", "(read) Wind velocity initial condition in Body Z frame in feet/second")]
        public double WindWFpsIC { get { return ww; } }

        /// <summary>
        /// sets/gets the initial body axis Z velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-north-fps", "(read) Wind northward velocity initial condition in feet/second")]
        public double WindNFpsIC { get { return wnorth; } }

        /// <summary>
        /// sets/gets the initial body axis Z velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-east-fps", "(read) Wind eastward velocity initial condition in feet/second")]
        public double WindEFpsIC { get { return weast; } }

        /// <summary>
        /// sets/gets the initial body axis Z velocity in feet/second.
        /// </summary>
        [ScriptAttribute("ic/vw-down-fps", "(read) Wind downward velocity initial condition in feet/second")]
        public double WindDFpsIC { get { return wdown; } }

        /// <summary>
        /// gets the initial total wind velocity in feet/sec.
        /// </summary>
        [ScriptAttribute("ic/vw-mag-fps", "(read) Wind velocity magnitude initial condition in feet/sec")]
        public double WindFpsIC { get { return Math.Sqrt(wnorth * wnorth + weast * weast); } }

        /// <summary>
        /// gets/sets the initial body axis roll rate in radians/second.
        /// </summary>
        [ScriptAttribute("ic/p-rad_sec", "(read/write) Roll rate initial condition in radians/second")]
        public double PRadpsIC
        {
            get { return p; }
            set { p = value; }
        }

        /// <summary>
        /// gets/sets the initial body axis pitch rate in radians/second.
        /// </summary>
        [ScriptAttribute("ic/q-rad_sec", "(read/write) Pitch rate initial condition in radians/second")]
        public double QRadpsIC
        {
            get { return q; }
            set { q = value; }
        }

        /// <summary>
        /// gets/sets the initial body axis yaw rate in radians/second.
        /// </summary>
        [ScriptAttribute("ic/r-rad_sec", "(read/write) Yaw rate initial condition in radians/second")]
        public double RRadpsIC
        {
            get { return r; }
            set { r = value; }
        }

        [ScriptAttribute("ic/alpha-rad", "The initial alpha in radians.")]
        public double AlphaRadIC
        {
            get { return alpha; }
            set
            {
                alpha = value;
                salpha = Math.Sin(alpha); calpha = Math.Cos(alpha);
                getTheta();
            }
        }

        [ScriptAttribute("ic/beta-rad", "The initial beta in radians.")]
        public double BetaRadIC
        {
            get { return beta; }
            set
            {
                beta = value;
                sbeta = Math.Sin(beta); cbeta = Math.Cos(beta);
                getTheta();
            }
        }

        [ScriptAttribute("ic/theta-rad", "The initial pitch angle in radians.")]
        public double ThetaRadIC
        {
            get { return theta; }
            set
            {
                theta = value;
                stheta = Math.Sin(theta); ctheta = Math.Cos(theta);
                getAlpha();
            }
        }

        [ScriptAttribute("ic/lat-gc-rad", "The initial latitude in radians.")]
        public double LatitudeRadIC
        {
            get { return latitude; }
            set { latitude = value; }
        }

        [ScriptAttribute("ic/long-gc-rad", "The initial longitude in radians.")]
        public double LongitudeRadIC
        {
            get { return longitude; }
            set { longitude = value; }
        }
#endif
#if DELETEME
        public void Load(XmlElement root)
        {
            Load(root, true);
        }

        public void Load(XmlElement root, bool mustRun)
        {
            double tmp;
            try
            {
                foreach (XmlNode currentNode in root.ChildNodes)
                {
                    if (currentNode.NodeType == XmlNodeType.Element)
                    {
                        XmlElement currentElement = (XmlElement)currentNode;

                        if (currentElement.LocalName.Equals("ubody"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            UBodyFpsIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("vbody"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            VBodyFpsIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("wbody"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            WBodyFpsIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("latitude"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            LatitudeDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("longitude"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            LongitudeDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("phi"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            PhiDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("theta"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            ThetaDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("psi"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            PsiDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("alpha"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            AlphaDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("beta"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            BetaDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("gamma"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            FlightPathAngleDegIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("roc"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            ClimbRateFpmIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("altitude"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT");
                            AltitudeFtIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("winddir"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "DEG");
                            SetWindDirDegIC(tmp);
                        }
                        else if (currentElement.LocalName.Equals("vwind"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            SetWindMagKtsIC(tmp);
                        }
                        else if (currentElement.LocalName.Equals("hwind"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "KTS");
                            SetHeadWindKtsIC(tmp);
                        }
                        else if (currentElement.LocalName.Equals("xwind"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "KTS");
                            SetCrossWindKtsIC(tmp);
                        }
                        else if (currentElement.LocalName.Equals("vc"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            VcalibratedKtsIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("mach"))
                        {
                            tmp = FormatHelper.ValueAsNumber(currentElement);
                            MachIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("vground"))
                        {
                            tmp = FormatHelper.ValueAsNumberConvertTo(currentElement, "FT/SEC");
                            VgroundKtsIC = tmp;
                        }
                        else if (currentElement.LocalName.Equals("running"))
                        {
                            throw new NotImplementedException("TODO. Not implemented running");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error("Exception reading IC reset file: " + e);
                }
            }

            if (mustRun) fdmex.RunIC();
        }
#endif
        public virtual void Bind(PropertyManager PropertyManager)
        {
            fdmex.PropertyManager.Bind("", this);
        }

        public virtual void Unbind()
        {
            fdmex.PropertyManager.Unbind("", this);
        }


        private Vector3D vUVW_NED;
        private Vector3D vPQR_body;
        private Location position = new Location();
        private Quaternion orientation;
        private double vt;
        private double targetNlfIC;

        private Matrix3D Tw2b, Tb2w;
        private double alpha, beta;
        private double epa;

        private SpeedSet lastSpeedSet;
        private AltitudeSet lastAltitudeSet;
        private LatitudeSet lastLatitudeSet;
        private int enginesRunning;
        private TrimMode trimRequested;

        private FDMExecutive fdmex;
        private Atmosphere Atmosphere;
        private Aircraft Aircraft;

        private bool Load_v1(XmlElement document)
        {
            bool result = true;

            XmlElement elem = document.FindElement("longitude");
            if (elem != null)
                SetLongitudeRadIC(FormatHelper.ValueAsNumberConvertTo(elem, "RAD"));

            if (document.FindElement("elevation", out elem))
                SetTerrainElevationFtIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT"));

            if (document.FindElement("altitude", out elem))// This is feet above ground level
                SetAltitudeAGLFtIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT"));
            if (document.FindElement("altitudeAGL", out elem)) // This is feet above ground level
                SetAltitudeAGLFtIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT"));
            if (document.FindElement("altitudeMSL", out elem)) // This is feet above sea level
                SetAltitudeASLFtIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT"));

            result = LoadLatitude(document);

            Vector3D vOrient = orientation.GetEuler();

            if (document.FindElement("phi", out elem))
                vOrient.Phi = FormatHelper.ValueAsNumberConvertTo(elem, "RAD");
            if (document.FindElement("theta", out elem))
                vOrient.Theta = FormatHelper.ValueAsNumberConvertTo(elem, "RAD");
            if (document.FindElement("psi", out elem))
                vOrient.Psi = FormatHelper.ValueAsNumberConvertTo(elem, "RAD");

            orientation = new Quaternion(vOrient);

            if (document.FindElement("ubody", out elem))
                SetUBodyFpsIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT/SEC"));
            if (document.FindElement("vbody", out elem))
                SetVBodyFpsIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT/SEC"));
            if (document.FindElement("wbody", out elem))
                SetWBodyFpsIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT/SEC"));
            if (document.FindElement("vnorth", out elem))
                SetVNorthFpsIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT/SEC"));
            if (document.FindElement("veast", out elem))
                SetVEastFpsIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT/SEC"));
            if (document.FindElement("vdown", out elem))
                SetVDownFpsIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT/SEC"));
            if (document.FindElement("vc", out elem))
                SetVcalibratedKtsIC(FormatHelper.ValueAsNumberConvertTo(elem, "KTS"));
            if (document.FindElement("vt", out elem))
                SetVtrueKtsIC(FormatHelper.ValueAsNumberConvertTo(elem, "KTS"));
            if (document.FindElement("mach", out elem))
                SetMachIC(FormatHelper.ValueAsNumber(elem));
            if (document.FindElement("gamma", out elem))
                SetFlightPathAngleDegIC(FormatHelper.ValueAsNumberConvertTo(elem, "DEG"));
            if (document.FindElement("roc", out elem))
                SetClimbRateFpsIC(FormatHelper.ValueAsNumberConvertTo(elem, "FT/SEC"));
            if (document.FindElement("vground", out elem))
                SetVgroundKtsIC(FormatHelper.ValueAsNumberConvertTo(elem, "KTS"));
            if (document.FindElement("alpha", out elem))
                SetAlphaDegIC(FormatHelper.ValueAsNumberConvertTo(elem, "DEG"));
            if (document.FindElement("beta", out elem))
                SetBetaDegIC(FormatHelper.ValueAsNumberConvertTo(elem, "DEG"));
            if (document.FindElement("vwind", out elem))
                SetWindMagKtsIC(FormatHelper.ValueAsNumberConvertTo(elem, "KTS"));
            if (document.FindElement("winddir", out elem))
                SetWindDirDegIC(FormatHelper.ValueAsNumberConvertTo(elem, "DEG"));
            if (document.FindElement("hwind", out elem))
                SetHeadWindKtsIC(FormatHelper.ValueAsNumberConvertTo(elem, "KTS"));
            if (document.FindElement("xwind", out elem))
                SetCrossWindKtsIC(FormatHelper.ValueAsNumberConvertTo(elem, "KTS"));
            if (document.FindElement("targetNlf", out elem))
                SetTargetNlfIC(FormatHelper.ValueAsNumber(elem));
            if (document.FindElement("trim", out elem))
                SetTrimRequest(elem.InnerText);

            // Refer to Stevens and Lewis, 1.5-14a, pg. 49.
            // This is the rotation rate of the "Local" frame, expressed in the local frame.
            Matrix3D Tl2b = orientation.GetT();
            double radInv = 1.0 / position.Radius;
            Vector3D vOmegaLocal = new Vector3D(radInv * vUVW_NED.East,
                                                -radInv * vUVW_NED.North,
                                                -radInv * vUVW_NED.East * position.TanLatitude);

            vPQR_body = Tl2b * vOmegaLocal;

            return result;
        }
        private bool Load_v2(XmlElement document)
        {
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
        }

        private void InitializeIC()
        {
            alpha = beta = 0.0;
            epa = 0.0;
            // FIXME: Since FGDefaultGroundCallback assumes the Earth is spherical, so
            // must FGLocation. However this should be updated according to the assumption
            // made by the actual callback.

            // double a = fdmex.GetInertial().GetSemimajor();
            // double b = fdmex.GetInertial().GetSemiminor();
            double a = fdmex.GetInertial().GetRefRadius();
            double b = fdmex.GetInertial().GetRefRadius();

            position.SetEllipse(a, b);

            position.SetPositionGeodetic(0.0, 0.0, 0.0);

            orientation = new Quaternion(0.0, 0.0, 0.0);
            vUVW_NED = Vector3D.Zero;
            vPQR_body = Vector3D.Zero;
            vt = 0;

            targetNlfIC = 1.0;

            Tw2b = new Matrix3D(1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0);
            Tb2w = new Matrix3D(1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0);

            lastSpeedSet = SpeedSet.setvt;
            lastAltitudeSet = AltitudeSet.setasl;
            lastLatitudeSet = LatitudeSet.setgeoc;
            enginesRunning = 0;
            trimRequested = TrimMode.None;
        }

        /// <summary>
        /// Modifies the body frame orientation.
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="angle"></param>
        private void SetEulerAngleRadIC(int idx, double angle)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Matrix3D Tl2b = orientation.GetT();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;
            Vector3D _vUVW_BODY = Tl2b * vUVW_NED;
            Vector3D vOrient = orientation.GetEuler();

            vOrient[idx - 1] = angle;
            orientation = new Quaternion(vOrient);

            if ((lastSpeedSet != SpeedSet.setned) && (lastSpeedSet != SpeedSet.setvg))
            {
                Matrix3D newTb2l = orientation.GetTInv();
                vUVW_NED = newTb2l * _vUVW_BODY;
                _vt_NED = vUVW_NED + _vWIND_NED;
                vt = _vt_NED.Magnitude();
            }

            calcAeroAngles(_vt_NED);
        }

        /// <summary>
        /// Modifies an aircraft velocity component (eU, eV or eW) in the body frame. The
        /// true airspeed is modified accordingly. If there is some wind, the airspeed
        /// direction modification may differ from the body velocity modification.
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="vel"></param>
        private void SetBodyVelFpsIC(VelocityType idx, double vel)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Matrix3D Tl2b = orientation.GetT();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vUVW_BODY = Tl2b * vUVW_NED;
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;

            _vUVW_BODY[(int)idx - 1] = vel;
            vUVW_NED = Tb2l * _vUVW_BODY;
            _vt_NED = vUVW_NED + _vWIND_NED;
            vt = _vt_NED.Magnitude();

            calcAeroAngles(_vt_NED);

            lastSpeedSet = SpeedSet.setuvw;
        }

        /// <summary>
        /// Modifies an aircraft velocity component (eX, eY or eZ) in the local NED frame.
        /// The true airspeed is modified accordingly. If there is some wind, the airspeed
        /// direction modification may differ from the local velocity modification.
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="vel"></param>
        private void SetNEDVelFpsIC(VelocityType idx, double vel)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;

            vUVW_NED[(int)idx - 1] = vel;
            _vt_NED = vUVW_NED + _vWIND_NED;
            vt = _vt_NED.Magnitude();

            calcAeroAngles(_vt_NED);

            lastSpeedSet = SpeedSet.setned;
        }

        private double GetBodyWindFpsIC(VelocityType idx)
        {
            Matrix3D Tl2b = orientation.GetT();
            Vector3D _vt_BODY = Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vUVW_BODY = Tl2b * vUVW_NED;
            Vector3D _vWIND_BODY = _vt_BODY - _vUVW_BODY;

            return _vWIND_BODY[(int)idx - 1];
        }

        private double GetNEDWindFpsIC(VelocityType idx)
        {
            Matrix3D Tb2l = orientation.GetTInv();
            Vector3D _vt_NED = Tb2l * Tw2b * new Vector3D(vt, 0.0, 0.0);
            Vector3D _vWIND_NED = _vt_NED - vUVW_NED;

            return _vWIND_NED[(int)idx - 1];
        }

        private double GetBodyVelFpsIC(VelocityType idx)
        {
            Matrix3D Tl2b = orientation.GetT();
            Vector3D _vUVW_BODY = Tl2b * vUVW_NED;

            return _vUVW_BODY[(int)idx - 1];
        }

        /// <summary>
        /// Updates alpha and beta according to the aircraft true airspeed in the local
        /// NED frame.
        /// </summary>
        /// <param name="_vt_NED"></param>
        private void calcAeroAngles(Vector3D _vt_NED)
        {
            Matrix3D Tl2b = orientation.GetT();
            Vector3D _vt_BODY = Tl2b * _vt_NED;
            double ua = _vt_BODY.X;
            double va = _vt_BODY.Y;
            double wa = _vt_BODY.Z;
            double uwa = Math.Sqrt(ua * ua + wa * wa);
            double calpha, cbeta;
            double salpha, sbeta;

            alpha = beta = 0.0;
            calpha = cbeta = 1.0;
            salpha = sbeta = 0.0;

            if (wa != 0)
                alpha = Math.Atan2(wa, ua);

            // alpha cannot be constrained without updating other informations like the
            // true speed or the Euler angles. Otherwise we might end up with an
            // inconsistent state of the aircraft.
            /* alpha = Constrain(fdmex.GetAerodynamics().GetAlphaCLMin(), alpha,
                              fdmex.GetAerodynamics().GetAlphaCLMax());
             */

            if (va != 0)
                beta = Math.Atan2(va, uwa);

            if (uwa != 0)
            {
                calpha = ua / uwa;
                salpha = wa / uwa;
            }

            if (vt != 0)
            {
                cbeta = uwa / vt;
                sbeta = va / vt;
            }

            Tw2b = new Matrix3D(
                calpha * cbeta, -calpha * sbeta, -salpha,
                  sbeta, cbeta, 0.0,
           salpha * cbeta, -salpha * sbeta, calpha);
            Tb2w = Tw2b.Transposed();
        }

        /// <summary>
        /// When the AoA is modified, we need to update the angles theta and beta to
        /// keep the true airspeed amplitude, the climb rate and the heading unchanged.
        /// Beta will be modified if the aircraft roll angle is not null.
        /// </summary>
        /// <param name="alfa"></param>
        /// <param name="_vt_NED"></param>
        private void calcThetaBeta(double alfa, Vector3D _vt_NED)
        {
            Vector3D vOrient = orientation.GetEuler();
            double calpha = Math.Cos(alfa), salpha = Math.Sin(alfa);
            double cpsi = orientation.GetCosEuler(Quaternion.EulerAngles.ePsi), spsi = orientation.GetSinEuler(Quaternion.EulerAngles.ePsi);
            double cphi = orientation.GetCosEuler(Quaternion.EulerAngles.ePhi), sphi = orientation.GetSinEuler(Quaternion.EulerAngles.ePhi);
            Matrix3D Tpsi = new Matrix3D(cpsi, spsi, 0.0,
                  -spsi, cpsi, 0.0,
                     0.0, 0.0, 1.0);
            Matrix3D Tphi = new Matrix3D(1.0, 0.0, 0.0,
                  0.0, cphi, sphi,
                  0.0, -sphi, cphi);
            Matrix3D Talpha = new Matrix3D(calpha, 0.0, salpha,
                         0.0, 1.0, 0.0,
                    -salpha, 0.0, calpha);

            Vector3D v0 = Tpsi * _vt_NED;
            Vector3D n = (Talpha * Tphi).Transposed() * new Vector3D(0.0, 0.0, 1.0);
            Vector3D y = new Vector3D(0.0, 1.0, 0.0);
            Vector3D u = y - Vector3D.Dot(y, n) * n;
            Vector3D p = y * n;

            if (Vector3D.Dot(p, v0) < 0) p *= -1.0;
            p.Normalize();

            u *= Vector3D.Dot(v0, y) / Vector3D.Dot(u, y);

            // There are situations where the desired alpha angle cannot be obtained. This
            // is not a limitation of the algorithm but is due to the mathematical problem
            // not having a solution. This can only be cured by limiting the alpha angle
            // or by modifying an additional angle (psi ?). Since this is anticipated to
            // be a pathological case (mainly when a high roll angle is required) this
            // situation is not addressed below. However if there are complaints about the
            // following error being raised too often, we might need to reconsider this
            // position.
            if (Vector3D.Dot(v0, v0) < Vector3D.Dot(u, u))
            {
                log.Error("Cannot modify angle 'alpha' from " + alpha + " to " + alfa);
                return;
            }

            Vector3D v1 = u + Math.Sqrt(Vector3D.Dot(v0, v0) - Vector3D.Dot(u, u)) * p;

            Vector3D v0xz = new Vector3D(v0.U, 0.0, v0.W);
            Vector3D v1xz = new Vector3D(v1.U, 0.0, v1.W);
            v0xz.Normalize();
            v1xz.Normalize();
            double sinTheta = (v1xz * v0xz).Y;
            vOrient.Theta = Math.Asin(sinTheta);

            orientation = new Quaternion(vOrient);

            Matrix3D Tl2b = orientation.GetT();
            Vector3D v2 = Talpha * Tl2b * _vt_NED;

            alpha = alfa;
            beta = Math.Atan2(v2.V, v2.U);
            double cbeta = 1.0, sbeta = 0.0;
            if (vt != 0.0)
            {
                cbeta = v2.U / vt;
                sbeta = v2.V / vt;
            }
            Tw2b = new Matrix3D(
                calpha * cbeta, -calpha * sbeta, -salpha,
                  sbeta, cbeta, 0.0,
           salpha * cbeta, -salpha * sbeta, calpha);
            Tb2w = Tw2b.Transposed();
        }

        private bool LoadLatitude(XmlElement position_el)
        {
            XmlElement latitude_el = position_el.FindElement("latitude");
            if (latitude_el != null)
            {
                double latitude = FormatHelper.ValueAsNumberConvertTo(latitude_el, "RAD");

                if (Math.Abs(latitude) > 0.5 * Math.PI)
                {
                    string unit_type = latitude_el.GetAttribute("unit");
                    if (string.IsNullOrEmpty(unit_type)) unit_type = "RAD";

                    string msg = "The latitude value " + FormatHelper.ValueAsNumber(latitude_el) + " " + unit_type +
                            " is outside the range [";
                    if (unit_type == "DEG")
                        msg += "-90 DEG ; +90 DEG]";
                    else
                        msg += "-PI/2 RAD; +PI/2 RAD]";
                    log.Error(msg);
                    return false;
                }

                string lat_type = latitude_el.GetAttribute("type");

                if (lat_type == "geod" || lat_type == "geodetic")
                    SetGeodLatitudeRadIC(latitude);
                else
                {
                    position.Latitude = latitude;
                    lastLatitudeSet = LatitudeSet.setgeoc;
                }
            }
             return true;
        }
        private void SetTrimRequest(string trim)
        {
            string trimOption = trim.ToLowerInvariant();
            if (trimOption == "1")
                trimRequested = TrimMode.Ground;  // For backwards compatabiity
            else if (trimOption == "longitudinal")
                trimRequested = TrimMode.Longitudinal;
            else if (trimOption == "full")
                trimRequested = TrimMode.Full;
            else if (trimOption == "ground")
                trimRequested = TrimMode.Ground;
            else if (trimOption == "pullup")
                trimRequested = TrimMode.Pullup;
            else if (trimOption == "custom")
                trimRequested = TrimMode.Custom;
            else if (trimOption == "turn")
                trimRequested = TrimMode.Turn;
        }
        protected void Debug(int from) { throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM"); }
    }
}
