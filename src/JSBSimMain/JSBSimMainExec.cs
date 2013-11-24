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

namespace JSBSimMain
{

    using System;
    using System.Collections;
    using System.IO;
    using System.Text;

    // Import log4net classes.
    using log4net;

    using JSBSim;
    using JSBSim.Script;
    using CommonUtils.MathLib;
    using Nini.Config;

    class JSBSimMainExec
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
        

        static void Main(string[] args)
        {
            JSBSimMainExec main = new JSBSimMainExec();
            main.CheckOptions(args);
            main.InitLog();
            main.Run();
        }

        protected bool realtime = false;
        protected bool suspend = false;
        protected bool scripted = false;

        protected string logOutputName;
        protected string logDirectiveName;
        protected string rootDir = ".";
        protected string scriptName = null;
        protected string resetName = null;
        protected string aircraftName = null;
        protected InitialCondition IC;

        protected Script script;

        protected string fileLog4Net = "Log4Net.config";

        public void Run()
        {
            if (!rootDir.EndsWith("/"))
                rootDir += "/";
            FDMExecutive fdm = new FDMExecutive();
            fdm.AircraftPath = rootDir + "aircraft";
            fdm.EnginePath = rootDir + "engine";

            if (scriptName != null) // SCRIPTED CASE
            {
                scriptName = rootDir + scriptName;
                script = new Script(fdm);
                script.LoadScript(scriptName);
                scripted = true;
            }
            else if (aircraftName != null || resetName != null)
            {        // form jsbsim <acname> <resetfile>

                aircraftName = rootDir + aircraftName;
                resetName = rootDir + resetName;
                fdm.LoadModel(rootDir + "aircraft", rootDir + "engine", aircraftName);

                IC = fdm.GetIC;
                IC.Load(resetName, true);

                Trim fgt = new Trim(fdm, TrimMode.Full);
                if (!fgt.DoTrim())
                {
                    log.Debug("Trim Failed");
                }
                fgt.Report();
            }
            else
            {
                Console.WriteLine("  No Aircraft, Script, or Reset information given\n");
                return;
            }

            long clockTicks = 0, totalPauseTicks = 0, pauseTicks = 0;
            long initialClockTicks = System.DateTime.Now.Ticks;

            double newFiveSecondValue = 0.0;
            bool scriptResult = true;
            bool result = fdm.Run();
            if (suspend) fdm.Hold();

            //Displays a pattern defined by the universal sortable date/time pattern
            const string timeFormat = "yyyy-MM-dd HH:mm:ss.fffffff";

            //strftime(s, 99, "%A %B %D %Y %X", localtime(&tod));
            if (log.IsInfoEnabled)
                log.Info("Start: " + DateTime.Now.ToString(timeFormat));

            // if running realtime, throttle the execution, else just run flat-out fast
            // if suspended, then don't increment realtime counter
            while (result && scriptResult)
            {
                if (!(fdm.Holding() || fdm.State.IsIntegrationSuspended))
                    if (realtime)
                    { // realtime mode

                        // track times when simulation is suspended
                        if (pauseTicks != 0)
                        {
                            totalPauseTicks += clockTicks - pauseTicks;
                            pauseTicks = 0;
                        }

                        while ((clockTicks - totalPauseTicks) / TimeSpan.TicksPerSecond >= fdm.State.SimTime)
                        {
                            if (scripted)
                            {
                                if (!script.RunScript())
                                {
                                    scriptResult = false;
                                    break;
                                }
                            }
                            result = fdm.Run();

                            // print out status every five seconds
                            if (fdm.State.SimTime >= newFiveSecondValue)
                            {
                                if (log.IsInfoEnabled)
                                    log.Info("Simulation elapsed time: " + fdm.State.SimTime);
                                newFiveSecondValue += 5.0;
                            }
                            if (fdm.Holding()) break;
                        }

                    }
                    else
                    { // batch mode
                        if (scripted)
                        {
                            if (!script.RunScript())
                            {
                                scriptResult = false;
                                break;
                            }
                        }
                        result = fdm.Run();

                    }
                else
                { // Suspended
                    if (pauseTicks == 0)
                    {
                        pauseTicks = System.DateTime.Now.Ticks - initialClockTicks; // remember start of pause
                        Console.WriteLine("  ... Holding ...\n\n");
                    }
                    result = fdm.Run();
                }
                clockTicks = System.DateTime.Now.Ticks - initialClockTicks;
            }
            if (log.IsInfoEnabled)
            {
                log.Info("End: " + DateTime.Now.ToString(timeFormat));
                log.Info("Seconds processor time used: " + (double)(clockTicks - totalPauseTicks) / (double)TimeSpan.TicksPerSecond + " seconds");
            }

        }

        private const string ConfigTag = "JSBSIM";

        /// <summary>
        /// Loads all configuration values.
        /// </summary>
        public void CheckOptions(string[] args)
        {
            ArgvConfigSource source = new ArgvConfigSource(args);

            source.AddSwitch(ConfigTag, "help", "h");
            source.AddSwitch(ConfigTag, "version", "v");
            source.AddSwitch(ConfigTag, "outputlogfile", "o");
            source.AddSwitch(ConfigTag, "logdirectivefile", "l");
            source.AddSwitch(ConfigTag, "root", "r");
            source.AddSwitch(ConfigTag, "aircraft", "a");
            source.AddSwitch(ConfigTag, "script", "sc");
            source.AddSwitch(ConfigTag, "realtime", "rt");
            source.AddSwitch(ConfigTag, "suspend", "s");
            source.AddSwitch(ConfigTag, "initfile", "i");

            if (args.Length == 0)
            {
                UseOptions();
            }
            else
            {
                string param = source.Configs[ConfigTag].Get("help");
                if (param != null && param.Equals("true"))
                    UseOptions();

                param = source.Configs[ConfigTag].Get("version");
                if (param != null && param.Equals("true"))
                {
                    if (log.IsInfoEnabled)
                        log.Info("  JSBSim Version: " + FDMExecutive.GetVersion() + "\n");
                    return;
                }

                param = source.Configs[ConfigTag].Get("realtime");
                if (param != null && param.Equals("true"))
                    realtime = true;

                param = source.Configs[ConfigTag].Get("suspend");
                if (param != null && param.Equals("true"))
                    realtime = true;

                param = source.Configs[ConfigTag].Get("outputlogfile");
                if (param != null)
                {
                    if (param.Equals("true"))
                    {
                        logOutputName = "JSBout.csv";
                        if (log.IsWarnEnabled)
                            log.Warn("  Output log file name not valid or not understood. Using JSBout.csv as default");
                    } else
                        logOutputName = param;
                }

                param = source.Configs[ConfigTag].Get("logdirectivefile");
                if (param != null)
                {
                    if (param.Equals("true"))
                    {
                        if (log.IsWarnEnabled)
                            log.Warn("    Log directives file not valid or not understood.");
                    }
                    else
                        logDirectiveName = param;
                }

                param = source.Configs[ConfigTag].Get("root");
                if (param != null)
                {
                    if (param.Equals("true"))
                    {
                        if (log.IsWarnEnabled)
                            log.Warn("  Root directory not valid or not understood.");
                    }
                    else
                        rootDir = param;
                }

                param = source.Configs[ConfigTag].Get("aircraft");
                if (param != null)
                {
                    if (param.Equals("true"))
                    {
                        if (log.IsWarnEnabled)
                            log.Warn("  Aircraft name not valid or not understood");
                    }
                    else
                        aircraftName = param;
                }

                param = source.Configs[ConfigTag].Get("script");
                if (param != null)
                {
                    if (param.Equals("true"))
                    {
                        if (log.IsWarnEnabled)
                            log.Warn("  Script name not valid or not understood.");
                    }
                    else
                        scriptName = param;
                }

                param = source.Configs[ConfigTag].Get("initfile");
                if (param != null)
                {
                    if (param.Equals("true"))
                    {
                        if (log.IsWarnEnabled)
                            log.Warn("  Reset name not valid or not understood.");
                    }
                    else
                        resetName = param;
                }

            }
        }

        public void UseOptions()
        {
            Console.WriteLine("  JSBSim version " + FDMExecutive.GetVersion() + "\n");
            Console.WriteLine("  Usage: jsbsim <options>\n");
            Console.WriteLine("  options:");
            Console.WriteLine("    --help  returns this message");
            Console.WriteLine("    --version  returns the version number");
            Console.WriteLine("    --outputlogfile=<filename>  sets the name of the data output file");
            Console.WriteLine("    --logdirectivefile=<filename>  specifies the name of the data logging directives file");
            Console.WriteLine("    --root=<path>  specifies the root of the configuration file directory");
            Console.WriteLine("    --aircraft=<filename>  specifies the name of the aircraft to be modeled");
            Console.WriteLine("    --script=<filename>  specifies a script to run");
            Console.WriteLine("    --realtime  specifies to run in actual real world time");
            Console.WriteLine("    --suspend  specifies to suspend the simulation after initialization");
            Console.WriteLine("    --initfile=<filename>  specifies an initilization file\n");
            Console.WriteLine("  NOTE: There can be no spaces around the = sign when");
            Console.WriteLine("        an option is followed by a filename\n");
        }

        const string log4NetDefaultConfig = @"<?xml version=""1.0"" encoding=""utf-32"" ?>
                        <log4net>
                            <!-- A1 is set to be a ConsoleAppender -->
                            <appender name=""Long"" type=""log4net.Appender.ConsoleAppender"">
                                <!-- A1 uses PatternLayout -->
                                <layout type=""log4net.Layout.PatternLayout"">
                                    <conversionPattern value=""%-4timestamp [%thread] %-5level %logger %ndc - %message%newline"" />
                                </layout>
                            </appender>

                            <appender name=""Short"" type=""log4net.Appender.ConsoleAppender"">
                                <!-- A1 uses PatternLayout -->
                                <layout type=""log4net.Layout.PatternLayout"">
                                    <conversionPattern value=""%message%newline"" />
                                </layout>
                            </appender>

                            <!-- Set root logger level to DEBUG and its only appender to A1 -->
                            <root>
                                <level value=""INFO"" />
                                <appender-ref ref=""Short"" />
                            </root>
                        </log4net>";

        public void InitLog()
        {
            FileInfo logFile = new System.IO.FileInfo(fileLog4Net);
            if (logFile.Exists)
            {
                // Log4Net is configured using a XmlConfigurator.
                log4net.Config.XmlConfigurator.Configure(logFile);
            }
            else
            {
#if NNDEBUG
                // Set up a simple configuration that logs on the console.
                log4net.Config.BasicConfigurator.Configure();
#else
                log4net.Config.XmlConfigurator.Configure(new MemoryStream(UnicodeEncoding.UTF32.GetBytes(log4NetDefaultConfig)));
#endif

            }
        }
    }
}
