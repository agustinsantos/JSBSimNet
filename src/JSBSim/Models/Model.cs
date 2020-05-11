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
    using JSBSim.InputOutput;
    using CommonUtils.IO;
    using System.Xml;


    /// <summary>
    /// Base class for all scheduled JSBSim models
    /// This code is based on FGFDMExec written by Jon S. Berndt
    /// </summary>
    [Serializable]
    public class Model : ModelFunctions
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

        public Model(FDMExecutive fdmex)
        {
            FDMExec = fdmex;

            //in order for Model derived classes to self-Bind (that is, call
            //their Bind function in the constructor, the PropertyManager pointer
            //must be brought up now.
            //TODO ?? propertyManager = FDMExec.PropertyManager;

            exe_ctr = 1;
            rate = 1;

            Bind();
        }

        /// <summary>
        /// Runs the model; called by the Executive.
        /// Can pass in a value indicating if the executive is directing the simulation to Hold.
        /// </summary>
        /// <param name="Holding">if true, the executive has been directed to hold the sim from 
        /// advancing time.Some models may ignore this flag, such as the Input
        /// model, which may need to be active to listen on a socket for the
        /// "Resume" command to be given. The Holding flag is not used in the base
        /// FGModel class.</param>
        /// <returns>false if no error</returns>
        public virtual bool Run(bool Holding)
        {
            if (log.IsDebugEnabled) log.Debug("Entering Run() for model " + Name);

            if (rate == 1) return false; // Fast exit if nothing to do

            if (exe_ctr >= rate) exe_ctr = 0;

            if (exe_ctr++ == 1) return false;
            else return true;
        }

        protected bool InternalRun()
        {
            //if (log.IsInfoEnabled)
            //    log.Info("Entering Run() for model " + name);

            if (exe_ctr == 1)
            {
                if (exe_ctr++ >= rate) exe_ctr = 1;
                return false;
            }
            else
            {
                if (exe_ctr++ >= rate) exe_ctr = 1;
                return true;
            }
        }

        public override bool InitModel()
        {
            exe_ctr = 1;
            return base.InitModel();
        }

        /// <summary>
        /// Get/Set the ouput rate for the model in frames
        /// </summary>
        public virtual int Rate
        {
            get { return rate; }
            set { rate = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public FDMExecutive GetExec() { return FDMExec; }

        public void SetPropertyManager(PropertyManager fgpm) { propertyManager = fgpm; }
        public virtual string FindFullPathName(string path)
        {
            return ModelLoader.CheckPathName(FDMExec.GetFullAircraftPath(), path);
        }
        public virtual void Bind()
        {
            FDMExec.PropertyManager.Bind("", this);
        }

        public virtual void Unbind()
        {
            FDMExec.PropertyManager.Unbind("", this);
        }

        /// <summary>
        /// Loads this model.
        /// </summary>
        /// <param name="el"></param>
        /// <param name="preLoad">true if model functions and local properties must be
        /// preloaded.</param>
        /// <returns>true if model is successfully loaded</returns>
        protected virtual bool Load(XmlElement el, bool preLoad)
        {
            ModelLoader modelLoader = new ModelLoader(this);
            XmlElement document = modelLoader.Open(el);
            ModelFunctions ModelFunctions = new ModelFunctions();

            if (document == null) return false;

            if (document.Name != el.Name)
            {
                log.Error(" Read model '" + document.Name
                        + "' while expecting model '" + el.Name + "'");
                return false;
            }

            bool result = true;

            if (preLoad)
                result = ModelFunctions.Load(document, FDMExec);

            if (document != el)
            {
                el.MergeAttributes(document);

                if (preLoad)
                {
                    // After reading interface properties in a file, read properties in the
                    // local model element. This allows general-purpose models to be defined
                    // in a file, with overrides or initial loaded constants supplied in the
                    // relevant element of the aircraft configuration file.
                    LocalProperties.Load(el, propertyManager, true);
                }

                foreach (var node in document.ChildNodes)
                {
                    XmlElement element = node as XmlElement;
                    if (element != null)
                    {
                        el.AppendChild(element);
                    }
                }
            }

            return result;
        }

        protected int exe_ctr = 1;
        protected int rate = 1;
        protected string name;

        protected FDMExecutive FDMExec;
        protected PropertyManager propertyManager;
    }
}
