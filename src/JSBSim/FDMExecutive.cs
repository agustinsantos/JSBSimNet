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
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Reflection;
    using System.IO;

    // Import log4net classes.
    using log4net;

    using JSBSim.InputOutput;
    using JSBSim.Models;
    using JSBSim.Models.Propulsion;
    using JSBSim.MathValues;


    /// <summary>
    /// Encapsulates the JSBSim simulation executive.
    /// This class is the interface class through which all other simulation classes
    /// are instantiated, initialized, and run. When integrated with FlightGear (or
    /// other flight simulator) this class is typically instantiated by an interface
    /// class on the simulator side.
    /// 
    /// At the time of simulation initialization, the interface
    /// class creates an instance of this executive class. The
    /// executive is subsequently directed to load the chosen aircraft specification
    /// file:
    /// 
    /// <code>
    /// fdmex = new FGFDMExec( … );
    /// result = fdmex.LoadModel( … );
    /// </code>
    /// 
    /// When an aircraft model is loaded the config file is parsed and for each of the
    /// sections of the config file (propulsion, flight control, etc.) the
    /// corresponding "ReadXXX()" method is called. From within this method the
    /// "Load()" method of that system is called (e.g. LoadFCS).
    /// Subsequent to the creation of the executive and loading of the model,
    /// initialization is performed. Initialization involves copying control inputs
    /// into the appropriate JSBSim data storage locations, configuring it for the set
    /// of user supplied initial conditions, and then copying state variables from
    /// JSBSim. The state variables are used to drive the instrument displays and to
    /// place the vehicle model in world space for visual rendering:
    /// 
    /// <code>
    /// copy_to_JSBsim(); // copy control inputs to JSBSim
    /// fdmex.RunIC(); // loop JSBSim once w/o integrating
    /// copy_from_JSBsim(); // update the bus
    /// </code>
    /// 
    /// Once initialization is complete, cyclic execution proceeds:
    /// 
    /// <code>
    /// copy_to_JSBsim(); // copy control inputs to JSBSim
    /// fdmex.Run(); // execute JSBSim
    /// copy_from_JSBsim(); // update the bus
    /// </code>
    /// 
    /// JSBSim can be used in a standalone mode by creating a compact stub program
    /// that effectively performs the same progression of steps as outlined above for
    /// the integrated version, but with two exceptions. First, the copy_to_JSBSim()
    /// and copy_from_JSBSim() functions are not used because the control inputs are
    /// handled directly by the scripting facilities and outputs are handled by the
    /// output (data logging) class. Second, the name of a script file can be supplied
    /// to the stub program. Scripting (see FGScript) provides a way to supply command
    /// inputs to the simulation:
    /// 
    /// <code>
    /// FDMExec = new FGFDMExec();
    /// Script = new FGScript( … );
    /// Script.LoadScript( ScriptName ); // the script loads the aircraft and ICs
    /// result = FDMExec.Run();
    /// while (result) { // cyclic execution
    ///   if (Scripted) if (!Script.RunScript()) break; // execute script
    ///   result = FDMExec.Run(); // execute JSBSim
    /// }
    /// </code>
    /// 
    /// The standalone mode has been useful for verifying changes before committing
    /// updates to the source code repository. It is also useful for running sets of
    /// tests that reveal some aspects of simulated aircraft performance, such as
    /// range, time-to-climb, takeoff distance, etc.
    /// 
    /// This code is based on FGFDMExec written by Jon S. Berndt
    /// </summary>
    /// <property name="simulator/do_trim">
    /// (write only) Can be set to the integer equivalent to one of
    /// tLongitudinal (0), tFull (1), tGround (2), tPullup (3),
    /// tCustom (4), tTurn (5). Setting this to a legal value
    /// (such as by a script) causes a trim to be performed. This
    /// roperty actually maps toa function call of DoTrim().
    /// </property>
    public class FDMExecutive
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

        public FDMExecutive()
            : this(null)
        {
        }

        internal Inertial GetInertial()
        {
            return models[(int)eModels.eInertial] as Inertial;
        }

        /// Default constructor
        public FDMExecutive(PropertyManager root)
        {
            IdFDM = FDMctr;
            FDMctr++;

            //TODO read JSBSIM_DEBUG var and set debug level.

            if (root == null)
                propManager = new PropertyManager();
            else
                propManager = root;

            //instance = propManager.GetPropertyNode("/fdm/jsbsim", IdFDM, true);

            if (log.IsInfoEnabled && IdFDM == 0)
            {
                log.Info("JSBSim Flight Dynamics Model v");
                log.Info("JSBSim version [cfg file spec v " + neededCfgVersion + "]");
                log.Info("JSBSim startup beginning ...");

            }

            // this is here to catch errors in binding member functions
            // to the property tree.
            try
            {
                Allocate();
            }
            catch (Exception any)
            {
                if (log.IsWarnEnabled) log.Warn("Error FDMExecutive" + any.Message);
                throw any;
            }

            constructing = true;
            propManager.TieInt32("simulation/do_trim", null, this.DoTrim);
            constructing = false;
        }
        /// Default constructor
        public FDMExecutive(PropertyManager root = null, int[] fdmctr = null)
        { throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM"); }

        /// <summary>
        /// This list of enums is very important! The order in which models are listed
        /// here determines the order of execution of the models.
        ///
        /// There are some conditions that need to be met :
        /// 1. FCS can request mass geometry changes via the inertia/pointmass-*
        ///    properties so it must be executed before MassBalance
        /// 2. MassBalance must be executed before Propulsion, Aerodynamics,
        ///    GroundReactions, ExternalReactions and BuoyantForces to ensure that
        ///    their moments are computed with the updated CG position.
        /// </summary>
        public enum eModels
        {
            ePropagate = 0,
            eInput,
            eInertial,
            eAtmosphere,
            eWinds,
            eSystems,
            eMassBalance,
            eAuxiliary,
            ePropulsion,
            eAerodynamics,
            eGroundReactions,
            eExternalReactions,
            eBuoyantForces,
            eAircraft,
            eAccelerations,
            eOutput,
            eNumStandardModels
        };

        /// <summary>
        /// Unbind all tied JSBSim properties.
        /// </summary>
        /// <param name=""></param>
        //TODO public void Unbind() { instance.Unbind(); }


        public static string GetVersion()
        {
            return JSBSimVersion.ToString();
        }

        /// <summary>
        /// This routine places a model into the runlist at the specified rate. The
        ///	"rate" is not really a clock rate. It represents how many calls to the
        ///	Run() method must be made before the model is executed. A
        ///	value of 1 means that the model will be executed for each call to the
        ///	exec's Run() method. A value of 5 means that the model will only be
        ///	executed every 5th call to the exec's Run() method.
        /// </summary>
        /// <param name="model">The model being scheduled</param>
        /// <param name="rate">The rate at which to execute the model as described above</param>
#if DELETEME
        public void Schedule(Model model, int rate)
        {
            model.Rate = rate;
            models.Add(model);
        }
#endif

        ///<summary>
        /// Pauses execution by preventing time from incrementing.
        ///</summary>
        public void Hold() { holding = true; }

        internal bool IntegrationSuspended()
        {
            throw new NotImplementedException();
        }

        ///<summary>
        /// Resumes execution from a "Hold".
        ///</summary>
        void Resume() { holding = false; }

        ///<summary>
        /// Returns true if the simulation is Holding (i.e. simulation time is not moving).
        ///</summary>
        public bool Holding() { return holding; }

        /// <summary>
        /// This executes each scheduled model in succession.
        /// </summary>
        /// <returns>true if successful, false if sim should be ended</returns>
        public bool Run()
        {
            if (models.Length == 0) return false;

            for (int i = 1; i < SlaveFDMList.Count; i++)
            {
                //    SlaveFDMList[i]->exec->State->Initialize(); // Transfer state to the slave FDM
                //    SlaveFDMList[i]->exec->Run();
            }


            System.Collections.IEnumerator modelsEnumerator = models.GetEnumerator();

            while (modelsEnumerator.MoveNext())
            {
                ((Model)modelsEnumerator.Current).Run(false);

            }
            frame++;

            if (!Holding()) state.IncrTime();

            return true;
        }

        /// <summary>
        /// Initializes the sim from the initial condition object and executes
        /// each scheduled model without integrating i.e. dt=0.
        /// </summary>
        /// <returns>true if successful</returns>
        public bool RunIC()
        {

            state.SuspendIntegration();
            state.Initialize(ic);
            Run();
            state.ResumeIntegration();
            return true;
        }

        public GroundCallback GroundCallback
        {
            get { return groundCallback; }
            set { groundCallback = value; }
        }

        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

        public List<string> EnumerateFDMs()
        {
            List<string> FDMList = new List<string>();

            FDMList.Add(Aircraft.AircraftName);

            for (int i = 1; i < SlaveFDMList.Count; i++)
            {
                FDMList.Add(SlaveFDMList[i].exec.Aircraft.AircraftName);
            }

            return FDMList;
        }

        /// <summary>
        /// Loads an aircraft model.
        /// </summary>
        /// <param name="aircraftPath">path to the aircraft directory. For instance:
        /// "aircraft". Under aircraft, then, would be directories for various
        /// modeled aircraft such as C172/, x15/, etc.</param>
        /// <param name="enginePath">path to the directory under which engine config
        /// files are kept, for instance "engine"</param>
        /// <param name="enginePath">path to the directory under which systems config
        /// files are kept, for instance "systems"</param>
        /// <param name="model">the name of the aircraft model itself. This file 
        /// will be looked for in the directory specified in the AircraftPath 
        /// variable, and in turn under the directory with the same name as the
        /// model. For instance: "aircraft/x15/x15.xml"</param>
        /// <param name="addModelToPath">set to true to add the model name to the 
        /// aircraftPath, defaults to true</param>
        /// <returns>true if successful</returns>
        public void LoadModel(string aircraftPath, string enginePath, string SystemsPath,
            string model, bool addModelToPath = true)
        {
            AircraftPath = aircraftPath;
            EnginePath = enginePath;

            LoadModel(model, addModelToPath);
        }

        public void LoadModel(string aircraftPath, string enginePath, string model)
        {
            LoadModel(aircraftPath, enginePath, null, model, true);
        }

        /// <summary>
        /// Loads an aircraft model.
        /// </summary>
        /// <param name="model">the name of the aircraft model itself. This file 
        /// will be looked for in the directory specified in the AircraftPath 
        /// variable, and in turn under the directory with the same name as the
        /// model. For instance: "aircraft/x15/x15.xml"</param>
        /// <param name="addModelToPath">set to true to add the model name to the 
        /// AircraftPath, defaults to true</param>
        /// <returns>true if successful</returns>
        public void LoadModel(string model, bool addModelToPath)
        {
            string aircraftCfgFileName = aircraftPath;

            this.modelName = model;

            if (addModelToPath)
                aircraftCfgFileName += "/" + model;

            aircraftCfgFileName += "/" + model + ".xml";

            try
            {
                // XmlTextReader reader = new XmlTextReader(aircraftCfgFileName);
                XmlReaderSettings settings = new XmlReaderSettings();

                settings.ValidationType = ValidationType.Schema;

                XmlReader xmlReader = XmlReader.Create(new XmlTextReader(aircraftCfgFileName), settings);

                Stream schema = Assembly.GetExecutingAssembly().GetManifestResourceStream("JSBSim.JSBSim.xsd");
                if (schema != null)
                    settings.Schemas.Add(null, new XmlTextReader(schema));

                // Load the XmlTextReader from the stream
                LoadModel(xmlReader);

                xmlReader.Close();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error("Exception reading aircraft file: " + e);
                }
            }

        }

        public void LoadModel(string model)
        {
            LoadModel(model, true);
        }


        public void LoadModel(XmlReader reader)
        {
            XmlDocument doc = new XmlDocument();
            // load the data into the dom
            doc.Load(reader);
            XmlNodeList childNodes = doc.GetElementsByTagName("fdm_config");
            ReadAircraft(childNodes[0] as XmlElement);
        }
        /*  Loads a script
      @param Script The full path name and file name for the script to be loaded.
      @param deltaT The simulation integration step size, if given.  If no value is supplied
                    then 0.0 is used and the value is expected to be supplied in
                    the script file itself.
      @param initfile The initialization file that will override the initialization file
                      specified in the script file. If no file name is given on the command line,
                      the file specified in the script will be used. If an initialization file 
                      is not given in either place, an error will result.
      @return true if successfully loads; false otherwise. */
        public bool LoadScript(string Script, double deltaT = 0.0,
                  string initfile = "")
        { throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM"); }


        /* Sets the path to the engine config file directories.
            @param path path to the directory under which engine config
            files are kept, for instance "engine"  */
        public bool SetEnginePath(string path)
        {
            EnginePath = GetFullPath(path);
            return true;
        }
        /*  Sets the path to the aircraft config file directories.
          @param path path to the aircraft directory. For instance:
          "aircraft". Under aircraft, then, would be directories for various
          modeled aircraft such as C172/, x15/, etc.  */
        public bool SetAircraftPath(string path)
        {
            AircraftPath = GetFullPath(path);
            return true;
        }

        /*  Sets the path to the systems config file directories.
            @param path path to the directory under which systems config
            files are kept, for instance "systems"  */
        public bool SetSystemsPath(string path)
        {
            SystemsPath = GetFullPath(path);
            return true;
        }
        /// Retrieves the engine path.
        public string GetEnginePath() { return EnginePath; }
        /// Retrieves the aircraft path.
        public string GetAircraftPath() { return AircraftPath; }
        /// Retrieves the systems path.
        public string GetSystemsPath() { return SystemsPath; }
        /// Retrieves the full aircraft path name.
        public string GetFullAircraftPath() { return FullAircraftPath; }

        private void ReadAircraft(XmlElement element)
        {
            ReadPrologue(element);

            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;
                    if (currentElement.LocalName.Equals("fileheader"))
                    {
                        ReadFileHeader(currentElement);
                    }
                    else if (currentElement.LocalName.Equals("slave"))
                    {
                        ReadSlave(currentElement);
                    }
                    else if (currentElement.LocalName.Equals("metrics"))
                    {
                        this.aircraft.Load(currentElement);
                    }
                    else if (currentElement.LocalName.Equals("mass_balance"))
                    {
                        this.massBalance.Load(currentElement);
                    }
                    else if (currentElement.LocalName.Equals("aerodynamics"))
                    {
                        this.aerodynamics.Load(currentElement);
                    }
                    else if (currentElement.LocalName.Equals("ground_reactions"))
                    {
                        this.groundReactions.Load(currentElement);
                    }
                    else if (currentElement.LocalName.Equals("propulsion"))
                    {
                        this.propulsion.Load(currentElement);
                    }
                    else if ((currentElement.LocalName.Equals("flight_control")) ||
                       (currentElement.LocalName.Equals("autopilot")))
                    {
                        this.FCS.Load(currentElement);
                    }
                    else if (currentElement.LocalName.Equals("input"))
                    {
                        input.Load(currentElement);
                    }
                    else if (currentElement.LocalName.Equals("output"))
                    {
                        Output output = new Output(this);
                        output.InitModel();
                        //Schedule(output, 1);
                        output.Load(currentElement);
                        outputs.Add(output);
                    }
                    else
                        if (log.IsWarnEnabled)
                        log.Warn("Found unexpected subsystem:  <" + currentElement.LocalName + ">.");
                }
            }
        }



        /// Top-level executive State and Model retrieval mechanism

        /// <summary>
        /// Returns the State
        /// </summary>
        /// <returns></returns>
        public State State { get { return state; } }

        /// <summary>
        /// Returns the Atmosphere model
        /// </summary>
        /// <returns></returns>
        public Atmosphere Atmosphere { get { return (Atmosphere)models[(int)eModels.eAtmosphere]; } }

        /// <summary>
        /// Returns the Aircraft reference.
        /// </summary>
        public MassBalance MassBalance { get { return (MassBalance)models[(int)eModels.eMassBalance]; } }

        /// <summary>
        /// Returns the Aircraft reference.
        /// </summary>
        public Aircraft Aircraft { get { return (Aircraft)models[(int)eModels.eAircraft]; } }

        /// <summary>
        /// Returns the Propulsion reference.
        /// </summary>
        public Propulsion Propulsion { get { return (Propulsion)models[(int)eModels.ePropulsion]; } }

        /// <summary>
        /// Returns the Aerodynamics reference
        /// </summary>
        public Aerodynamics Aerodynamics { get { return (Aerodynamics)models[(int)eModels.eAerodynamics]; } }

        /// <summary>
        /// Returns the FlightControlSystem model.
        /// </summary>
        /// <returns></returns>
        public FlightControlSystem FlightControlSystem { get { return (FlightControlSystem)models[(int)eModels.eSystems]; ; } }

        /// <summary>
        /// Returns the GroundReactions reference.
        /// </summary>
        public GroundReactions GroundReactions { get { return (GroundReactions)models[(int)eModels.eGroundReactions]; } }

        /// <summary>
        /// Returns the Input reference.
        /// </summary>
        public Input Input { get { return (Input)models[(int)eModels.eInput]; ; } }

        /// <summary>
        /// Returns the Inertial reference.
        /// </summary>
        public Inertial Inertial { get { return (Inertial)models[(int)eModels.eInertial]; } }

        /// <summary>
        /// Returns the Output reference.
        /// </summary>
        //public Output Output { get { return output; } }

        /// <summary>
        /// Returns a reference to the InitialCondition object
        /// </summary>
        public InitialCondition GetIC { get { return ic; } }

        /// <summary>
        /// Returns the Auxiliary reference.
        /// </summary>
        public Auxiliary Auxiliary { get { return (Auxiliary)models[(int)eModels.eAuxiliary]; } }

        /// <summary>
        /// Returns the Propagate reference.
        /// </summary>
        public Propagate Propagate { get { return (Propagate)models[(int)eModels.ePropagate]; } }

        public void DisableOutput()
        {
            for (int i = 0; i < outputs.Count; i++)
            {
                outputs[i].Disable();
            }
        }

        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

        public void EnableOutput()
        {
            for (int i = 0; i < outputs.Count; i++)
            {
                outputs[i].Enable();
            }
        }

        /// <summary>
        /// Returns a reference to the Trim object
        /// </summary>
        /// <returns></returns>
        public Trim GetTrim()
        {
            this.Trim = new Trim(this, TrimMode.None);
            return Trim; // TODO review ??
        }

        public void DoTrim(TrimMode mode)
        {
            DoTrim((int)mode);
        }

        public void DoTrim(int mode)
        {
            double saved_time;

            if (constructing) return;

            saved_time = State.SimTime;
            Trim trim = new Trim(this, (TrimMode)mode);
            if (!trim.DoTrim())
                log.Error("Trim Failed");
            trim.Report();
            State.SimTime = saved_time;
        }

        /// <summary>
        /// Gets/Sets the engine path to the directory under which engine 
        /// config files are kept, for instance "engine"
        /// </summary>
        public string EnginePath
        {
            get { return enginePath; }
            set { enginePath = value; }
        }

        /// <summary>
        /// Gets/sets the aircraft path. For instance: 
        /// "aircraft". Under aircraft, then, would be directories for various	
        /// modeled aircraft such as C172/, x15/, etc.path.
        /// </summary>
        public string AircraftPath
        {
            get { return aircraftPath; }
            set { aircraftPath = value; }
        }

        /// <summary>
        /// Gets/sets path to the directory under which systems config
        /// files are kept, for instance "systems"
        /// </summary>
        public string SystemsPath
        {
            get { return systemsPath; }
            set { systemsPath = value; }
        }

        /// <summary>
        /// Gets/sets the full aircraft path name.
        /// </summary>
        public string FullAircraftPath
        {
            get { return fullAircraftPath; }
            set { fullAircraftPath = value; }
        }

        //  /// Retrieves the control path.
        //  inline string GetControlPath(void)        {return ControlPath;}

        public string ModelName { get { return modelName; } }

        public PropertyManager PropertyManager { get { return propManager; } }

        public void SetSlave() { IsSlave = true; }

        public struct PropertyCatalogStructure
        {
            /// Name of the property.
            public string base_string;
            /// The node for the property.
            public PropertyNode node;
        }

        /** Builds a catalog of properties.
        *   This function descends the property tree and creates a list (an STL vector)
        *   containing the name and node for all properties.
        *   @param pcs The "root" property catalog structure pointer.  */
        public void BuildPropertyCatalog(PropertyCatalogStructure pcs) { }

        /** Retrieves property or properties matching the supplied string.
        *   A string is returned that contains a carriage return delimited list of all
        *   strings in the property catalog that matches the supplied check string.
        *   @param check The string to search for in the property catalog.
        *   @return the carriage-return-delimited string containing all matching strings
        *               in the catalog.  */
        public string QueryPropertyCatalog(string check)
        {
            throw new NotImplementedException("QueryPropertyCatalog");
        }

        /// Use the MSIS atmosphere model.
        public void UseAtmosphereMSIS()
        {
            Atmosphere oldAtmosphere = Atmosphere;
            atmosphere = new MSIS(this);
            if (!atmosphere.InitModel())
            {
                if (log.IsErrorEnabled)
                    log.Error("MSIS Atmosphere model init failed");
                error += 1;
            }
        }

        /// Use the Mars atmosphere model. (Not operative yet.)
        public void UseAtmosphereMars()
        {
            throw new NotImplementedException("UseAtmosphereMars");
        }
        public TemplateFunc GetTemplateFunc(string name)
        {
            return templateFunctions.ContainsKey(name) ? templateFunctions[name] : null;
        }
        public void AddTemplateFunc(string name, XmlElement el)
        {
            templateFunctions[name] = new TemplateFunc(this, el);
        }

        private bool Allocate()
        {
            bool result = true;

            models = new Model[(int)eModels.eNumStandardModels];

            // First build the inertial model since some other models are relying on
            // the inertial model and the ground callback to build themselves.
            // Note that this does not affect the order in which the models will be
            // executed later.
            models[(int)eModels.eInertial] = new Inertial(this);

            // See the eModels enum specification in the header file. The order of the
            // enums specifies the order of execution. The Models[] vector is the primary
            // storage array for the list of models.
            models[(int)eModels.ePropagate] = new Propagate(this);
            models[(int)eModels.eInput] = new Input(this);
            models[(int)eModels.eAtmosphere] = new StandardAtmosphere(this);
            //PENDING new JSBSIM models[(int)eModels.eWinds] = new Winds(this);
            models[(int)eModels.eSystems] = new FlightControlSystem(this);
            models[(int)eModels.eMassBalance] = new MassBalance(this);
            models[(int)eModels.eAuxiliary] = new Auxiliary(this);
            models[(int)eModels.ePropulsion] = new Propulsion(this);
            models[(int)eModels.eAerodynamics] = new Aerodynamics(this);
            models[(int)eModels.eGroundReactions] = new GroundReactions(this);
            //PENDING new JSBSIM models[(int)eModels.eExternalReactions] = new ExternalReactions(this);
            //PENDING new JSBSIM models[(int)eModels.eBuoyantForces] = new BuoyantForces(this);
            models[(int)eModels.eAircraft] = new Aircraft(this);
            //PENDING new JSBSIM models[(int)eModels.eAccelerations] = new Accelerations(this);
            models[(int)eModels.eOutput] = new Output(this);

            // Assign the Model shortcuts for internal executive use only.
            propagate = (Propagate)models[(int)eModels.ePropagate];
            inertial = (Inertial)models[(int)eModels.eInertial];
            atmosphere = (Atmosphere)models[(int)eModels.eAtmosphere];
            //PENDING new JSBSIM winds = (Winds)models[(int)eModels.eWinds];
            FCS = (FlightControlSystem)models[(int)eModels.eSystems];
            massBalance = (MassBalance)models[(int)eModels.eMassBalance];
            auxiliary = (Auxiliary)models[(int)eModels.eAuxiliary];
            propulsion = (Propulsion)models[(int)eModels.ePropulsion];
            aerodynamics = (Aerodynamics)models[(int)eModels.eAerodynamics];
            groundReactions = (GroundReactions)models[(int)eModels.eGroundReactions];
            //PENDING new JSBSIM  externalReactions = (ExternalReactions)models[(int)eModels.eExternalReactions];
            //PENDING new JSBSIM buoyantForces = (BuoyantForces)models[(int)eModels.eBuoyantForces];
            aircraft = (Aircraft)models[(int)eModels.eAircraft];
            //PENDING new JSBSIM accelerations = (Accelerations)models[(int)eModels.eAccelerations];
            //PENDING new JSBSIM output = (Output)models[(int)eModels.eOutput];

            // Initialize planet (environment) constants
            LoadPlanetConstants();

            // Initialize models
            for (int i = 0; i < models.Length; i++)
            {
                // The Input/Output models must not be initialized prior to IC loading
                if (i == (int)eModels.eInput || i == (int)eModels.eOutput) continue;

                //PENDING new JSBSIM LoadInputs(i);
                if (models[i] != null)
                    models[i].InitModel();
            }

            ic = new InitialCondition(this);
            ic.Bind(instance);

            modelLoaded = false;

            return result;

            //---------------------- old code pending 
#if DELETEME
            bool result = true;
            massBalance = new MassBalance(this);
            //output = new Output(this);
            input = new Input(this);
            //TODO groundCallback = new DefaultGroundCallback();
            state = new State(this); // This must be done here, as the State
            // class needs valid pointers to the above model classes


            // Initialize models so they can communicate with each other

            if (!atmosphere.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("Atmosphere model init failed");
                error += 1;
            }
            if (!FCS.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("FCS model init failed");
                error += 2;
            }
            if (!propulsion.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("Propulsion model init failed");
                error += 4;
            }
            if (!massBalance.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("MassBalance model init failed");
                error += 8;
            }
            if (!aerodynamics.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("Aerodynamics model init failed");
                error += 16;
            }
            if (!inertial.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("Inertial model init failed");
                error += 32;
            }
            if (!groundReactions.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("Ground Reactions model init failed");
                error += 64;
            }
            if (!aircraft.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("Aircraft model init failed");
                error += 128;
            }
            if (!propagate.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("Propagate model init failed");
                error += 256;
            }
            if (!auxiliary.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("Auxiliary model init failed");
                error += 512;
            }
            if (!input.InitModel())
            {
                if (log.IsErrorEnabled) log.Error("Intput model init failed");
                error += 1024;
            }

            if (error > 0) result = false;

            ic = new InitialCondition(this);

            // Schedule a model. The second arg (the integer) is the pass number. For
            // instance, the atmosphere model gets executed every fifth pass it is called
            // by the executive. Everything else here gets executed each pass.
            // IC and Trim objects are NOT scheduled.

            Schedule(input, 1);
            Schedule(atmosphere, 1);
            Schedule(FCS, 1);
            Schedule(propulsion, 1);
            Schedule(massBalance, 1);
            Schedule(aerodynamics, 1);
            Schedule(inertial, 1);
            Schedule(groundReactions, 1);
            Schedule(aircraft, 1);
            Schedule(propagate, 1);
            Schedule(auxiliary, 1);
            //Schedule(output, 1);

            modelLoaded = false;
            return result;
#endif
        }

        private bool DeAllocate()
        {
            // TODO use dispose??
            /*
            delete atmosphere;
            delete FCS;
            delete propulsion;
            delete massBalance;
            delete aerodynamics;
            delete inertial;
            delete GroundReactions;
            delete Aircraft;
            delete Propagate;
            delete Auxiliary;
            delete Output;
            delete State;

            delete IC;
            delete Trim;
            */

            error = 0;

            state = null;
            atmosphere = null;
            massBalance = null;
            inertial = null;
            propulsion = null;
            aerodynamics = null;
            FCS = null;
            groundReactions = null;
            aircraft = null;
            propagate = null;
            auxiliary = null;
            input = null;
            modelLoaded = false;
            return modelLoaded;
        }

        private void LoadPlanetConstants()
        {
            propagate.inputs.vOmegaPlanet = Inertial.GetOmegaPlanet();
            //accelerations.inputs.vOmegaPlanet = Inertial.GetOmegaPlanet();
            propagate.inputs.SemiMajor = Inertial.GetSemimajor();
            propagate.inputs.SemiMinor = Inertial.GetSemiminor();
            //auxiliary.inputs.StandardGravity = Inertial.GetStandardGravity();
        }
        //TODO private Model FirstModel = null;
        private Model[] models;

        //TODO delete?? private bool terminate = false;
        private bool holding = false;
        private bool constructing = false;
        private int error = 0;
        private int frame = 0;
        private int IdFDM = 0;
        private static int FDMctr = 0;
        private bool modelLoaded = false;
        private string modelName;
        private bool IsSlave = false;
        //private static PropertyManager master;
        private PropertyManager propManager;

        private struct slaveData
        {
            public FDMExecutive exec;
            public string info;
            public double x, y, z;
            public double roll, pitch, yaw;
            public bool mated;
        }

        private List<slaveData> SlaveFDMList = new List<slaveData>();
        private string aircraftPath = ".";
        private string enginePath = ".";
        private string systemsPath = ".";
        private string fullAircraftPath = ".";
        //  string ControlPath;
        private string rootDir = "";

        private string CFGVersion;
        private string release;

        // Standard Model references - shortcuts for internal executive use only.
        private Propagate propagate = null;
        private GroundCallback groundCallback = null;
        private State state = null;
        private Auxiliary auxiliary = null;
        private Atmosphere atmosphere = null;
        private MassBalance massBalance = null;
        private Aircraft aircraft = null;
        private Inertial inertial = null;
        private Propulsion propulsion = null;
        private Aerodynamics aerodynamics = null;
        private FlightControlSystem FCS = null;
        private GroundReactions groundReactions = null;
        private Trim Trim = null;
        //private Output output = null;
        private List<Output> outputs = new List<Output>();
        private Input input = null;

        private bool trim_status;
        private int ta_mode;
        private int ResetMode;
        private int trim_completed;

        //private Script Script;
        private InitialCondition ic = null;
        private Trim trim;

        private PropertyManager Root;
        private bool StandAlone;
        private PropertyManager instance;


        private Dictionary<string, TemplateFunc> templateFunctions = new Dictionary<string, TemplateFunc>();

        private const string CONFIG_FDM_NAME2 = "name";
        private const string CONFIG_FDM_VERSION2 = "version";
        private const string CONFIG_FDM_RELEASE2 = "release";

        private void ReadFileHeader(System.Xml.XmlElement element)
        {
            foreach (XmlNode currentNode in element.ChildNodes)
            {
                if (currentNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement currentElement = (XmlElement)currentNode;

                    if (currentElement.LocalName.Equals("author"))
                    {
                        if (log.IsDebugEnabled) log.Debug("  Model Author:  " + currentElement.InnerText);
                    }
                    else if (currentElement.LocalName.Equals(("filecreationdate")))
                    {
                        if (log.IsDebugEnabled) log.Debug("  Creation Date: " + currentElement.InnerText);
                    }
                    else if (currentElement.LocalName.Equals(("version")))
                    {
                        if (log.IsDebugEnabled) log.Debug("  Version:       " + currentElement.InnerText);
                    }
                    else if (currentElement.LocalName.Equals(("description")))
                    {
                        if (log.IsDebugEnabled) log.Debug("  Description:   " + currentElement.InnerText);
                    }
                }
            }
        }

        private void ReadPrologue(System.Xml.XmlElement element)
        {
            // Look for the config attributes
            this.aircraft.AircraftName = element.GetAttribute(CONFIG_FDM_NAME2);
            this.CFGVersion = element.GetAttribute(CONFIG_FDM_VERSION2);
            this.release = element.GetAttribute(CONFIG_FDM_RELEASE2);

            if (log.IsDebugEnabled)
            {
                log.Debug("Reading Aircraft Configuration File: " + this.modelName);
                log.Debug("                            Version: " + this.CFGVersion);
            }

            if (log.IsWarnEnabled && this.CFGVersion != neededCfgVersion)
            {
                log.Warn("YOU HAVE AN INCOMPATIBLE CFG FILE FOR THIS AIRCRAFT." +
                    " RESULTS WILL BE UNPREDICTABLE !!");
                log.Warn("Current version needed is: " + neededCfgVersion);
                log.Warn("         You have version: " + this.CFGVersion);
            }

            if (log.IsWarnEnabled && release.Equals("ALPHA"))
            {
                log.Warn("This aircraft model is an " + release + " release!!!");
                log.Warn("This aircraft model may not even properly load, and probably will not fly as expected.");
                log.Warn("Use this model for development purposes ONLY!!!");
            }
            else if (log.IsWarnEnabled && release.Equals("BETA"))
            {
                log.Warn("This aircraft model is an " + release + " release!!!");
                log.Warn("This aircraft model probably will not fly as expected.");
                log.Warn("Use this model for development purposes ONLY!!!");
            }
            else if (log.IsWarnEnabled && release.Equals("PRODUCTION"))
            {
                log.Warn("This aircraft model is an " + release + " release.");
            }
            else if (log.IsWarnEnabled)
            {
                log.Warn("This aircraft model is an " + release + " release!!!");
                log.Warn("This aircraft model may not even properly load, and probably will not fly as expected.");
                log.Warn("Use this model for development purposes ONLY!!!");
            }

        }

        private void ReadSlave(System.Xml.XmlElement element)
        {
            // Add a new slaveData object to the slave FDM list
            // Populate that slaveData element with a new FDMExec object
            // Set the IsSlave flag for that FDMExec object
            // Get the aircraft name
            // set debug level to print out no additional data for slave objects
            // Load the model given the aircraft name
            // reset debug level to prior setting

            slaveData slave = new slaveData();
            slave.exec = new FDMExecutive();
            slave.exec.SetSlave();
            SlaveFDMList.Add(slave);
            /*
            string token;
              string AircraftName = AC_cfg->GetValue("file");

              debug_lvl = 0;                 // turn off debug output for slave vehicle

              SlaveFDMList.back()->exec->SetAircraftPath( AircraftPath );
              SlaveFDMList.back()->exec->SetEnginePath( EnginePath );
              SlaveFDMList.back()->exec->LoadModel(AircraftName);
              debug_lvl = saved_debug_lvl;   // turn debug output back on for master vehicle

              AC_cfg->GetNextConfigLine();
              while ((token = AC_cfg->GetValue()) != string("/SLAVE")) {
                *AC_cfg >> token;
                if      (token == "xloc")  { *AC_cfg >> SlaveFDMList.back()->x;    }
                else if (token == "yloc")  { *AC_cfg >> SlaveFDMList.back()->y;    }
                else if (token == "zloc")  { *AC_cfg >> SlaveFDMList.back()->z;    }
                else if (token == "pitch") { *AC_cfg >> SlaveFDMList.back()->pitch;}
                else if (token == "yaw")   { *AC_cfg >> SlaveFDMList.back()->yaw;  }
                else if (token == "roll")  { *AC_cfg >> SlaveFDMList.back()->roll;  }
                else cerr << "Unknown identifier: " << token << " in slave vehicle definition" << endl;
              }
            */
            if (log.IsDebugEnabled)
            {
                log.Debug("      X = " + slave.x);
                log.Debug("      Y = " + slave.y);
                log.Debug("      Z = " + slave.z);
                log.Debug("      Pitch = " + slave.pitch);
                log.Debug("      Yaw = " + slave.yaw);
                log.Debug("      Roll = " + slave.roll);
            }
        }
        private string GetFullPath(string name)
        {
            if (!Path.IsPathRooted(name))
                return Path.Combine(rootDir, name);
            else
                return name;
        }
        protected const string neededCfgVersion = "2.0";

        protected static readonly Version JSBSimVersion = Assembly.GetCallingAssembly().GetName().Version;

        internal Random GetRandomEngine()
        {
            throw new NotImplementedException("Pending upgrade to lastest version of JSBSIM");
        }
    }
}
