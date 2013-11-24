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

using System;
using System.Collections.Generic;
using System.Text;

// Import log4net classes.
using log4net;

namespace JSBSim.Models
{

    /// <summary>
    /// Models the MSIS-00 atmosphere.
    /// This is a wrapper for the NRL-MSIS-00 model 2001:
    /// 
    /// This C++ format model wraps the NRLMSISE-00 C source code package - release
    /// 20020503
    /// 
    /// The NRLMSISE-00 model was developed by Mike Picone, Alan Hedin, and
    /// Doug Drob. They also wrote a NRLMSISE-00 distribution package in 
    /// FORTRAN which is available at
    /// http://uap-www.nrl.navy.mil/models_web/msis/msis_home.htm
    /// 
    /// Dominik Brodowski implemented and maintains this C version. You can
    /// reach him at devel@brodo.de. See the file "DOCUMENTATION" for details,
    /// and check http://www.brodo.de/english/pub/nrlmsise/index.html for
    /// updated releases of this package.
    /// @author David Culp
    /// @version $Id: FGMSIS.h,v 1.3 2005/06/13 16:59:18 ehofman Exp $
    /// </summary>
    public class MSIS : Atmosphere
    {
        /// <summary>
        /// Define a static logger variable so that it references the
        ///	Logger instance.
        /// 
        /// NOTE that uMath.Sing System.Reflection.MethodBase.GetCurrentMethod().DeclaringType
        /// is equivalent to typeof(LoggingExample) but is more portable
        /// i.e. you can copy the code directly into another class without
        /// needing to edit the code.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        /// Constructor
        public MSIS(FDMExecutive exec)
            : base(exec)
        {
            Name = "MSIS";
        }

        /** Runs the MSIS-00 atmosphere model; called by the Executive
      @return false if no error */
        public override bool Run()
        {
            if (InternalRun()) return true;
            if (FDMExec.Holding()) return false;


            //do temp, pressure, and density first
            if (!useExternal)
            {
                // get sea-level values
                Calculate(FDMExec.Auxiliary.GetDayOfYear(),
                          FDMExec.Auxiliary.GetSecondsInDay(),
                          0.0,
                          FDMExec.Propagate.GetLocation().LatitudeDeg,
                          FDMExec.Propagate.GetLocation().LongitudeDeg);
                SLtemperature = output.t[1] * 1.8;
                SLdensity = output.d[5] * 1.940321;
                SLpressure = 1716.488 * SLdensity * SLtemperature;
                SLsoundspeed = Math.Sqrt(2403.0832 * SLtemperature);
                rSLtemperature = 1.0 / SLtemperature;
                rSLpressure = 1.0 / SLpressure;
                rSLdensity = 1.0 / SLdensity;
                rSLsoundspeed = 1.0 / SLsoundspeed;

                // get at-altitude values
                Calculate(FDMExec.Auxiliary.GetDayOfYear(),
                          FDMExec.Auxiliary.GetSecondsInDay(),
                          FDMExec.Propagate.Altitude,
                          FDMExec.Propagate.GetLocation().LatitudeDeg,
                          FDMExec.Propagate.GetLocation().LongitudeDeg);
                internalInfo.Temperature = output.t[1] * 1.8;
                internalInfo.Density = output.d[5] * 1.940321;
                internalInfo.Pressure = 1716.488 * internalInfo.Density * internalInfo.Temperature;
                soundspeed = Math.Sqrt(2403.0832 * internalInfo.Temperature);
                //cout << "T=" << intTemperature << " D=" << intDensity << " P=";
                //cout << intPressure << " a=" << soundspeed << endl;
            }

            if (turbType != TurbType.ttNone)
            {
                Turbulence();
                vWindNED += vTurbulence;
            }

            if (vWindNED[0] != 0.0)
                psiw = Math.Atan2(vWindNED[1], vWindNED[0]);

            if (psiw < 0) psiw += 2 * Math.PI;

            return false;
        }

        public override bool InitModel()
        {
            base.InitModel();

            flags.switches[0] = 0;
            for (int i = 1; i < 24; i++) flags.switches[i] = 1;

            for (int i = 0; i < 7; i++) aph.a[i] = 100.0;

            // set some common magnetic flux values
            input.f107A = 150.0;
            input.f107 = 150.0;
            input.ap = 4.0;

            SLtemperature = internalInfo.Temperature = 518.0;
            SLpressure = internalInfo.Pressure = 2116.7;
            SLdensity = internalInfo.Density = 0.002378;
            SLsoundspeed = Math.Sqrt(2403.0832 * SLtemperature);
            rSLtemperature = 1.0 / internalInfo.Temperature;
            rSLpressure = 1.0 / internalInfo.Pressure;
            rSLdensity = 1.0 / internalInfo.Density;
            rSLsoundspeed = 1.0 / SLsoundspeed;

            useInfo = internalInfo;

            useExternal = false;

            return true;
        }

        /// Does nothing. External control is not allowed.
        public override void UseExternal()
        {
        }


        private void Calculate(int day,      // day of year (1 to 366) 
                       double sec,   // seconds in day (0.0 to 86400.0)
                       double alt,   // altitude, feet
                       double lat,   // geodetic latitude, degrees
                       double lon    // geodetic longitude, degrees
                      )
        {
            input.year = 2000;
            input.doy = day;
            input.sec = sec;
            input.alt = alt / 3281;  //feet to kilometers
            input.g_lat = lat;
            input.g_long = lon;

            input.lst = (sec / 3600) + (lon / 15);
            if (input.lst > 24.0) input.lst -= 24.0;
            if (input.lst < 0.0) input.lst = 24 - input.lst;

            gtd7d(input, flags, output);
        }

        //void Debug(int from);

        private nrlmsise_flags flags = new nrlmsise_flags();
        private nrlmsise_input input = new nrlmsise_input();
        private nrlmsise_output output = new nrlmsise_output();
        private ap_array aph = new ap_array();

        /* PARMB */
        private double gsurf;
        private double re;

        /* GTS3C */
        private double dd;

        /* DMIX */
        private double dm04, dm16, dm28, dm32, dm40, dm01, dm14;

        /* MESO7 */
        private double[] meso_tn1 = new double[5];
        private double[] meso_tn2 = new double[4];
        private double[] meso_tn3 = new double[3];
        private double[] meso_tgn1 = new double[2];
        private double[] meso_tgn2 = new double[2];
        private double[] meso_tgn3 = new double[2];

        /* LPOLY */
        private double dfa;
        private double[,] plg = new double[4, 9];
        private double ctloc, stloc;
        private double c2tloc, s2tloc;
        private double s3tloc, c3tloc;
        private double apdf;
        private double[] apt = new double[4];

        private void tselec(ref nrlmsise_flags flags)
        {
            int i;
            for (i = 0; i < 24; i++)
            {
                if (i != 9)
                {
                    if (flags.switches[i] == 1)
                        flags.sw[i] = 1;
                    else
                        flags.sw[i] = 0;
                    if (flags.switches[i] > 0)
                        flags.swc[i] = 1;
                    else
                        flags.swc[i] = 0;
                }
                else
                {
                    flags.sw[i] = flags.switches[i];
                    flags.swc[i] = flags.switches[i];
                }
            }
        }

        private void glatf(double lat, out double gv, out double reff)
        {
            double dgtr = 1.74533E-2;
            double c2;
            c2 = Math.Cos(2.0 * dgtr * lat);
            gv = 980.616 * (1.0 - 0.0026373 * c2);
            reff = 2.0 * (gv) / (3.085462E-6 + 2.27E-9 * c2) * 1.0E-5;
        }

        private double ccor(double alt, double r, double h1, double zh)
        {
            /*        CHEMISTRY/DISSOCIATION CORRECTION FOR MSIS MODELS
             *         ALT - altitude
             *         R - target ratio
             *         H1 - transition scale length
             *         ZH - altitude of 1/2 R
             */
            double e;
            double ex;
            e = (alt - zh) / h1;
            if (e > 70)
                return Math.Exp(0.0);
            if (e < -70)
                return Math.Exp(r);
            ex = Math.Exp(e);
            e = r / (1.0 + ex);
            return Math.Exp(e);
        }

        private double ccor2(double alt, double r, double h1, double zh, double h2)
        {
            /*        CHEMISTRY/DISSOCIATION CORRECTION FOR MSIS MODELS
             *         ALT - altitude
             *         R - target ratio
             *         H1 - transition scale length
             *         ZH - altitude of 1/2 R
             *         H2 - transition scale length #2 ?
             */
            double e1, e2;
            double ex1, ex2;
            double ccor2v;
            e1 = (alt - zh) / h1;
            e2 = (alt - zh) / h2;
            if ((e1 > 70) || (e2 > 70))
                return Math.Exp(0.0);
            if ((e1 < -70) && (e2 < -70))
                return Math.Exp(r);
            ex1 = Math.Exp(e1);
            ex2 = Math.Exp(e2);
            ccor2v = r / (1.0 + 0.5 * (ex1 + ex2));
            return Math.Exp(ccor2v);
        }

        private double scalh(double alt, double xm, double temp)
        {
            double g;
            double rgas = 831.4;
            g = gsurf / (Math.Pow((1.0 + alt / re), 2.0));
            g = rgas * temp / (g * xm);
            return g;
        }

        private double dnet(double dd, double dm, double zhm, double xmm, double xm)
        {
            /*       TURBOPAUSE CORRECTION FOR MSIS MODELS
             *        Root mean density
             *         DD - diffusive density
             *         DM - full mixed density
             *         ZHM - transition scale length
             *         XMM - full mixed molecular weight
             *         XM  - species molecular weight
             *         DNET - combined density
             */
            double a;
            double ylog;
            a = zhm / (xmm - xm);
            if (!((dm > 0) && (dd > 0)))
            {
                if (log.IsErrorEnabled) log.Error("dnet log error " + dm + ", " + dd + ", " + xm);
                if ((dd == 0) && (dm == 0))
                    dd = 1;
                if (dm == 0)
                    return dd;
                if (dd == 0)
                    return dm;
            }
            ylog = a * Math.Log(dm / dd);
            if (ylog < -10)
                return dd;
            if (ylog > 10)
                return dm;
            a = dd * Math.Pow((1.0 + Math.Exp(ylog)), (1.0 / a));
            return a;
        }

        private void splini(double[] xa, double[] ya, double[] y2a, int n, double x, out double y)
        {
            /*      INTEGRATE CUBIC SPLINE FUNCTION FROM XA(1) TO X
             *       XA,YA: ARRAYS OF TABULATED FUNCTION IN ASCENDING ORDER BY X
             *       Y2A: ARRAY OF SECOND DERIVATIVES
             *       N: SIZE OF ARRAYS XA,YA,Y2A
             *       X: ABSCISSA ENDPOINT FOR INTEGRATION
             *       Y: OUTPUT VALUE
             */
            double yi = 0;
            int klo = 0;
            int khi = 1;
            double xx, h, a, b, a2, b2;
            while ((x > xa[klo]) && (khi < n))
            {
                xx = x;
                if (khi < (n - 1))
                {
                    if (x < xa[khi])
                        xx = x;
                    else
                        xx = xa[khi];
                }
                h = xa[khi] - xa[klo];
                a = (xa[khi] - xx) / h;
                b = (xx - xa[klo]) / h;
                a2 = a * a;
                b2 = b * b;
                yi += ((1.0 - a2) * ya[klo] / 2.0 + b2 * ya[khi] / 2.0 + ((-(1.0 + a2 * a2) / 4.0 + a2 / 2.0) * y2a[klo] + (b2 * b2 / 4.0 - b2 / 2.0) * y2a[khi]) * h * h / 6.0) * h;
                klo++;
                khi++;
            }
            y = yi;
        }

        private void splint(double[] xa, double[] ya, double[] y2a, int n, double x, out double y)
        {
            /*      CALCULATE CUBIC SPLINE INTERP VALUE
             *       ADAPTED FROM NUMERICAL RECIPES BY PRESS ET AL.
             *       XA,YA: ARRAYS OF TABULATED FUNCTION IN ASCENDING ORDER BY X
             *       Y2A: ARRAY OF SECOND DERIVATIVES
             *       N: SIZE OF ARRAYS XA,YA,Y2A
             *       X: ABSCISSA FOR INTERPOLATION
             *       Y: OUTPUT VALUE
             */
            int klo = 0;
            int khi = n - 1;
            int k;
            double h;
            double a, b, yi;
            while ((khi - klo) > 1)
            {
                k = (khi + klo) / 2;
                if (xa[k] > x)
                    khi = k;
                else
                    klo = k;
            }
            h = xa[khi] - xa[klo];
            if (h == 0.0)
                if (log.IsErrorEnabled)
                    log.Error("bad XA input to splint");
            a = (xa[khi] - x) / h;
            b = (x - xa[klo]) / h;
            yi = a * ya[klo] + b * ya[khi] + ((a * a * a - a) * y2a[klo] + (b * b * b - b) * y2a[khi]) * h * h / 6.0;
            y = yi;
        }

        private void spline(double[] x, double[] y, int n, double yp1, double ypn, ref double[] y2)
        {
            /*       CALCULATE 2ND DERIVATIVES OF CUBIC SPLINE INTERP FUNCTION
             *       ADAPTED FROM NUMERICAL RECIPES BY PRESS ET AL
             *       X,Y: ARRAYS OF TABULATED FUNCTION IN ASCENDING ORDER BY X
             *       N: SIZE OF ARRAYS X,Y
             *       YP1,YPN: SPECIFIED DERIVATIVES AT X[0] AND X[N-1]; VALUES
             *                >= 1E30 SIGNAL SIGNAL SECOND DERIVATIVE ZERO
             *       Y2: OUTPUT ARRAY OF SECOND DERIVATIVES
             */
            double[] u = new double[n];
            double sig, p, qn, un;
            int i, k;
            if (yp1 > 0.99E30)
            {
                y2[0] = 0;
                u[0] = 0;
            }
            else
            {
                y2[0] = -0.5;
                u[0] = (3.0 / (x[1] - x[0])) * ((y[1] - y[0]) / (x[1] - x[0]) - yp1);
            }
            for (i = 1; i < (n - 1); i++)
            {
                sig = (x[i] - x[i - 1]) / (x[i + 1] - x[i - 1]);
                p = sig * y2[i - 1] + 2.0;
                y2[i] = (sig - 1.0) / p;
                u[i] = (6.0 * ((y[i + 1] - y[i]) / (x[i + 1] - x[i]) - (y[i] - y[i - 1]) / (x[i] - x[i - 1])) / (x[i + 1] - x[i - 1]) - sig * u[i - 1]) / p;
            }
            if (ypn > 0.99E30)
            {
                qn = 0;
                un = 0;
            }
            else
            {
                qn = 0.5;
                un = (3.0 / (x[n - 1] - x[n - 2])) * (ypn - (y[n - 1] - y[n - 2]) / (x[n - 1] - x[n - 2]));
            }
            y2[n - 1] = (un - qn * u[n - 2]) / (qn * y2[n - 2] + 1.0);
            for (k = n - 2; k >= 0; k--)
                y2[k] = y2[k] * y2[k + 1] + u[k];
        }

        private double zeta(double zz, double zl)
        {
            return ((zz - zl) * (re + zl) / (re + zz));
        }

        private double densm(double alt, double d0, double xm, ref double tz, int mn3, double[] zn3,
                     double[] tn3, double[] tgn3, int mn2, double[] zn2, double[] tn2,
                     double[] tgn2)
        {
            /*      Calculate Temperature and Density Profiles for lower atmos.  */
            double[] xs = new double[10];
            double[] ys = new double[10];
            double[] y2out = new double[10];
            double rgas = 831.4;
            double z, z1, z2, t1, t2, zg, zgdif;
            double yd1, yd2;
            double x, y, yi;
            double expl, gamm, glb;
            double densm_tmp;
            int mn;
            int k;
            densm_tmp = d0;
            if (alt > zn2[0])
            {
                if (xm == 0.0)
                    return tz;
                else
                    return d0;
            }

            /* STRATOSPHERE/MESOSPHERE TEMPERATURE */
            if (alt > zn2[mn2 - 1])
                z = alt;
            else
                z = zn2[mn2 - 1];
            mn = mn2;
            z1 = zn2[0];
            z2 = zn2[mn - 1];
            t1 = tn2[0];
            t2 = tn2[mn - 1];
            zg = zeta(z, z1);
            zgdif = zeta(z2, z1);

            /* set up spline nodes */
            for (k = 0; k < mn; k++)
            {
                xs[k] = zeta(zn2[k], z1) / zgdif;
                ys[k] = 1.0 / tn2[k];
            }
            yd1 = -tgn2[0] / (t1 * t1) * zgdif;
            yd2 = -tgn2[1] / (t2 * t2) * zgdif * (Math.Pow(((re + z2) / (re + z1)), 2.0));

            /* calculate spline coefficients */
            spline(xs, ys, mn, yd1, yd2, ref y2out);
            x = zg / zgdif;
            splint(xs, ys, y2out, mn, x, out y);

            /* temperature at altitude */
            tz = 1.0 / y;
            if (xm != 0.0)
            {
                /* calaculate stratosphere / mesospehere density */
                glb = gsurf / (Math.Pow((1.0 + z1 / re), 2.0));
                gamm = xm * glb * zgdif / rgas;

                /* Integrate temperature profile */
                splini(xs, ys, y2out, mn, x, out yi);
                expl = gamm * yi;
                if (expl > 50.0)
                    expl = 50.0;

                /* Density at altitude */
                densm_tmp = densm_tmp * (t1 / tz) * Math.Exp(-expl);
            }

            if (alt > zn3[0])
            {
                if (xm == 0.0)
                    return tz;
                else
                    return densm_tmp;
            }

            /* troposhere / stratosphere temperature */
            z = alt;
            mn = mn3;
            z1 = zn3[0];
            z2 = zn3[mn - 1];
            t1 = tn3[0];
            t2 = tn3[mn - 1];
            zg = zeta(z, z1);
            zgdif = zeta(z2, z1);

            /* set up spline nodes */
            for (k = 0; k < mn; k++)
            {
                xs[k] = zeta(zn3[k], z1) / zgdif;
                ys[k] = 1.0 / tn3[k];
            }
            yd1 = -tgn3[0] / (t1 * t1) * zgdif;
            yd2 = -tgn3[1] / (t2 * t2) * zgdif * (Math.Pow(((re + z2) / (re + z1)), 2.0));

            /* calculate spline coefficients */
            spline(xs, ys, mn, yd1, yd2, ref y2out);
            x = zg / zgdif;
            splint(xs, ys, y2out, mn, x, out y);

            /* temperature at altitude */
            tz = 1.0 / y;
            if (xm != 0.0)
            {
                /* calaculate tropospheric / stratosphere density */
                glb = gsurf / (Math.Pow((1.0 + z1 / re), 2.0));
                gamm = xm * glb * zgdif / rgas;

                /* Integrate temperature profile */
                splini(xs, ys, y2out, mn, x, out yi);
                expl = gamm * yi;
                if (expl > 50.0)
                    expl = 50.0;

                /* Density at altitude */
                densm_tmp = densm_tmp * (t1 / tz) * Math.Exp(-expl);
            }
            if (xm == 0.0)
                return tz;
            else
                return densm_tmp;
        }
        private double densu(double alt, double dlb, double tinf, double tlb, double xm,
                     double alpha, ref double tz, double zlb, double s2, int mn1,
                     double[] zn1, double[] tn1, double[] tgn1)
        {
            /*      Calculate Temperature and Density Profiles for MSIS models
             *      New lower thermo polynomial
             */
            double yd2, yd1, x = 0.0, y = 0.0;
            double rgas = 831.4;
            double densu_temp = 1.0;
            double za, z, zg2, tt, ta = 0.0;
            double dta, z1 = 0.0, z2, t1 = 0.0, t2, zg, zgdif = 0.0;
            int mn = 0;
            int k;
            double glb;
            double expl;
            double yi;
            double densa;
            double gamma, gamm;
            double[] xs = new double[5], ys = new double[5], y2out = new double[5];
            /* joining altitudes of Bates and spline */
            za = zn1[0];
            if (alt > za)
                z = alt;
            else
                z = za;

            /* geopotential altitude difference from ZLB */
            zg2 = zeta(z, zlb);

            /* Bates temperature */
            tt = tinf - (tinf - tlb) * Math.Exp(-s2 * zg2);
            ta = tt;
            tz = tt;
            densu_temp = tz;

            if (alt < za)
            {
                /* calculate temperature below ZA
                 * temperature gradient at ZA from Bates profile */
                dta = (tinf - ta) * s2 * Math.Pow(((re + zlb) / (re + za)), 2.0);
                tgn1[0] = dta;
                tn1[0] = ta;
                if (alt > zn1[mn1 - 1])
                    z = alt;
                else
                    z = zn1[mn1 - 1];
                mn = mn1;
                z1 = zn1[0];
                z2 = zn1[mn - 1];
                t1 = tn1[0];
                t2 = tn1[mn - 1];
                /* geopotental difference from z1 */
                zg = zeta(z, z1);
                zgdif = zeta(z2, z1);
                /* set up spline nodes */
                for (k = 0; k < mn; k++)
                {
                    xs[k] = zeta(zn1[k], z1) / zgdif;
                    ys[k] = 1.0 / tn1[k];
                }
                /* end node derivatives */
                yd1 = -tgn1[0] / (t1 * t1) * zgdif;
                yd2 = -tgn1[1] / (t2 * t2) * zgdif * Math.Pow(((re + z2) / (re + z1)), 2.0);
                /* calculate spline coefficients */
                spline(xs, ys, mn, yd1, yd2, ref y2out);
                x = zg / zgdif;
                splint(xs, ys, y2out, mn, x, out y);
                /* temperature at altitude */
                tz = 1.0 / y;
                densu_temp = tz;
            }
            if (xm == 0)
                return densu_temp;

            /* calculate density above za */
            glb = gsurf / Math.Pow((1.0 + zlb / re), 2.0);
            gamma = xm * glb / (s2 * rgas * tinf);
            expl = Math.Exp(-s2 * gamma * zg2);
            if (expl > 50.0)
                expl = 50.0;
            if (tt <= 0)
                expl = 50.0;

            /* density at altitude */
            densa = dlb * Math.Pow((tlb / tt), ((1.0 + alpha + gamma))) * expl;
            densu_temp = densa;
            if (alt >= za)
                return densu_temp;

            /* calculate density below za */
            glb = gsurf / Math.Pow((1.0 + z1 / re), 2.0);
            gamm = xm * glb * zgdif / rgas;

            /* integrate spline temperatures */
            splini(xs, ys, y2out, mn, x, out yi);
            expl = gamm * yi;
            if (expl > 50.0)
                expl = 50.0;
            if (tz <= 0)
                expl = 50.0;

            /* density at altitude */
            densu_temp = densu_temp * Math.Pow((t1 / tz), (1.0 + alpha)) * Math.Exp(-expl);
            return densu_temp;
        }

        /*    3hr Magnetic activity functions */
        /*    Eq. A24d */
        private double g0(double a, double[] p)
        {
            return (a - 4.0 + (p[25] - 1.0) * (a - 4.0 + (Math.Exp(-Math.Sqrt(p[24] * p[24]) *
                          (a - 4.0)) - 1.0) / Math.Sqrt(p[24] * p[24])));
        }

        /*    Eq. A24c */
        private double sumex(double ex)
        {
            return (1.0 + (1.0 - Math.Pow(ex, 19.0)) / (1.0 - ex) * Math.Pow(ex, 0.5));
        }

        /*    Eq. A24a */
        double sg0(double ex, double[] p, double[] ap)
        {
            return (g0(ap[1], p) + (g0(ap[2], p) * ex + g0(ap[3], p) * ex * ex +
                          g0(ap[4], p) * Math.Pow(ex, 3.0) + (g0(ap[5], p) * Math.Pow(ex, 4.0) +
                          g0(ap[6], p) * Math.Pow(ex, 12.0)) * (1.0 - Math.Pow(ex, 8.0)) / (1.0 - ex))) / sumex(ex);
        }

        private double globe7(double[] p, nrlmsise_input input, nrlmsise_flags flags)
        {
            /*       CALCULATE G(L) FUNCTION
             *       Upper Thermosphere Parameters */
            double[] t = new double[15];
            int i, j;
            int sw9 = 1;
            double apd;
            double xlong;
            double tloc;
            double c, s, c2, c4, s2;
            double sr = 7.2722E-5;
            double dgtr = 1.74533E-2;
            double dr = 1.72142E-2;
            double hr = 0.2618;
            double cd32, cd18, cd14, cd39;
            double p32, p18, p14, p39;
            double df, dfa;
            double f1, f2;
            double tinf;
            ap_array ap;

            tloc = input.lst;
            for (j = 0; j < 14; j++)
                t[j] = 0;
            if (flags.sw[9] > 0)
                sw9 = 1;
            else if (flags.sw[9] < 0)
                sw9 = -1;
            xlong = input.g_long;

            /* calculate legendre polynomials */
            c = Math.Sin(input.g_lat * dgtr);
            s = Math.Cos(input.g_lat * dgtr);
            c2 = c * c;
            c4 = c2 * c2;
            s2 = s * s;

            plg[0, 1] = c;
            plg[0, 2] = 0.5 * (3.0 * c2 - 1.0);
            plg[0, 3] = 0.5 * (5.0 * c * c2 - 3.0 * c);
            plg[0, 4] = (35.0 * c4 - 30.0 * c2 + 3.0) / 8.0;
            plg[0, 5] = (63.0 * c2 * c2 * c - 70.0 * c2 * c + 15.0 * c) / 8.0;
            plg[0, 6] = (11.0 * c * plg[0, 5] - 5.0 * plg[0, 4]) / 6.0;
            /*      plg[0,7] = (13.0*c*plg[0,6] - 6.0*plg[0,5])/7.0; */
            plg[1, 1] = s;
            plg[1, 2] = 3.0 * c * s;
            plg[1, 3] = 1.5 * (5.0 * c2 - 1.0) * s;
            plg[1, 4] = 2.5 * (7.0 * c2 * c - 3.0 * c) * s;
            plg[1, 5] = 1.875 * (21.0 * c4 - 14.0 * c2 + 1.0) * s;
            plg[1, 6] = (11.0 * c * plg[1, 5] - 6.0 * plg[1, 4]) / 5.0;
            /*      plg[1,7] = (13.0*c*plg[1,6]-7.0*plg[1,5])/6.0; */
            /*      plg[1,8] = (15.0*c*plg[1,7]-8.0*plg[1,6])/7.0; */
            plg[2, 2] = 3.0 * s2;
            plg[2, 3] = 15.0 * s2 * c;
            plg[2, 4] = 7.5 * (7.0 * c2 - 1.0) * s2;
            plg[2, 5] = 3.0 * c * plg[2, 4] - 2.0 * plg[2, 3];
            plg[2, 6] = (11.0 * c * plg[2, 5] - 7.0 * plg[2, 4]) / 4.0;
            plg[2, 7] = (13.0 * c * plg[2, 6] - 8.0 * plg[2, 5]) / 5.0;
            plg[3, 3] = 15.0 * s2 * s;
            plg[3, 4] = 105.0 * s2 * s * c;
            plg[3, 5] = (9.0 * c * plg[3, 4] - 7.0 * plg[3, 3]) / 2.0;
            plg[3, 6] = (11.0 * c * plg[3, 5] - 8.0 * plg[3, 4]) / 3.0;

            if (!(((flags.sw[7] == 0) && (flags.sw[8] == 0)) && (flags.sw[14] == 0)))
            {
                stloc = Math.Sin(hr * tloc);
                ctloc = Math.Cos(hr * tloc);
                s2tloc = Math.Sin(2.0 * hr * tloc);
                c2tloc = Math.Cos(2.0 * hr * tloc);
                s3tloc = Math.Sin(3.0 * hr * tloc);
                c3tloc = Math.Cos(3.0 * hr * tloc);
            }

            cd32 = Math.Cos(dr * (input.doy - p[31]));
            cd18 = Math.Cos(2.0 * dr * (input.doy - p[17]));
            cd14 = Math.Cos(dr * (input.doy - p[13]));
            cd39 = Math.Cos(2.0 * dr * (input.doy - p[38]));
            p32 = p[31];
            p18 = p[17];
            p14 = p[13];
            p39 = p[38];

            /* F10.7 EFFECT */
            df = input.f107 - input.f107A;
            dfa = input.f107A - 150.0;
            t[0] = p[19] * df * (1.0 + p[59] * dfa) + p[20] * df * df + p[21] * dfa + p[29] * Math.Pow(dfa, 2.0);
            f1 = 1.0 + (p[47] * dfa + p[19] * df + p[20] * df * df) * flags.swc[1];
            f2 = 1.0 + (p[49] * dfa + p[19] * df + p[20] * df * df) * flags.swc[1];

            /*  TIME INDEPENDENT */
            t[1] = (p[1] * plg[0, 2] + p[2] * plg[0, 4] + p[22] * plg[0, 6]) +
                  (p[14] * plg[0, 2]) * dfa * flags.swc[1] + p[26] * plg[0, 1];

            /*  SYMMETRICAL ANNUAL */
            t[2] = p[18] * cd32;

            /*  SYMMETRICAL SEMIANNUAL */
            t[3] = (p[15] + p[16] * plg[0, 2]) * cd18;

            /*  ASYMMETRICAL ANNUAL */
            t[4] = f1 * (p[9] * plg[0, 1] + p[10] * plg[0, 3]) * cd14;

            /*  ASYMMETRICAL SEMIANNUAL */
            t[5] = p[37] * plg[0, 1] * cd39;

            /* DIURNAL */
            if (flags.sw[7] != 0)
            {
                double t71, t72;
                t71 = (p[11] * plg[1, 2]) * cd14 * flags.swc[5];
                t72 = (p[12] * plg[1, 2]) * cd14 * flags.swc[5];
                t[6] = f2 * ((p[3] * plg[1, 1] + p[4] * plg[1, 3] + p[27] * plg[1, 5] + t71) *
                     ctloc + (p[6] * plg[1, 1] + p[7] * plg[1, 3] + p[28] * plg[1, 5]
                        + t72) * stloc);
            }

            /* SEMIDIURNAL */
            if (flags.sw[8] != 0)
            {
                double t81, t82;
                t81 = (p[23] * plg[2, 3] + p[35] * plg[2, 5]) * cd14 * flags.swc[5];
                t82 = (p[33] * plg[2, 3] + p[36] * plg[2, 5]) * cd14 * flags.swc[5];
                t[7] = f2 * ((p[5] * plg[2, 2] + p[41] * plg[2, 4] + t81) * c2tloc + (p[8] * plg[2, 2] + p[42] * plg[2, 4] + t82) * s2tloc);
            }

            /* TERDIURNAL */
            if (flags.sw[14] != 0)
            {
                t[13] = f2 * ((p[39] * plg[3, 3] + (p[93] * plg[3, 4] + p[46] * plg[3, 6]) * cd14 * flags.swc[5]) * s3tloc + (p[40] * plg[3, 3] + (p[94] * plg[3, 4] + p[48] * plg[3, 6]) * cd14 * flags.swc[5]) * c3tloc);
            }

            /* magnetic activity based on daily ap */
            if (flags.sw[9] == -1)
            {
                ap = input.ap_a;
                if (p[51] != 0)
                {
                    double exp1;
                    exp1 = Math.Exp(-10800.0 * Math.Sqrt(p[51] * p[51]) / (1.0 + p[138] * (45.0 - Math.Sqrt(input.g_lat * input.g_lat))));
                    if (exp1 > 0.99999)
                        exp1 = 0.99999;
                    if (p[24] < 1.0E-4)
                        p[24] = 1.0E-4;
                    apt[0] = sg0(exp1, p, ap.a);
                    /* apt[1]=sg2(Math.Exp1,p,ap.a);
                       apt[2]=sg0(Math.Exp2,p,ap.a);
                       apt[3]=sg2(Math.Exp2,p,ap.a);
                    */
                    if (flags.sw[9] != 0)
                    {
                        t[8] = apt[0] * (p[50] + p[96] * plg[0, 2] + p[54] * plg[0, 4] +
                     (p[125] * plg[0, 1] + p[126] * plg[0, 3] + p[127] * plg[0, 5]) * cd14 * flags.swc[5] +
                     (p[128] * plg[1, 1] + p[129] * plg[1, 3] + p[130] * plg[1, 5]) * flags.swc[7] *
                                 Math.Cos(hr * (tloc - p[131])));
                    }
                }
            }
            else
            {
                double p44, p45;
                apd = input.ap - 4.0;
                p44 = p[43];
                p45 = p[44];
                if (p44 < 0)
                    p44 = 1.0E-5;
                apdf = apd + (p45 - 1.0) * (apd + (Math.Exp(-p44 * apd) - 1.0) / p44);
                if (flags.sw[9] != 0)
                {
                    t[8] = apdf * (p[32] + p[45] * plg[0, 2] + p[34] * plg[0, 4] +
                   (p[100] * plg[0, 1] + p[101] * plg[0, 3] + p[102] * plg[0, 5]) * cd14 * flags.swc[5] +
                   (p[121] * plg[1, 1] + p[122] * plg[1, 3] + p[123] * plg[1, 5]) * flags.swc[7] *
                          Math.Cos(hr * (tloc - p[124])));
                }
            }

            if ((flags.sw[10] != 0) && (input.g_long > -1000.0))
            {

                /* longitudinal */
                if (flags.sw[11] != 0)
                {
                    t[10] = (1.0 + p[80] * dfa * flags.swc[1]) *
                   ((p[64] * plg[1, 2] + p[65] * plg[1, 4] + p[66] * plg[1, 6]
                    + p[103] * plg[1, 1] + p[104] * plg[1, 3] + p[105] * plg[1, 5]
                    + flags.swc[5] * (p[109] * plg[1, 1] + p[110] * plg[1, 3] + p[111] * plg[1, 5]) * cd14) *
                        Math.Cos(dgtr * input.g_long)
                    + (p[90] * plg[1, 2] + p[91] * plg[1, 4] + p[92] * plg[1, 6]
                    + p[106] * plg[1, 1] + p[107] * plg[1, 3] + p[108] * plg[1, 5]
                    + flags.swc[5] * (p[112] * plg[1, 1] + p[113] * plg[1, 3] + p[114] * plg[1, 5]) * cd14) *
                    Math.Sin(dgtr * input.g_long));
                }

                /* ut and mixed ut, longitude */
                if (flags.sw[12] != 0)
                {
                    t[11] = (1.0 + p[95] * plg[0, 1]) * (1.0 + p[81] * dfa * flags.swc[1]) *
                      (1.0 + p[119] * plg[0, 1] * flags.swc[5] * cd14) *
                      ((p[68] * plg[0, 1] + p[69] * plg[0, 3] + p[70] * plg[0, 5]) *
                      Math.Cos(sr * (input.sec - p[71])));
                    t[11] += flags.swc[11] *
                      (p[76] * plg[2, 3] + p[77] * plg[2, 5] + p[78] * plg[2, 7]) *
                      Math.Cos(sr * (input.sec - p[79]) + 2.0 * dgtr * input.g_long) * (1.0 + p[137] * dfa * flags.swc[1]);
                }

                /* ut, longitude magnetic activity */
                if (flags.sw[13] != 0)
                {
                    if (flags.sw[9] == -1)
                    {
                        if (p[51] != 0)
                        {
                            t[12] = apt[0] * flags.swc[11] * (1.0 + p[132] * plg[0, 1]) *
                              ((p[52] * plg[1, 2] + p[98] * plg[1, 4] + p[67] * plg[1, 6]) *
                               Math.Cos(dgtr * (input.g_long - p[97])))
                              + apt[0] * flags.swc[11] * flags.swc[5] *
                              (p[133] * plg[1, 1] + p[134] * plg[1, 3] + p[135] * plg[1, 5]) *
                              cd14 * Math.Cos(dgtr * (input.g_long - p[136]))
                              + apt[0] * flags.swc[12] *
                              (p[55] * plg[0, 1] + p[56] * plg[0, 3] + p[57] * plg[0, 5]) *
                              Math.Cos(sr * (input.sec - p[58]));
                        }
                    }
                    else
                    {
                        t[12] = apdf * flags.swc[11] * (1.0 + p[120] * plg[0, 1]) *
                          ((p[60] * plg[1, 2] + p[61] * plg[1, 4] + p[62] * plg[1, 6]) *
                          Math.Cos(dgtr * (input.g_long - p[63])))
                          + apdf * flags.swc[11] * flags.swc[5] *
                          (p[115] * plg[1, 1] + p[116] * plg[1, 3] + p[117] * plg[1, 5]) *
                          cd14 * Math.Cos(dgtr * (input.g_long - p[118]))
                          + apdf * flags.swc[12] *
                          (p[83] * plg[0, 1] + p[84] * plg[0, 3] + p[85] * plg[0, 5]) *
                          Math.Cos(sr * (input.sec - p[75]));
                    }
                }
            }

            /* parms not used: 82, 89, 99, 139-149 */
            tinf = p[30];
            for (i = 0; i < 14; i++)
                tinf = tinf + Math.Abs(flags.sw[i + 1]) * t[i];
            return tinf;
        }

        private double glob7s(double[,] p, int firstindex, nrlmsise_input input,
                              nrlmsise_flags flags)
        {
            /*    VERSION OF GLOBE FOR LOWER ATMOSPHERE 10/26/99
             */
            double pset = 2.0;
            double[] t = new double[14];
            double tt;
            double cd32, cd18, cd14, cd39;
            double p32, p18, p14, p39;
            int i, j;
            double dr = 1.72142E-2;
            double dgtr = 1.74533E-2;
            /* confirm parameter set */
            if (p[firstindex, 99] == 0)
                p[firstindex, 99] = pset;
            if (p[firstindex, 99] != pset)
            {
                if (log.IsErrorEnabled)
                    log.Error("Wrong parameter set for glob7sn");
                return -1;
            }
            for (j = 0; j < 14; j++)
                t[j] = 0.0;
            cd32 = Math.Cos(dr * (input.doy - p[firstindex, 31]));
            cd18 = Math.Cos(2.0 * dr * (input.doy - p[firstindex, 17]));
            cd14 = Math.Cos(dr * (input.doy - p[firstindex, 13]));
            cd39 = Math.Cos(2.0 * dr * (input.doy - p[firstindex, 38]));
            p32 = p[firstindex, 31];
            p18 = p[firstindex, 17];
            p14 = p[firstindex, 13];
            p39 = p[firstindex, 38];

            /* F10.7 */
            t[0] = p[firstindex, 21] * dfa;

            /* time independent */
            t[1] = p[firstindex, 1] * plg[0, 2] + p[firstindex, 2] * plg[0, 4] + p[firstindex, 22] * plg[0, 6] + p[firstindex, 26] * plg[0, 1] + p[firstindex, 14] * plg[0, 3] + p[firstindex, 59] * plg[0, 5];

            /* SYMMETRICAL ANNUAL */
            t[2] = (p[firstindex, 18] + p[firstindex, 47] * plg[0, 2] + p[firstindex, 29] * plg[0, 4]) * cd32;

            /* SYMMETRICAL SEMIANNUAL */
            t[3] = (p[firstindex, 15] + p[firstindex, 16] * plg[0, 2] + p[firstindex, 30] * plg[0, 4]) * cd18;

            /* ASYMMETRICAL ANNUAL */
            t[4] = (p[firstindex, 9] * plg[0, 1] + p[firstindex, 10] * plg[0, 3] + p[firstindex, 20] * plg[0, 5]) * cd14;

            /* ASYMMETRICAL SEMIANNUAL */
            t[5] = (p[firstindex, 37] * plg[0, 1]) * cd39;

            /* DIURNAL */
            if (flags.sw[7] != 0)
            {
                double t71, t72;
                t71 = p[firstindex, 11] * plg[1, 2] * cd14 * flags.swc[5];
                t72 = p[firstindex, 12] * plg[1, 2] * cd14 * flags.swc[5];
                t[6] = ((p[firstindex, 3] * plg[1, 1] + p[firstindex, 4] * plg[1, 3] + t71) * ctloc + (p[firstindex, 6] * plg[1, 1] + p[firstindex, 7] * plg[1, 3] + t72) * stloc);
            }

            /* SEMIDIURNAL */
            if (flags.sw[8] != 0)
            {
                double t81, t82;
                t81 = (p[firstindex, 23] * plg[2, 3] + p[firstindex, 35] * plg[2, 5]) * cd14 * flags.swc[5];
                t82 = (p[firstindex, 33] * plg[2, 3] + p[firstindex, 36] * plg[2, 5]) * cd14 * flags.swc[5];
                t[7] = ((p[firstindex, 5] * plg[2, 2] + p[firstindex, 41] * plg[2, 4] + t81) * c2tloc + (p[firstindex, 8] * plg[2, 2] + p[firstindex, 42] * plg[2, 4] + t82) * s2tloc);
            }

            /* TERDIURNAL */
            if (flags.sw[14] != 0)
            {
                t[13] = p[firstindex, 39] * plg[3, 3] * s3tloc + p[firstindex, 40] * plg[3, 3] * c3tloc;
            }

            /* MAGNETIC ACTIVITY */
            if (flags.sw[9] != 0)
            {
                if (flags.sw[9] == 1)
                    t[8] = apdf * (p[firstindex, 32] + p[firstindex, 45] * plg[0, 2] * flags.swc[2]);
                if (flags.sw[9] == -1)
                    t[8] = (p[firstindex, 50] * apt[0] + p[firstindex, 96] * plg[0, 2] * apt[0] * flags.swc[2]);
            }

            /* LONGITUDINAL */
            if (!((flags.sw[10] == 0) || (flags.sw[11] == 0) || (input.g_long <= -1000.0)))
            {
                t[10] = (1.0 + plg[0, 1] * (p[firstindex, 80] * flags.swc[5] * Math.Cos(dr * (input.doy - p[firstindex, 81]))
                        + p[firstindex, 85] * flags.swc[6] * Math.Cos(2.0 * dr * (input.doy - p[firstindex, 86])))
                  + p[firstindex, 83] * flags.swc[3] * Math.Cos(dr * (input.doy - p[firstindex, 84]))
                  + p[firstindex, 87] * flags.swc[4] * Math.Cos(2.0 * dr * (input.doy - p[firstindex, 88])))
                  * ((p[firstindex, 64] * plg[1, 2] + p[firstindex, 65] * plg[1, 4] + p[firstindex, 66] * plg[1, 6]
                  + p[firstindex, 74] * plg[1, 1] + p[firstindex, 75] * plg[1, 3] + p[firstindex, 76] * plg[1, 5]
                  ) * Math.Cos(dgtr * input.g_long)
                  + (p[firstindex, 90] * plg[1, 2] + p[firstindex, 91] * plg[1, 4] + p[firstindex, 92] * plg[1, 6]
                  + p[firstindex, 77] * plg[1, 1] + p[firstindex, 78] * plg[1, 3] + p[firstindex, 79] * plg[1, 5]
                  ) * Math.Sin(dgtr * input.g_long));
            }
            tt = 0;
            for (i = 0; i < 14; i++)
                tt += Math.Abs(flags.sw[i + 1]) * t[i];
            return tt;
        }

        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

        private void gtd7(nrlmsise_input input, nrlmsise_flags flags,
                          nrlmsise_output output)
        {
            double xlat;
            double xmm;
            int mn3 = 5;
            double[] zn3 = new double[5] { 32.5, 20.0, 15.0, 10.0, 0.0 };
            int mn2 = 4;
            double[] zn2 = new double[4] { 72.5, 55.0, 45.0, 32.5 };
            double altt;
            double zmix = 62.5;
            double tmp;
            double dm28m;
            double tz = 0.0;
            double dmc;
            double dmr;
            double dz28;
            nrlmsise_output soutput = new nrlmsise_output();
            int i;

            tselec(ref flags);

            /* Latitude variation of gravity (none for sw[2]=0) */
            xlat = input.g_lat;
            if (flags.sw[2] == 0)
                xlat = 45.0;
            glatf(xlat, out gsurf, out re);

            xmm = MSISData.pdm[2, 4];

            /* THERMOSPHERE / MESOSPHERE (above zn2[0]) */
            if (input.alt > zn2[0])
                altt = input.alt;
            else
                altt = zn2[0];

            tmp = input.alt;
            input.alt = altt;
            gts7(input, flags, soutput);
            altt = input.alt;
            input.alt = tmp;
            if (flags.sw[0] != 0)   /* metric adjustment */
                dm28m = dm28 * 1.0E6;
            else
                dm28m = dm28;
            output.t[0] = soutput.t[0];
            output.t[1] = soutput.t[1];
            if (input.alt >= zn2[0])
            {
                for (i = 0; i < 9; i++)
                    output.d[i] = soutput.d[i];
                return;
            }

            /*       LOWER MESOSPHERE/UPPER STRATOSPHERE (between zn3[0] and zn2[0])
             *         Temperature at nodes and gradients at end nodes
             *         Inverse temperature a linear function of spherical harmonics
             */
            meso_tgn2[0] = meso_tgn1[1];
            meso_tn2[0] = meso_tn1[4];
            meso_tn2[1] = MSISData.pma[0, 0] * MSISData.pavgm[0] / (1.0 - flags.sw[20] * glob7s(MSISData.pma, 0, input, flags));
            meso_tn2[2] = MSISData.pma[1, 0] * MSISData.pavgm[1] / (1.0 - flags.sw[20] * glob7s(MSISData.pma, 1, input, flags));
            meso_tn2[3] = MSISData.pma[2, 0] * MSISData.pavgm[2] / (1.0 - flags.sw[20] * flags.sw[22] * glob7s(MSISData.pma, 2, input, flags));
            meso_tgn2[1] = MSISData.pavgm[8] * MSISData.pma[9, 0] * (1.0 + flags.sw[20] * flags.sw[22] * glob7s(MSISData.pma, 9, input, flags)) * meso_tn2[3] * meso_tn2[3] / (Math.Pow((MSISData.pma[2, 0] * MSISData.pavgm[2]), 2.0));
            meso_tn3[0] = meso_tn2[3];

            if (input.alt < zn3[0])
            {
                /*       LOWER STRATOSPHERE AND TROPOSPHERE (below zn3[0])
                 *         Temperature at nodes and gradients at end nodes
                 *         Inverse temperature a linear function of spherical harmonics
                 */
                meso_tgn3[0] = meso_tgn2[1];
                meso_tn3[1] = MSISData.pma[3, 0] * MSISData.pavgm[3] / (1.0 - flags.sw[22] * glob7s(MSISData.pma, 3, input, flags));
                meso_tn3[2] = MSISData.pma[4, 0] * MSISData.pavgm[4] / (1.0 - flags.sw[22] * glob7s(MSISData.pma, 4, input, flags));
                meso_tn3[3] = MSISData.pma[5, 0] * MSISData.pavgm[5] / (1.0 - flags.sw[22] * glob7s(MSISData.pma, 5, input, flags));
                meso_tn3[4] = MSISData.pma[6, 0] * MSISData.pavgm[6] / (1.0 - flags.sw[22] * glob7s(MSISData.pma, 6, input, flags));
                meso_tgn3[1] = MSISData.pma[7, 0] * MSISData.pavgm[7] * (1.0 + flags.sw[22] * glob7s(MSISData.pma, 7, input, flags)) * meso_tn3[4] * meso_tn3[4] / (Math.Pow((MSISData.pma[6, 0] * MSISData.pavgm[6]), 2.0));
            }

            /* LINEAR TRANSITION TO FULL MIXING BELOW zn2[0] */

            dmc = 0;
            if (input.alt > zmix)
                dmc = 1.0 - (zn2[0] - input.alt) / (zn2[0] - zmix);
            dz28 = soutput.d[2];

            /**** N2 density ****/
            dmr = soutput.d[2] / dm28m - 1.0;
            output.d[2] = densm(input.alt, dm28m, xmm, ref tz, mn3, zn3, meso_tn3, meso_tgn3, mn2, zn2, meso_tn2, meso_tgn2);
            output.d[2] = output.d[2] * (1.0 + dmr * dmc);

            /**** HE density ****/
            dmr = soutput.d[0] / (dz28 * MSISData.pdm[0, 1]) - 1.0;
            output.d[0] = output.d[2] * MSISData.pdm[0, 1] * (1.0 + dmr * dmc);

            /**** O density ****/
            output.d[1] = 0;
            output.d[8] = 0;

            /**** O2 density ****/
            dmr = soutput.d[3] / (dz28 * MSISData.pdm[3, 1]) - 1.0;
            output.d[3] = output.d[2] * MSISData.pdm[3, 1] * (1.0 + dmr * dmc);

            /**** AR density ***/
            dmr = soutput.d[4] / (dz28 * MSISData.pdm[4, 1]) - 1.0;
            output.d[4] = output.d[2] * MSISData.pdm[4, 1] * (1.0 + dmr * dmc);

            /**** Hydrogen density ****/
            output.d[6] = 0;

            /**** Atomic nitrogen density ****/
            output.d[7] = 0;

            /**** Total mass density */
            output.d[5] = 1.66E-24 * (4.0 * output.d[0] + 16.0 * output.d[1] +
                               28.0 * output.d[2] + 32.0 * output.d[3] + 40.0 * output.d[4]
                               + output.d[6] + 14.0 * output.d[7]);

            if (flags.sw[0] != 0)
                output.d[5] = output.d[5] / 1000;

            /**** temperature at altitude ****/
            dd = densm(input.alt, 1.0, 0, ref tz, mn3, zn3, meso_tn3, meso_tgn3,
                             mn2, zn2, meso_tn2, meso_tgn2);
            output.t[1] = tz;

        }

        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

        private void gtd7d(nrlmsise_input input, nrlmsise_flags flags,
                         nrlmsise_output output)
        {
            gtd7(input, flags, output);
            output.d[5] = 1.66E-24 * (4.0 * output.d[0] + 16.0 * output.d[1] +
                             28.0 * output.d[2] + 32.0 * output.d[3] + 40.0 * output.d[4]
                             + output.d[6] + 14.0 * output.d[7] + 16.0 * output.d[8]);
        }

        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

        private void ghp7(nrlmsise_input input, nrlmsise_flags flags,
                          nrlmsise_output output, double press)
        {
            double bm = 1.3806E-19;
            double rgas = 831.4;
            double test = 0.00043;
            double ltest = 12;
            double pl, p;
            double zi = 0.0;
            double z;
            double cl, cl2;
            double ca, cd;
            double xn, xm, diff;
            double g, sh;
            int l;
            pl = Math.Log10(press);
            if (pl >= -5.0)
            {
                if (pl > 2.5)
                    zi = 18.06 * (3.00 - pl);
                else if ((pl > 0.075) && (pl <= 2.5))
                    zi = 14.98 * (3.08 - pl);
                else if ((pl > -1) && (pl <= 0.075))
                    zi = 17.80 * (2.72 - pl);
                else if ((pl > -2) && (pl <= -1))
                    zi = 14.28 * (3.64 - pl);
                else if ((pl > -4) && (pl <= -2))
                    zi = 12.72 * (4.32 - pl);
                else if (pl <= -4)
                    zi = 25.3 * (0.11 - pl);
                cl = input.g_lat / 90.0;
                cl2 = cl * cl;
                if (input.doy < 182)
                    cd = (1.0 - (double)input.doy) / 91.25;
                else
                    cd = ((double)input.doy) / 91.25 - 3.0;
                ca = 0;
                if ((pl > -1.11) && (pl <= -0.23))
                    ca = 1.0;
                if (pl > -0.23)
                    ca = (2.79 - pl) / (2.79 + 0.23);
                if ((pl <= -1.11) && (pl > -3))
                    ca = (-2.93 - pl) / (-2.93 + 1.11);
                z = zi - 4.87 * cl * cd * ca - 1.64 * cl2 * ca + 0.31 * ca * cl;
            }
            else
                z = 22.0 * Math.Pow((pl + 4.0), 2.0) + 110.0;

            /* iteration  loop */
            l = 0;
            do
            {
                l++;
                input.alt = z;
                gtd7(input, flags, output);
                z = input.alt;
                xn = output.d[0] + output.d[1] + output.d[2] + output.d[3] + output.d[4] + output.d[6] + output.d[7];
                p = bm * xn * output.t[1];
                if (flags.sw[0] != 0)
                    p = p * 1.0E-6;
                diff = pl - Math.Log10(p);
                if (Math.Sqrt(diff * diff) < test)
                    return;
                if (l == ltest)
                {
                    if (log.IsErrorEnabled)
                        log.Error("ERROR: ghp7 not converging for press " + press + ", diff " + diff);
                    return;
                }
                xm = output.d[5] / xn / 1.66E-24;
                if (flags.sw[0] != 0)
                    xm = xm * 1.0E3;
                g = gsurf / (Math.Pow((1.0 + z / re), 2.0));
                sh = rgas * output.t[1] / (xm * g);

                /* new altitude estimate uMath.Sing scale height */
                if (l < 6)
                    z = z - sh * diff * 2.302;
                else
                    z = z - sh * diff;
            } while (1 == 1);
        }

        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

        private void gts7(nrlmsise_input input, nrlmsise_flags flags,
                          nrlmsise_output output)
        {
            /*     Thermospheric portion of NRLMSISE-00
             *     See GTD7 for more extensive comments
             *     alt > 72.5 km!
             */
            double za;
            int i, j;
            double ddum, z;
            double[] zn1 = new double[5] { 120.0, 110.0, 100.0, 90.0, 72.5 };
            double tinf;
            int mn1 = 5;
            double g0;
            double tlb;
            double s, z0, t0, tr12;
            double db01, db04, db14, db16, db28, db32, db40, db48;
            double zh28, zh04, zh16, zh32, zh40, zh01, zh14;
            double zhm28, zhm04, zhm16, zhm32, zhm40, zhm01, zhm14;
            double xmd;
            double b28, b04, b16, b32, b40, b01, b14;
            double tz = 0;
            double g28, g4, g16, g32, g40, g1, g14;
            double zhf, xmm;
            double zc04, zc16, zc32, zc40, zc01, zc14;
            double hc04, hc16, hc32, hc40, hc01, hc14;
            double hcc16, hcc32, hcc01, hcc14;
            double zcc16, zcc32, zcc01, zcc14;
            double rc16, rc32, rc01, rc14;
            double rl;
            double g16h, db16h, tho, zsht, zmho, zsho;
            double dgtr = 1.74533E-2;
            double dr = 1.72142E-2;
            double[] alpha = new double[9] { -0.38, 0.0, 0.0, 0.0, 0.17, 0.0, -0.38, 0.0, 0.0 };
            double[] altl = new double[8] { 200.0, 300.0, 160.0, 250.0, 240.0, 450.0, 320.0, 450.0 };
            double dd;
            double hc216, hcc232;
            za = MSISData.pdl[1, 15];
            zn1[0] = za;
            for (j = 0; j < 9; j++)
                output.d[j] = 0;

            /* TINF VARIATIONS NOT IMPORTANT BELOW ZA OR ZN1(1) */
            if (input.alt > zn1[0])
                tinf = MSISData.ptm[0] * MSISData.pt[0] *
                  (1.0 + flags.sw[16] * globe7(MSISData.pt, input, flags));
            else
                tinf = MSISData.ptm[0] * MSISData.pt[0];
            output.t[0] = tinf;

            /*  GRADIENT VARIATIONS NOT IMPORTANT BELOW ZN1(5) */
            if (input.alt > zn1[4])
                g0 = MSISData.ptm[3] * MSISData.ps[0] *
                  (1.0 + flags.sw[19] * globe7(MSISData.ps, input, flags));
            else
                g0 = MSISData.ptm[3] * MSISData.ps[0];
            tlb = MSISData.ptm[1] * (1.0 + flags.sw[17] * globe7(MSISData.pd[3], input, flags)) * MSISData.pd[3][0];
            s = g0 / (tinf - tlb);

            /*      Lower thermosphere temp variations not significant for
             *       density above 300 km */
            if (input.alt < 300.0)
            {
                meso_tn1[1] = MSISData.ptm[6] * MSISData.ptl[0, 0] / (1.0 - flags.sw[18] * glob7s(MSISData.ptl, 0, input, flags));
                meso_tn1[2] = MSISData.ptm[2] * MSISData.ptl[1, 0] / (1.0 - flags.sw[18] * glob7s(MSISData.ptl, 1, input, flags));
                meso_tn1[3] = MSISData.ptm[7] * MSISData.ptl[2, 0] / (1.0 - flags.sw[18] * glob7s(MSISData.ptl, 2, input, flags));
                meso_tn1[4] = MSISData.ptm[4] * MSISData.ptl[3, 0] / (1.0 - flags.sw[18] * flags.sw[20] * glob7s(MSISData.ptl, 3, input, flags));
                meso_tgn1[1] = MSISData.ptm[8] * MSISData.pma[8, 0] * (1.0 + flags.sw[18] * flags.sw[20] * glob7s(MSISData.pma, 8, input, flags)) * meso_tn1[4] * meso_tn1[4] / (Math.Pow((MSISData.ptm[4] * MSISData.ptl[3, 0]), 2.0));
            }
            else
            {
                meso_tn1[1] = MSISData.ptm[6] * MSISData.ptl[0, 0];
                meso_tn1[2] = MSISData.ptm[2] * MSISData.ptl[1, 0];
                meso_tn1[3] = MSISData.ptm[7] * MSISData.ptl[2, 0];
                meso_tn1[4] = MSISData.ptm[4] * MSISData.ptl[3, 0];
                meso_tgn1[1] = MSISData.ptm[8] * MSISData.pma[8, 0] * meso_tn1[4] * meso_tn1[4] / (Math.Pow((MSISData.ptm[4] * MSISData.ptl[3, 0]), 2.0));
            }

            z0 = zn1[3];
            t0 = meso_tn1[3];
            tr12 = 1.0;

            /* N2 variation factor at Zlb */
            g28 = flags.sw[21] * globe7(MSISData.pd[2], input, flags);

            /* VARIATION OF TURBOPAUSE HEIGHT */
            zhf = MSISData.pdl[1, 24] * (1.0 + flags.sw[5] * MSISData.pdl[0, 24] * Math.Sin(dgtr * input.g_lat) * Math.Cos(dr * (input.doy - MSISData.pt[13])));
            output.t[0] = tinf;
            xmm = MSISData.pdm[2, 4];
            z = input.alt;


            /**** N2 DENSITY ****/

            /* Diffusive density at Zlb */
            db28 = MSISData.pdm[2, 0] * Math.Exp(g28) * MSISData.pd[2][0];
            /* Diffusive density at Alt */
            output.d[2] = densu(z, db28, tinf, tlb, 28.0, alpha[2], ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
            dd = output.d[2];
            /* Turbopause */
            zh28 = MSISData.pdm[2, 2] * zhf;
            zhm28 = MSISData.pdm[2, 3] * MSISData.pdl[1, 5];
            xmd = 28.0 - xmm;
            /* Mixed density at Zlb */
            b28 = densu(zh28, db28, tinf, tlb, xmd, (alpha[2] - 1.0), ref tz, MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
            if ((flags.sw[15] != 0) && (z <= altl[2]))
            {
                /*  Mixed density at Alt */
                dm28 = densu(z, b28, tinf, tlb, xmm, alpha[2], ref tz, MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                /*  Net density at Alt */
                output.d[2] = dnet(output.d[2], dm28, zhm28, xmm, 28.0);
            }


            /**** HE DENSITY ****/

            /*   Density variation factor at Zlb */
            g4 = flags.sw[21] * globe7(MSISData.pd[0], input, flags);
            /*  Diffusive density at Zlb */
            db04 = MSISData.pdm[0, 0] * Math.Exp(g4) * MSISData.pd[0][0];
            /*  Diffusive density at Alt */
            output.d[0] = densu(z, db04, tinf, tlb, 4.0, alpha[0], ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
            dd = output.d[0];
            if ((flags.sw[15] != 0) && (z < altl[0]))
            {
                /*  Turbopause */
                zh04 = MSISData.pdm[0, 2];
                /*  Mixed density at Zlb */
                b04 = densu(zh04, db04, tinf, tlb, 4.0 - xmm, alpha[0] - 1.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                /*  Mixed density at Alt */
                dm04 = densu(z, b04, tinf, tlb, xmm, 0.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                zhm04 = zhm28;
                /*  Net density at Alt */
                output.d[0] = dnet(output.d[0], dm04, zhm04, xmm, 4.0);
                /*  Correction to specified mixing ratio at ground */
                rl = Math.Log(b28 * MSISData.pdm[0, 1] / b04);
                zc04 = MSISData.pdm[0, 4] * MSISData.pdl[1, 0];
                hc04 = MSISData.pdm[0, 5] * MSISData.pdl[1, 1];
                /*  Net density corrected at Alt */
                output.d[0] = output.d[0] * ccor(z, rl, hc04, zc04);
            }


            /**** O DENSITY ****/

            /*  Density variation factor at Zlb */
            g16 = flags.sw[21] * globe7(MSISData.pd[1], input, flags);
            /*  Diffusive density at Zlb */
            db16 = MSISData.pdm[1, 0] * Math.Exp(g16) * MSISData.pd[1][0];
            /*   Diffusive density at Alt */
            output.d[1] = densu(z, db16, tinf, tlb, 16.0, alpha[1], ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
            dd = output.d[1];
            if ((flags.sw[15] != 0) && (z <= altl[1]))
            {
                /*   Turbopause */
                zh16 = MSISData.pdm[1, 2];
                /*  Mixed density at Zlb */
                b16 = densu(zh16, db16, tinf, tlb, 16.0 - xmm, (alpha[1] - 1.0), ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                /*  Mixed density at Alt */
                dm16 = densu(z, b16, tinf, tlb, xmm, 0.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                zhm16 = zhm28;
                /*  Net density at Alt */
                output.d[1] = dnet(output.d[1], dm16, zhm16, xmm, 16.0);
                rl = MSISData.pdm[1, 1] * MSISData.pdl[1, 16] * (1.0 + flags.sw[1] * MSISData.pdl[0, 23] * (input.f107A - 150.0));
                hc16 = MSISData.pdm[1, 5] * MSISData.pdl[1, 3];
                zc16 = MSISData.pdm[1, 4] * MSISData.pdl[1, 2];
                hc216 = MSISData.pdm[1, 5] * MSISData.pdl[1, 4];
                output.d[1] = output.d[1] * ccor2(z, rl, hc16, zc16, hc216);
                /*   Chemistry correction */
                hcc16 = MSISData.pdm[1, 7] * MSISData.pdl[1, 13];
                zcc16 = MSISData.pdm[1, 6] * MSISData.pdl[1, 12];
                rc16 = MSISData.pdm[1, 3] * MSISData.pdl[1, 14];
                /*  Net density corrected at Alt */
                output.d[1] = output.d[1] * ccor(z, rc16, hcc16, zcc16);
            }


            /**** O2 DENSITY ****/

            /*   Density variation factor at Zlb */
            g32 = flags.sw[21] * globe7(MSISData.pd[4], input, flags);
            /*  Diffusive density at Zlb */
            db32 = MSISData.pdm[3, 0] * Math.Exp(g32) * MSISData.pd[4][0];
            /*   Diffusive density at Alt */
            output.d[3] = densu(z, db32, tinf, tlb, 32.0, alpha[3], ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
            dd = output.d[3];
            if (flags.sw[15] != 0)
            {
                if (z <= altl[3])
                {
                    /*   Turbopause */
                    zh32 = MSISData.pdm[3, 2];
                    /*  Mixed density at Zlb */
                    b32 = densu(zh32, db32, tinf, tlb, 32.0 - xmm, alpha[3] - 1.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                    /*  Mixed density at Alt */
                    dm32 = densu(z, b32, tinf, tlb, xmm, 0.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                    zhm32 = zhm28;
                    /*  Net density at Alt */
                    output.d[3] = dnet(output.d[3], dm32, zhm32, xmm, 32.0);
                    /*   Correction to specified mixing ratio at ground */
                    rl = Math.Log(b28 * MSISData.pdm[3, 1] / b32);
                    hc32 = MSISData.pdm[3, 5] * MSISData.pdl[1, 7];
                    zc32 = MSISData.pdm[3, 4] * MSISData.pdl[1, 6];
                    output.d[3] = output.d[3] * ccor(z, rl, hc32, zc32);
                }
                /*  Correction for general departure from diffusive equilibrium above Zlb */
                hcc32 = MSISData.pdm[3, 7] * MSISData.pdl[1, 22];
                hcc232 = MSISData.pdm[3, 7] * MSISData.pdl[0, 22];
                zcc32 = MSISData.pdm[3, 6] * MSISData.pdl[1, 21];
                rc32 = MSISData.pdm[3, 3] * MSISData.pdl[1, 23] * (1.0 + flags.sw[1] * MSISData.pdl[0, 23] * (input.f107A - 150.0));
                /*  Net density corrected at Alt */
                output.d[3] = output.d[3] * ccor2(z, rc32, hcc32, zcc32, hcc232);
            }


            /**** AR DENSITY ****/

            /*   Density variation factor at Zlb */
            g40 = flags.sw[20] * globe7(MSISData.pd[5], input, flags);
            /*  Diffusive density at Zlb */
            db40 = MSISData.pdm[4, 0] * Math.Exp(g40) * MSISData.pd[5][0];
            /*   Diffusive density at Alt */
            output.d[4] = densu(z, db40, tinf, tlb, 40.0, alpha[4], ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
            dd = output.d[4];
            if ((flags.sw[15] != 0) && (z <= altl[4]))
            {
                /*   Turbopause */
                zh40 = MSISData.pdm[4, 2];
                /*  Mixed density at Zlb */
                b40 = densu(zh40, db40, tinf, tlb, 40.0 - xmm, alpha[4] - 1.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                /*  Mixed density at Alt */
                dm40 = densu(z, b40, tinf, tlb, xmm, 0.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                zhm40 = zhm28;
                /*  Net density at Alt */
                output.d[4] = dnet(output.d[4], dm40, zhm40, xmm, 40.0);
                /*   Correction to specified mixing ratio at ground */
                rl = Math.Log(b28 * MSISData.pdm[4, 1] / b40);
                hc40 = MSISData.pdm[4, 5] * MSISData.pdl[1, 9];
                zc40 = MSISData.pdm[4, 4] * MSISData.pdl[1, 8];
                /*  Net density corrected at Alt */
                output.d[4] = output.d[4] * ccor(z, rl, hc40, zc40);
            }


            /**** HYDROGEN DENSITY ****/

            /*   Density variation factor at Zlb */
            g1 = flags.sw[21] * globe7(MSISData.pd[6], input, flags);
            /*  Diffusive density at Zlb */
            db01 = MSISData.pdm[5, 0] * Math.Exp(g1) * MSISData.pd[6][0];
            /*   Diffusive density at Alt */
            output.d[6] = densu(z, db01, tinf, tlb, 1.0, alpha[6], ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
            dd = output.d[6];
            if ((flags.sw[15] != 0) && (z <= altl[6]))
            {
                /*   Turbopause */
                zh01 = MSISData.pdm[5, 2];
                /*  Mixed density at Zlb */
                b01 = densu(zh01, db01, tinf, tlb, 1.0 - xmm, alpha[6] - 1.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                /*  Mixed density at Alt */
                dm01 = densu(z, b01, tinf, tlb, xmm, 0.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                zhm01 = zhm28;
                /*  Net density at Alt */
                output.d[6] = dnet(output.d[6], dm01, zhm01, xmm, 1.0);
                /*   Correction to specified mixing ratio at ground */
                rl = Math.Log(b28 * MSISData.pdm[5, 1] * Math.Sqrt(MSISData.pdl[1, 17] * MSISData.pdl[1, 17]) / b01);
                hc01 = MSISData.pdm[5, 5] * MSISData.pdl[1, 11];
                zc01 = MSISData.pdm[5, 4] * MSISData.pdl[1, 10];
                output.d[6] = output.d[6] * ccor(z, rl, hc01, zc01);
                /*   Chemistry correction */
                hcc01 = MSISData.pdm[5, 7] * MSISData.pdl[1, 19];
                zcc01 = MSISData.pdm[5, 6] * MSISData.pdl[1, 18];
                rc01 = MSISData.pdm[5, 3] * MSISData.pdl[1, 20];
                /*  Net density corrected at Alt */
                output.d[6] = output.d[6] * ccor(z, rc01, hcc01, zcc01);
            }


            /**** ATOMIC NITROGEN DENSITY ****/

            /*   Density variation factor at Zlb */
            g14 = flags.sw[21] * globe7(MSISData.pd[7], input, flags);
            /*  Diffusive density at Zlb */
            db14 = MSISData.pdm[6, 0] * Math.Exp(g14) * MSISData.pd[7][0];
            /*   Diffusive density at Alt */
            output.d[7] = densu(z, db14, tinf, tlb, 14.0, alpha[7], ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
            dd = output.d[7];
            if ((flags.sw[15] != 0) && (z <= altl[7]))
            {
                /*   Turbopause */
                zh14 = MSISData.pdm[6, 2];
                /*  Mixed density at Zlb */
                b14 = densu(zh14, db14, tinf, tlb, 14.0 - xmm, alpha[7] - 1.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                /*  Mixed density at Alt */
                dm14 = densu(z, b14, tinf, tlb, xmm, 0.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
                zhm14 = zhm28;
                /*  Net density at Alt */
                output.d[7] = dnet(output.d[7], dm14, zhm14, xmm, 14.0);
                /*   Correction to specified mixing ratio at ground */
                rl = Math.Log(b28 * MSISData.pdm[6, 1] * Math.Sqrt(MSISData.pdl[0, 2] * MSISData.pdl[0, 2]) / b14);
                hc14 = MSISData.pdm[6, 5] * MSISData.pdl[0, 1];
                zc14 = MSISData.pdm[6, 4] * MSISData.pdl[0, 0];
                output.d[7] = output.d[7] * ccor(z, rl, hc14, zc14);
                /*   Chemistry correction */
                hcc14 = MSISData.pdm[6, 7] * MSISData.pdl[0, 4];
                zcc14 = MSISData.pdm[6, 6] * MSISData.pdl[0, 3];
                rc14 = MSISData.pdm[6, 3] * MSISData.pdl[0, 5];
                /*  Net density corrected at Alt */
                output.d[7] = output.d[7] * ccor(z, rc14, hcc14, zcc14);
            }


            /**** Anomalous OXYGEN DENSITY ****/

            g16h = flags.sw[21] * globe7(MSISData.pd[8], input, flags);
            db16h = MSISData.pdm[7, 0] * Math.Exp(g16h) * MSISData.pd[8][0];
            tho = MSISData.pdm[7, 9] * MSISData.pdl[0, 6];
            dd = densu(z, db16h, tho, tho, 16.0, alpha[8], ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
            zsht = MSISData.pdm[7, 5];
            zmho = MSISData.pdm[7, 4];
            zsho = scalh(zmho, 16.0, tho);
            output.d[8] = dd * Math.Exp(-zsht / zsho * (Math.Exp(-(z - zmho) / zsht) - 1.0));


            /* total mass density */
            output.d[5] = 1.66E-24 * (4.0 * output.d[0] + 16.0 * output.d[1] + 28.0 * output.d[2] + 32.0 * output.d[3] + 40.0 * output.d[4] + output.d[6] + 14.0 * output.d[7]);
            db48 = 1.66E-24 * (4.0 * db04 + 16.0 * db16 + 28.0 * db28 + 32.0 * db32 + 40.0 * db40 + db01 + 14.0 * db14);



            /* temperature */
            z = Math.Sqrt(input.alt * input.alt);
            ddum = densu(z, 1.0, tinf, tlb, 0.0, 0.0, ref output.t[1], MSISData.ptm[5], s, mn1, zn1, meso_tn1, meso_tgn1);
            if (flags.sw[0] != 0)
            {
                for (i = 0; i < 9; i++)
                    output.d[i] = output.d[i] * 1.0E6;
                output.d[5] = output.d[5] / 1000;
            }
        }
    }

    public sealed class nrlmsise_flags
    {
        public int[] switches = new int[24];
        public double[] sw = new double[24];
        public double[] swc = new double[24];
    };

    public sealed class ap_array
    {
        public double[] a = new double[7];
    };

    class nrlmsise_input
    {
        public int year;      /* year, currently ignored                           */
        public int doy;       /* day of year                                       */
        public double sec;    /* seconds in day (UT)                               */
        public double alt;    /* altitude in kilometers                            */
        public double g_lat;  /* geodetic latitude                                 */
        public double g_long; /* geodetic longitude                                */
        public double lst;    /* local apparent solar time (hours), see note below */
        public double f107A;  /* 81 day average of F10.7 flux (centered on DOY)    */
        public double f107;   /* daily F10.7 flux for previous day                 */
        public double ap;     /* magnetic index(daily)                             */
        public ap_array ap_a = new ap_array(); /* see above */
    };

    public sealed class nrlmsise_output
    {
        public double[] d = new double[9];   /* densities    */
        public double[] t = new double[2];   /* temperatures */
    };
}
