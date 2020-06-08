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
    using System.Collections.Generic;
    using System.Text;

    // Import log4net classes.
    using log4net;

    using JSBSim.Models;
    using CommonUtils.MathLib;
    using JSBSim.MathValues;

    /// <summary>
    ///  Models atmospheric disturbances: winds, gusts, turbulence, downbursts, etc.
    /// 
    ///     <h2>Turbulence</h2>
    ///    ///     Various turbulence models are available. They are specified
    ///     via the property <tt>atmosphere/turb-type</tt>. The following models are
    ///     available:
    ///     - 0: ttNone (turbulence disabled)
    ///     - 1: ttStandard
    ///     - 2: ttCulp
    ///     - 3: ttMilspec (Dryden spectrum)
    ///     - 4: ttTustin (Dryden spectrum)
    /// 
    ///     The Milspec and Tustin models are described in the Yeager report cited
    ///     below.  They both use a Dryden spectrum model whose parameters (scale
    ///     lengths and intensities) are modelled according to MIL-F-8785C. Parameters
    ///     are modelled differently for altitudes below 1000ft and above 2000ft, for
    ///     altitudes in between they are interpolated linearly.
    /// 
    ///     The two models differ in the implementation of the transfer functions
    ///     described in the milspec.
    /// 
    ///     To use one of these two models, set <tt>atmosphere/turb-type</tt> to 4
    ///     resp. 5, and specify values for
    ///     <tt>atmosphere/turbulence/milspec/windspeed_at_20ft_AGL-fps</tt> and
    ///     <tt>atmosphere/turbulence/milspec/severity</tt> (the latter corresponds to
    ///     the probability of exceedence curves from Fig.&nbsp;7 of the milspec,
    ///     allowable range is 0 (disabled) to 7). <tt>atmosphere/psiw-rad</tt> is
    ///     respected as well; note that you have to specify a positive wind magnitude
    ///     to prevent psiw from being reset to zero.
    /// 
    ///     Reference values (cf. figures 7 and 9 from the milspec):
    ///     <table>
    ///       <tr><td><b>Intensity</b></td>
    ///           <td><b><tt>windspeed_at_20ft_AGL-fps</tt></b></td>
    ///           <td><b><tt>severity</tt></b></td></tr>
    ///       <tr><td>light</td>
    ///           <td>25 (15 knots)</td>
    ///           <td>3</td></tr>
    ///       <tr><td>moderate</td>
    ///           <td>50 (30 knots)</td>
    ///           <td>4</td></tr>
    ///       <tr><td>severe</td>
    ///           <td>75 (45 knots)</td>
    ///           <td>6</td></tr>
    ///     </table>
    /// 
    ///     <h2>Cosine Gust</h2>
    ///     A one minus cosine gust model is available. This permits a configurable,
    ///     predictable gust to be input to JSBSim for testing handling and
    ///     dynamics. Here is how a gust can be entered in a script:
    /// 
    ///     ~~~{.xml}
    ///     <event name="Introduce gust">
    ///       <condition> simulation/sim-time-sec ge 10 </condition>
    ///       <set name="atmosphere/cosine-gust/startup-duration-sec" value="5"/>
    ///       <set name="atmosphere/cosine-gust/steady-duration-sec" value="1"/>
    ///       <set name="atmosphere/cosine-gust/end-duration-sec" value="5"/>
    ///       <set name="atmosphere/cosine-gust/magnitude-ft_sec" value="30"/>
    ///       <set name="atmosphere/cosine-gust/frame" value="2"/>
    ///       <set name="atmosphere/cosine-gust/X-velocity-ft_sec" value="-1"/>
    ///       <set name="atmosphere/cosine-gust/Y-velocity-ft_sec" value="0"/>
    ///       <set name="atmosphere/cosine-gust/Z-velocity-ft_sec" value="0"/>
    ///       <set name="atmosphere/cosine-gust/start" value="1"/>
    ///       <notify/>
    ///     </event>
    ///     ~~~
    /// 
    ///     The x, y, z velocity components are meant to define the direction vector.
    ///     The vector will be normalized by the routine, so it does not need to be a
    ///     unit vector.
    /// 
    ///     The startup duration is the time it takes to build up to full strength
    ///     (magnitude-ft_sec) from zero. Steady duration is the time the gust stays at
    ///     the specified magnitude. End duration is the time it takes to dwindle to
    ///     zero from the specified magnitude. The start and end transients are in a
    ///     smooth cosine shape.
    /// 
    ///     The frame is specified from the following enum:
    /// 
    ///     enum eGustFrame {gfNone=0, gfBody, gfWind, gfLocal};
    /// 
    ///     That is, if you specify the X, Y, Z gust direction vector in the body frame,
    ///     frame will be "1". If the X, Y, and Z gust direction vector is in the Wind
    ///     frame, use frame = 2. If you specify the gust direction vector in the local
    ///     frame (N-E-D) use frame = 3. Note that an internal local frame direction
    ///     vector is created based on the X, Y, Z direction vector you specify and the
    ///     frame *at the time the gust is begun*. The direction vector is not updated
    ///     after the initial creation. This is to keep the gust at the same direction
    ///     independent of aircraft dynamics.
    /// 
    ///     The gust is triggered when the property atmosphere/cosine-gust/start is set
    ///     to 1. It can be used repeatedly - the gust resets itself after it has
    ///     completed.
    /// 
    ///     The cosine gust is global: it affects the whole world not just the vicinity
    ///     of the aircraft.
    /// 
    ///     @see Yeager, Jessie C.: "Implementation and Testing of Turbulence Models for
    ///          the F18-HARV" (<a
    ///          href="http://ntrs.nasa.gov/archive/nasa/casi.ntrs.nasa.gov/19980028448_1998081596.pdf">
    ///          pdf</a>), NASA CR-1998-206937, 1998
    /// 
    ///  @see MIL-F-8785C: Military Specification: Flying Qualities of Piloted Aircraft
    /// </summary>
    public class Winds : Model
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

        public Winds(FDMExecutive exec)
            : base(exec)
        {
            Name = "FGWinds";

            MagnitudedAccelDt = MagnitudeAccel = Magnitude = TurbDirection = 0.0;
            SetTurbType(tType.ttMilspec);
            TurbGain = 1.0;
            TurbRate = 10.0;
            Rhythmicity = 0.1;
            spike = target_time = strength = 0.0;
            wind_from_clockwise = 0.0;
            psiw = 0.0;

            vGustNED = Vector3D.Zero;
            vTurbulenceNED = Vector3D.Zero;
            vCosineGust = Vector3D.Zero;

            // Milspec turbulence model
            windspeed_at_20ft = 0.0;
            probability_of_exceedence_index = 0;
            POE_Table = new Table(7, 12);
            // this is Figure 7 from p. 49 of MIL-F-8785C
            // rows: probability of exceedance curve index, cols: altitude in ft
            double[,] data = new double[8, 13]  {
               { double.NaN, 500.0 ,  1750.0 ,  3750.0 ,  7500.0 ,  15000.0 ,  25000.0 ,  35000.0 ,  45000.0 ,  55000.0 ,  65000.0 ,  75000.0 ,  80000.0 },
               { 1 ,  3.2 ,  2.2 ,  1.5 ,  0.0 ,  0.0 ,  0.0 ,  0.0 ,  0.0 ,  0.0 ,  0.0 ,  0.0 ,  0.0 },
               { 2 ,  4.2 ,  3.6 ,  3.3 ,  1.6 ,  0.0 ,  0.0 ,  0.0 ,  0.0 ,  0.0 ,  0.0 ,  0.0 ,  0.0 },
               { 3 ,  6.6 ,  6.9 ,  7.4 ,  6.7 ,  4.6 ,  2.7 ,  0.4 ,  0.0 ,  0.0 ,  0.0 ,  0.0 ,  0.0 },
               { 4 ,  8.6 ,  9.6 ,  10.6 ,  10.1 ,  8.0 ,  6.6 ,  5.0 ,  4.2 ,  2.7 ,  0.0 ,  0.0 ,  0.0 },
               { 5 ,  11.8 ,  13.0 ,  16.0 ,  15.1 ,  11.6 ,  9.7 ,  8.1 ,  8.2 ,  7.9 ,  4.9 ,  3.2 ,  2.1 },
               { 6 ,  15.6 ,  17.6 ,  23.0 ,  23.6 ,  22.1 ,  20.0 ,  16.0 ,  15.1 ,  12.1 ,  7.9 ,  6.2 ,  5.1 },
               { 7 ,  18.7 ,  21.5 ,  28.4 ,  30.2 ,  30.7 ,  31.0 ,  25.2 ,  23.1 ,  17.5 ,  10.7 ,  8.4 ,  7.2 }
               };
            POE_Table = new Table(data);
            Bind();
            Debug(0);
        }

        /** Runs the winds model; called by the Executive
         Can pass in a value indicating if the executive is directing the simulation to Hold.
         @param Holding if true, the executive has been directed to hold the sim
                        from advancing time. Some models may ignore this flag, such
                        as the Input model, which may need to be active to listen
                        on a socket for the "Resume" command to be given.
         @return false if no error */
        public override bool Run(bool Holding)
        {
            if (base.Run(Holding)) return true;
            if (Holding) return false;

            if (turbType != tType.ttNone) Turbulence(inputs.AltitudeASL);
            if (oneMinusCosineGust.gustProfile.Running) CosineGust();

            vTotalWindNED = vWindNED + vGustNED + vCosineGust + vTurbulenceNED;

            // psiw (Wind heading) is the direction the wind is blowing towards
            if (vWindNED.X != 0.0) psiw = Math.Atan2(vWindNED.Y, vWindNED.X);
            if (psiw < 0) psiw += 2 * Math.PI;

            Debug(2);
            return false;
        }
        public override bool InitModel()
        {
            if (!base.InitModel()) return false;

            psiw = 0.0;

            vGustNED = Vector3D.Zero;
            vTurbulenceNED = Vector3D.Zero;
            vCosineGust = Vector3D.Zero;

            oneMinusCosineGust.gustProfile.Running = false;
            oneMinusCosineGust.gustProfile.elapsedTime = 0.0;

            return true;
        }

        public enum tType { ttNone, ttStandard, ttCulp, ttMilspec, ttTustin }
        public tType turbType;

        // TOTAL WIND access functions (wind + gust + turbulence)

        /// Retrieves the total wind components in NED frame.
        public virtual Vector3D GetTotalWindNED() { return vTotalWindNED; }

        /// Retrieves a total wind component in NED frame.
        public virtual double GetTotalWindNED(int idx) { return vTotalWindNED[idx - 1]; }

        // WIND access functions

        /// Sets the wind components in NED frame.
        public virtual void SetWindNED(double wN, double wE, double wD) { vWindNED[0] = wN; vWindNED[1] = wE; vWindNED[2] = wD; }

        /// Sets a wind component in NED frame.
        public virtual void SetWindNED(int idx, double wind) { vWindNED[idx - 1] = wind; }

        /// Sets the wind components in NED frame.
        public virtual void SetWindNED(Vector3D wind) { vWindNED = wind; }

        /// Retrieves the wind components in NED frame.
        public virtual Vector3D GetWindNED() { return vWindNED; }

        /// Retrieves a wind component in NED frame.
        public virtual double GetWindNED(int idx) { return vWindNED[idx - 1]; }

        /** Retrieves the direction that the wind is coming from.
            The direction is defined as north=0 and increases counterclockwise.
            The wind heading is returned in radians.*/
        public virtual double GetWindPsi() { return psiw; }

        /** Sets the direction that the wind is coming from.
            The direction is defined as north=0 and increases counterclockwise to 2*pi
            (radians). The vertical component of wind is assumed to be zero - and is
            forcibly set to zero. This function sets the vWindNED vector components
            based on the supplied direction. The magnitude of the wind set in the
            vector is preserved (assuming the vertical component is non-zero).
            @param dir wind direction in the horizontal plane, in radians.*/
        public virtual void SetWindPsi(double dir)
        {
            // psi is the angle that the wind is blowing *towards*
            double mag = GetWindspeed();
            psiw = dir;
            SetWindspeed(mag);
        }

        /// <summary>
        ///  psi is the angle that the wind is blowing *towards*
        /// </summary>
        /// <param name="speed"></param>
        public virtual void SetWindspeed(double speed)
        {
            if (vWindNED.Magnitude() == 0.0)
            {
                psiw = 0.0;
                vWindNED.North = speed;
            }
            else
            {
                vWindNED.North = speed * Math.Cos(psiw);
                vWindNED.East = speed * Math.Sin(psiw);
                vWindNED.Down = 0.0;
            }
        }


        public virtual double GetWindspeed()
        {
            return vWindNED.Magnitude();
        }

        // GUST access functions

        /// Sets a gust component in NED frame.
        public virtual void SetGustNED(int idx, double gust) { vGustNED[idx - 1] = gust; }

        /// Sets a turbulence component in NED frame.
        public virtual void SetTurbNED(int idx, double turb) { vTurbulenceNED[idx - 1] = turb; }

        /// Sets the gust components in NED frame.
        public virtual void SetGustNED(double gN, double gE, double gD) { vGustNED.North = gN; vGustNED.East = gE; vGustNED.Down = gD; }

        /// Retrieves a gust component in NED frame.
        public virtual double GetGustNED(int idx) { return vGustNED[idx - 1]; }

        /// Retrieves a turbulence component in NED frame.
        public virtual double GetTurbNED(int idx) { return vTurbulenceNED[idx - 1]; }

        /// Retrieves the gust components in NED frame.
        public virtual Vector3D GetGustNED() { return vGustNED; }

        /** Turbulence models available: ttNone, ttStandard, ttBerndt, ttCulp,
            ttMilspec, ttTustin */
        public virtual void SetTurbType(tType tt) { turbType = tt; }
        public virtual tType GetTurbType() { return turbType; }

        public virtual void SetTurbGain(double tg) { TurbGain = tg; }
        public virtual double GetTurbGain() { return TurbGain; }

        public virtual void SetTurbRate(double tr) { TurbRate = tr; }
        public virtual double GetTurbRate() { return TurbRate; }

        public virtual void SetRhythmicity(double r) { Rhythmicity = r; }
        public virtual double GetRhythmicity() { return Rhythmicity; }

        public virtual double GetTurbPQR(int idx) { return vTurbPQR[idx - 1]; }
        public virtual double GetTurbMagnitude() { return vTurbulenceNED.Magnitude(); }
        public virtual double GetTurbDirection() { return TurbDirection; }
        public virtual Vector3D GetTurbPQR() { return vTurbPQR; }

        public virtual void SetWindspeed20ft(double ws) { windspeed_at_20ft = ws; }
        public virtual double GetWindspeed20ft() { return windspeed_at_20ft; }

        /// allowable range: 0-7, 3=light, 4=moderate, 6=severe turbulence
        public virtual void SetProbabilityOfExceedence(int idx) { probability_of_exceedence_index = idx; }
        public virtual int GetProbabilityOfExceedence() { return probability_of_exceedence_index; }

        // Stores data defining a 1 - cosine gust profile that builds up, holds steady
        // and fades out over specified durations.
        public class OneMinusCosineProfile
        {
            public bool Running;           ///<- This flag is set true through Winds.StartGust().
            public double elapsedTime;     ///<- Stores the elapsed time for the ongoing gust.
            public double startupDuration; ///<- Specifies the time it takes for the gust startup transient.
            public double steadyDuration;  ///<- Specifies the duration of the steady gust.
            public double endDuration;     ///<- Specifies the time it takes for the gust to subsude.
            public OneMinusCosineProfile() ///<- The constructor.
            {
                elapsedTime = 0.0;
                Running = false;
                startupDuration = 2;
                steadyDuration = 4;
                endDuration = 2;
            }
        }

        public enum eGustFrame { gfNone = 0, gfBody, gfWind, gfLocal };

        /// Stores the information about a single one minus cosine gust instance.
        public class OneMinusCosineGust
        {
            public Vector3D vWind;                    ///<- The input normalized wind vector.
            public Vector3D vWindTransformed;         ///<- The transformed normal vector at the time the gust is started.
            public double magnitude;                  ///<- The magnitude of the wind vector.
            public eGustFrame gustFrame;              ///<- The frame that the wind vector is specified in.
            public OneMinusCosineProfile gustProfile; ///<- The gust shape (profile) data for this gust.
            public OneMinusCosineGust()               ///<- Constructor.
            {
                vWind = Vector3D.Zero;
                gustFrame = eGustFrame.gfLocal;
                magnitude = 1.0;
                gustProfile = new OneMinusCosineProfile();
            }
        };

        /// Stores information about a specified Up- or Down-burst.
        public class UpDownBurstStruct
        {
            public double ringLatitude;                           ///<- The latitude of the downburst run (radians)
            public double ringLongitude;                          ///<- The longitude of the downburst run (radians)
            public double ringAltitude;                           ///<- The altitude of the ring (feet).
            public double ringRadius;                             ///<- The radius of the ring (feet).
            public double ringCoreRadius;                         ///<- The cross-section "core" radius of the ring (feet).
            public double circulation;                            ///<- The circulation (gamma) (feet-squared per second).
            public OneMinusCosineProfile oneMCosineProfile;///<- A gust profile structure.
            public UpDownBurstStruct()
            {                                ///<- Constructor
                ringLatitude = ringLongitude = 0.0;
                ringAltitude = 1000.0;
                ringRadius = 2000.0;
                ringCoreRadius = 100.0;
                circulation = 100000.0;
            }
        }

        // 1 - Cosine gust setters
        /// Initiates the execution of the gust.
        public virtual void StartGust(bool running) { oneMinusCosineGust.gustProfile.Running = running; }
        ///Specifies the duration of the startup portion of the gust.
        public virtual void StartupGustDuration(double dur) { oneMinusCosineGust.gustProfile.startupDuration = dur; }
        ///Specifies the length of time that the gust is at a steady, full strength.
        public virtual void SteadyGustDuration(double dur) { oneMinusCosineGust.gustProfile.steadyDuration = dur; }
        /// Specifies the length of time it takes for the gust to return to zero velocity.
        public virtual void EndGustDuration(double dur) { oneMinusCosineGust.gustProfile.endDuration = dur; }
        /// Specifies the magnitude of the gust in feet/second.
        public virtual void GustMagnitude(double mag) { oneMinusCosineGust.magnitude = mag; }
        /** Specifies the frame that the gust direction vector components are specified in. The 
            body frame is defined with the X direction forward, and the Y direction positive out
            the right wing. The wind frame is defined with the X axis pointing into the velocity
            vector, the Z axis perpendicular to the X axis, in the aircraft XZ plane, and the Y
            axis completing the system. The local axis is a navigational frame with X pointing north,
            Y pointing east, and Z pointing down. This is a locally vertical, locally horizontal
            frame, with the XY plane tangent to the geocentric surface. */
        public virtual void GustFrame(eGustFrame gFrame) { oneMinusCosineGust.gustFrame = gFrame; }
        /// Specifies the X component of velocity in the specified gust frame (ft/sec).
        public virtual void GustXComponent(double x) { oneMinusCosineGust.vWind.X = x; }
        /// Specifies the Y component of velocity in the specified gust frame (ft/sec).
        public virtual void GustYComponent(double y) { oneMinusCosineGust.vWind.Y = y; }
        /// Specifies the Z component of velocity in the specified gust frame (ft/sec).
        public virtual void GustZComponent(double z) { oneMinusCosineGust.vWind.Z = z; }

        // Up- Down-burst functions
        public void NumberOfUpDownburstCells(int num)
        {
            // for (  int i = 0; i < UpDownBurstCells.Count; i++) delete UpDownBurstCells[i];
            UpDownBurstCells.Clear();
            if (num >= 0)
            {
                for (int i = 0; i < num; i++) UpDownBurstCells.Add(new UpDownBurstStruct());
            }
        }

        public struct Inputs
        {
            public double V;
            public double wingspan;
            public double DistanceAGL;
            public double AltitudeASL;
            public double longitude;
            public double latitude;
            public double planetRadius;
            public Matrix3D Tl2b;
            public Matrix3D Tw2b;
            public double totalDeltaT;
        }
        public Inputs inputs = new Inputs();



        private double MagnitudedAccelDt, MagnitudeAccel, Magnitude, TurbDirection;
        //double h;
        private double TurbGain;
        private double TurbRate;
        private double Rhythmicity;
        private double wind_from_clockwise;
        private double spike, target_time, strength;
        private Vector3D vTurbulenceGrad;
        private Vector3D vBodyTurbGrad;
        private Vector3D vTurbPQR;

        private OneMinusCosineGust oneMinusCosineGust = new OneMinusCosineGust();
        private List<UpDownBurstStruct> UpDownBurstCells = new List<UpDownBurstStruct>();

        // Dryden turbulence model
        private double windspeed_at_20ft; ///< in ft/s
        private int probability_of_exceedence_index; ///< this is bound as the severity property
        private Table POE_Table; ///< probability of exceedence table

        private double psiw;
        private Vector3D vTotalWindNED;
        private Vector3D vWindNED;
        private Vector3D vGustNED;
        private Vector3D vCosineGust;
        private Vector3D vBurstGust;
        private Vector3D vTurbulenceNED;

        private void Turbulence(double h)
        {
            switch (turbType)
            {

                case tType.ttCulp:
                    vTurbPQR.P = wind_from_clockwise;
                    if (TurbGain == 0.0) return;

                    // keep the inputs within allowable limts for this model
                    if (TurbGain < 0.0) TurbGain = 0.0;
                    if (TurbGain > 1.0) TurbGain = 1.0;
                    if (TurbRate < 0.0) TurbRate = 0.0;
                    if (TurbRate > 30.0) TurbRate = 30.0;
                    if (Rhythmicity < 0.0) Rhythmicity = 0.0;
                    if (Rhythmicity > 1.0) Rhythmicity = 1.0;

                    // generate a sine wave corresponding to turbulence rate in hertz
                    double time = FDMExec.GetSimTime();
                    double sinewave = Math.Sin(time * TurbRate * 6.283185307);

                    double random = 0.0;
                    if (target_time == 0.0)
                    {
                        strength = random = 1 - 2.0 * uniformNumber.Next();
                        target_time = time + 0.71 + (random * 0.5);
                    }
                    if (time > target_time)
                    {
                        spike = 1.0;
                        target_time = 0.0;
                    }

                    // max vertical wind speed in fps, corresponds to TurbGain = 1.0
                    double max_vs = 40;

                    vTurbulenceNED = Vector3D.Zero;
                    double delta = strength * max_vs * TurbGain * (1 - Rhythmicity) * spike;

                    // Vertical component of turbulence.
                    vTurbulenceNED.Down = sinewave * max_vs * TurbGain * Rhythmicity;
                    vTurbulenceNED.Down += delta;
                    if (inputs.DistanceAGL / inputs.wingspan < 3.0)
                        vTurbulenceNED.Down *= inputs.DistanceAGL / inputs.wingspan * 0.3333;

                    // Yaw component of turbulence.
                    vTurbulenceNED.North = Math.Sin(delta * 3.0);
                    vTurbulenceNED.East = Math.Cos(delta * 3.0);

                    // Roll component of turbulence. Clockwise vortex causes left roll.
                    vTurbPQR.P += delta * 0.04;

                    spike = spike * 0.9;
                    break;
                case tType.ttMilspec:
                case tType.ttTustin:
                    // an index of zero means turbulence is disabled
                    // airspeed occurs as divisor in the code below
                    if (probability_of_exceedence_index == 0 || inputs.V == 0)
                    {
                        vTurbulenceNED.North = vTurbulenceNED.East = vTurbulenceNED.Down = 0.0;
                        vTurbPQR.P = vTurbPQR.Q = vTurbPQR.R = 0.0;
                        return;
                    }

                    // Turbulence model according to MIL-F-8785C (Flying Qualities of Piloted Aircraft)
                    double b_w = inputs.wingspan, L_u, L_w, sig_u, sig_w;

                    if (b_w == 0.0) b_w = 30.0;

                    // clip height functions at 10 ft
                    if (h <= 10.0) h = 10;

                    // Scale lengths L and amplitudes sigma as function of height
                    if (h <= 1000)
                    {
                        L_u = h / Math.Pow(0.177 + 0.000823 * h, 1.2); // MIL-F-8785c, Fig. 10, p. 55
                        L_w = h;
                        sig_w = 0.1 * windspeed_at_20ft;
                        sig_u = sig_w / Math.Pow(0.177 + 0.000823 * h, 0.4); // MIL-F-8785c, Fig. 11, p. 56
                    }
                    else if (h <= 2000)
                    {
                        // linear interpolation between low altitude and high altitude models
                        L_u = L_w = 1000 + (h - 1000.0) / 1000.0 * 750.0;
                        sig_u = sig_w = 0.1 * windspeed_at_20ft
                                      + (h - 1000.0) / 1000.0 * (POE_Table.GetValue(probability_of_exceedence_index, h) - 0.1 * windspeed_at_20ft);
                    }
                    else
                    {
                        L_u = L_w = 1750.0; //  MIL-F-8785c, Sec. 3.7.2.1, p. 48
                        sig_u = sig_w = POE_Table.GetValue(probability_of_exceedence_index, h);
                    }

                    // keep values from last timesteps
                    // TODO maybe use deque?
                    double
                    xi_u_km1 = 0, nu_u_km1 = 0,
                    xi_v_km1 = 0, xi_v_km2 = 0, nu_v_km1 = 0, nu_v_km2 = 0,
                    xi_w_km1 = 0, xi_w_km2 = 0, nu_w_km1 = 0, nu_w_km2 = 0,
                    xi_p_km1 = 0, nu_p_km1 = 0,
                    xi_q_km1 = 0, xi_r_km1 = 0;


                    double T_V = inputs.totalDeltaT, // for compatibility of nomenclature
                      sig_p = 1.9 / Math.Sqrt(L_w * b_w) * sig_w, // Yeager1998, eq. (8)
                                                                  //sig_q = Math.Sqrt(M_PI/2/L_w/b_w), // eq. (14)
                                                                  //sig_r = Math.Sqrt(2*M_PI/3/L_w/b_w), // eq. (17)
                      L_p = Math.Sqrt(L_w * b_w) / 2.6, // eq. (10)
                      tau_u = L_u / inputs.V, // eq. (6)
                      tau_w = L_w / inputs.V, // eq. (3)
                      tau_p = L_p / inputs.V, // eq. (9)
                      tau_q = 4 * b_w / Math.PI / inputs.V, // eq. (13)
                      tau_r = 3 * b_w / Math.PI / inputs.V, // eq. (17)
                      nu_u = gaussianNumber.Next(),
                      nu_v = gaussianNumber.Next(),
                      nu_w = gaussianNumber.Next(),
                      nu_p = gaussianNumber.Next(),
                      xi_u = 0, xi_v = 0, xi_w = 0, xi_p = 0, xi_q = 0, xi_r = 0;

                    // values of turbulence NED velocities

                    if (turbType == tType.ttTustin)
                    {
                        // the following is the Tustin formulation of Yeager's report
                        double omega_w = inputs.V / L_w, // hidden in nomenclature p. 3
                        omega_v = inputs.V / L_u, // this is defined nowhere
                        C_BL = 1 / tau_u / Math.Tan(T_V / 2 / tau_u), // eq. (19)
                        C_BLp = 1 / tau_p / Math.Tan(T_V / 2 / tau_p), // eq. (22)
                        C_BLq = 1 / tau_q / Math.Tan(T_V / 2 / tau_q), // eq. (24)
                        C_BLr = 1 / tau_r / Math.Tan(T_V / 2 / tau_r); // eq. (26)

                        // all values calculated so far are strictly positive, except for
                        // the random numbers nu_*. This means that in the code below, all
                        // divisors are strictly positive, too, and no floating point
                        // exception should occur.
                        xi_u = -(1 - C_BL * tau_u) / (1 + C_BL * tau_u) * xi_u_km1
                             + sig_u * Math.Sqrt(2 * tau_u / T_V) / (1 + C_BL * tau_u) * (nu_u + nu_u_km1); // eq. (18)
                        xi_v = -2 * (sqr(omega_v) - sqr(C_BL)) / sqr(omega_v + C_BL) * xi_v_km1
                             - sqr(omega_v - C_BL) / sqr(omega_v + C_BL) * xi_v_km2
                             + sig_u * Math.Sqrt(3 * omega_v / T_V) / sqr(omega_v + C_BL) * (
                                   (C_BL + omega_v / Math.Sqrt(3.0)) * nu_v
                                 + 2 / Math.Sqrt(3.0) * omega_v * nu_v_km1
                                 + (omega_v / Math.Sqrt(3.0) - C_BL) * nu_v_km2); // eq. (20) for v
                        xi_w = -2 * (sqr(omega_w) - sqr(C_BL)) / sqr(omega_w + C_BL) * xi_w_km1
                             - sqr(omega_w - C_BL) / sqr(omega_w + C_BL) * xi_w_km2
                             + sig_w * Math.Sqrt(3 * omega_w / T_V) / sqr(omega_w + C_BL) * (
                                   (C_BL + omega_w / Math.Sqrt(3.0)) * nu_w
                                 + 2 / Math.Sqrt(3.0) * omega_w * nu_w_km1
                                 + (omega_w / Math.Sqrt(3.0) - C_BL) * nu_w_km2); // eq. (20) for w
                        xi_p = -(1 - C_BLp * tau_p) / (1 + C_BLp * tau_p) * xi_p_km1
                             + sig_p * Math.Sqrt(2 * tau_p / T_V) / (1 + C_BLp * tau_p) * (nu_p + nu_p_km1); // eq. (21)
                        xi_q = -(1 - 4 * b_w * C_BLq / Math.PI / inputs.V) / (1 + 4 * b_w * C_BLq / Math.PI / inputs.V) * xi_q_km1
                                            + C_BLq / inputs.V / (1 + 4 * b_w * C_BLq / Math.PI / inputs.V) * (xi_w - xi_w_km1); // eq. (23)
                        xi_r = -(1 - 3 * b_w * C_BLr / Math.PI / inputs.V) / (1 + 3 * b_w * C_BLr / Math.PI / inputs.V) * xi_r_km1
                                           + C_BLr / inputs.V / (1 + 3 * b_w * C_BLr / Math.PI / inputs.V) * (xi_v - xi_v_km1); // eq. (25)

                    }
                    else if (turbType == tType.ttMilspec)
                    {
                        // the following is the MIL-STD-1797A formulation
                        // as cited in Yeager's report
                        xi_u = (1 - T_V / tau_u) * xi_u_km1 + sig_u * Math.Sqrt(2 * T_V / tau_u) * nu_u;  // eq. (30)
                        xi_v = (1 - 2 * T_V / tau_u) * xi_v_km1 + sig_u * Math.Sqrt(4 * T_V / tau_u) * nu_v;  // eq. (31)
                        xi_w = (1 - 2 * T_V / tau_w) * xi_w_km1 + sig_w * Math.Sqrt(4 * T_V / tau_w) * nu_w;  // eq. (32)
                        xi_p = (1 - T_V / tau_p) * xi_p_km1 + sig_p * Math.Sqrt(2 * T_V / tau_p) * nu_p;  // eq. (33)
                        xi_q = (1 - T_V / tau_q) * xi_q_km1 + Math.PI / 4 / b_w * (xi_w - xi_w_km1);  // eq. (34)
                        xi_r = (1 - T_V / tau_r) * xi_r_km1 + Math.PI / 3 / b_w * (xi_v - xi_v_km1);  // eq. (35)
                    }

                    // rotate by wind azimuth and assign the velocities
                    double cospsi = Math.Cos(psiw), sinpsi = Math.Sin(psiw);
                    vTurbulenceNED.North = cospsi * xi_u + sinpsi * xi_v;
                    vTurbulenceNED.East = -sinpsi * xi_u + cospsi * xi_v;
                    vTurbulenceNED.Down = xi_w;

                    vTurbPQR.P = cospsi * xi_p + sinpsi * xi_q;
                    vTurbPQR.Q = -sinpsi * xi_p + cospsi * xi_q;
                    vTurbPQR.R = xi_r;

                    // vTurbPQR is in the body fixed frame, not NED
                    vTurbPQR = inputs.Tl2b * vTurbPQR;

                    // hand on the values for the next timestep
                    xi_u_km1 = xi_u; nu_u_km1 = nu_u;
                    xi_v_km2 = xi_v_km1; xi_v_km1 = xi_v; nu_v_km2 = nu_v_km1; nu_v_km1 = nu_v;
                    xi_w_km2 = xi_w_km1; xi_w_km1 = xi_w; nu_w_km2 = nu_w_km1; nu_w_km1 = nu_w;
                    xi_p_km1 = xi_p; nu_p_km1 = nu_p;
                    xi_q_km1 = xi_q;
                    xi_r_km1 = xi_r;
                    break;
                default:
                    break;
            }

            TurbDirection = Math.Atan2(vTurbulenceNED.East, vTurbulenceNED.North) * Constants.radtodeg;

        }
        private void UpDownBurst()
        {

            for (int i = 0; i < UpDownBurstCells.Count; i++)
            {
                /*double d =*/
                DistanceFromRingCenter(UpDownBurstCells[i].ringLatitude, UpDownBurstCells[i].ringLongitude);

            }
        }

        private void CosineGust()
        {
            OneMinusCosineProfile profile = oneMinusCosineGust.gustProfile;

            double factor = CosineGustProfile(profile.startupDuration,
                                               profile.steadyDuration,
                                               profile.endDuration,
                                               profile.elapsedTime);
            // Normalize the gust wind vector
            oneMinusCosineGust.vWind.Normalize();

            if (oneMinusCosineGust.vWindTransformed.Magnitude() == 0.0)
            {
                switch (oneMinusCosineGust.gustFrame)
                {
                    case eGustFrame.gfBody:
                        oneMinusCosineGust.vWindTransformed = inputs.Tl2b.GetInverse() * oneMinusCosineGust.vWind;
                        break;
                    case eGustFrame.gfWind:
                        oneMinusCosineGust.vWindTransformed = inputs.Tl2b.GetInverse() * inputs.Tw2b * oneMinusCosineGust.vWind;
                        break;
                    case eGustFrame.gfLocal:
                        // this is the native frame - and the default.
                        oneMinusCosineGust.vWindTransformed = oneMinusCosineGust.vWind;
                        break;
                    default:
                        break;
                }
            }

            vCosineGust = factor * oneMinusCosineGust.vWindTransformed * oneMinusCosineGust.magnitude;

            profile.elapsedTime += inputs.totalDeltaT;

            if (profile.elapsedTime > (profile.startupDuration + profile.steadyDuration + profile.endDuration))
            {
                profile.Running = false;
                profile.elapsedTime = 0.0;
                oneMinusCosineGust.vWindTransformed = Vector3D.Zero;
                vCosineGust = Vector3D.Zero;
            }
        }
        private double CosineGustProfile(double startDuration, double steadyDuration,
                                  double endDuration, double elapsedTime)
        {
            double factor = 0.0;
            if (elapsedTime >= 0 && elapsedTime <= startDuration)
            {
                factor = (1.0 - Math.Cos(Math.PI * elapsedTime / startDuration)) / 2.0;
            }
            else if (elapsedTime > startDuration && (elapsedTime <= (startDuration + steadyDuration)))
            {
                factor = 1.0;
            }
            else if (elapsedTime > (startDuration + steadyDuration) && elapsedTime <= (startDuration + steadyDuration + endDuration))
            {
                factor = (1 - Math.Cos(Math.PI * (1 - (elapsedTime - (startDuration + steadyDuration)) / endDuration))) / 2.0;
            }
            else
            {
                factor = 0.0;
            }

            return factor;
        }
        private double DistanceFromRingCenter(double lat, double lon)
        {
            double deltaLat = inputs.latitude - lat;
            double deltaLong = inputs.longitude - lon;
            double dLat2 = deltaLat / 2.0;
            double dLong2 = deltaLong / 2.0;
            double a = Math.Sin(dLat2) * Math.Sin(dLat2)
                       + Math.Cos(lat) * Math.Cos(inputs.latitude) * Math.Sin(dLong2) * Math.Sin(dLong2);
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
            double d = inputs.planetRadius * c;
            return d;
        }
        /// simply square a value
        private double sqr(double x) { return x * x; }


        private UniformRandom uniformNumber = new UniformRandom();
        private NormalRandom gaussianNumber = new NormalRandom();
        //private void Debug(int from);
    }
}
