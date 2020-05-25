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
    using System.Xml;
    using System.Collections.Generic;
    using JSBSim.MathValues;


    /// <summary>
    /// The model functions class provides the capability for loading, storing, and
    /// executing arbitrary functions.
    /// For certain classes, such as the engine, aerodynamics, ground reactions, 
    /// mass balance, etc., it can be useful to incorporate special functions that
    /// can operate on the local model parameters before and/or after the model
    /// executes.For example, there is no inherent chamber pressure calculation
    /// done in the rocket engine model.However, an arbitrary function can be added
    /// to a specific rocket engine XML configuration file. It would be tagged with
    ///  a "pre" or "post" type attribute to denote whether the function is to be
    /// executed before or after the standard model algorithm.
    /// This code is based on FGFDMExec written by Jon S. Berndt
    /// </summary>
    [Serializable]
    public class ModelFunctions
    {
        public void RunPreFunctions()
        {
            foreach (var prefunc in PreFunctions)
                prefunc.CacheValue(true);
        }
        public void RunPostFunctions()
        {
            foreach (var postfunc in PostFunctions)
                postfunc.CacheValue(true);
        }
        public bool Load(XmlElement el, FDMExecutive fdmex, string prefix = "")
        {
            LocalProperties.Load(el, fdmex.PropertyManager, false);
            PreLoad(el, fdmex, prefix);

            return true; // TODO: Need to make this value mean something.
        }
        public void PreLoad(XmlElement el, FDMExecutive fdmex, string prefix = "")
        {
            // Load model post-functions, if any
            XmlNodeList elemList = el.GetElementsByTagName("function");
            foreach (XmlNode node in elemList)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlElement function = node as XmlElement;
                    string fType = function.GetAttribute("type");
                    if (string.IsNullOrEmpty(fType) || fType == "pre")
                        PreFunctions.Add(new Function(fdmex, function, prefix));
                    else if (fType == "template")
                    {
                        string name = function.GetAttribute("name");
                        fdmex.AddTemplateFunc(name, function);
                    }
                }
            }
        }
        public void PostLoad(XmlElement el, FDMExecutive fdmex, string prefix = "")
        {
            // Load model post-functions, if any
            XmlNodeList elemList = el.GetElementsByTagName("function");
            foreach (XmlNode node in elemList)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlElement function = node as XmlElement;
                    if (function.GetAttribute("type") == "post")
                    {
                        PostFunctions.Add(new Function(fdmex, function, prefix));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the strings for the current set of functions.
        /// </summary>
        /// <param name="delimeter">either a tab or comma string depending on output type</param>
        /// <returns> a string containing the descriptive names for all functions</returns>
        public string GetFunctionStrings(string delimeter)
        {
            string FunctionStrings = "";

            foreach (var prefunc in PreFunctions)
            {
                if (!string.IsNullOrEmpty(FunctionStrings))
                    FunctionStrings += delimeter;

                FunctionStrings += prefunc.GetName();
            }

            foreach (var postfunc in PostFunctions)
            {
                if (!string.IsNullOrEmpty(FunctionStrings))
                    FunctionStrings += delimeter;

                FunctionStrings += postfunc.GetName();
            }

            return FunctionStrings;
        }

        /// <summary>
        /// Gets the function values.
        /// </summary>
        /// <param name="delimeter">delimeter either a tab or comma string depending on output type</param>
        /// <returns>a string containing the numeric values for the current set of
        /// functions</returns>
        public string GetFunctionValues(string delimeter)
        {
            string buf = "";

            foreach (var prefunc in PreFunctions)
            {
                buf += delimeter;
                buf += prefunc.GetValue();
            }

            foreach (var postfunc in PostFunctions)
            {
                buf += delimeter;
                buf += postfunc.GetValue();
            }

            return buf;
        }

        //// <summary>
        ///  Get one of the "pre" function
        /// </summary>
        /// <param name="name">name the name of the requested function.</param>
        /// <returns>a pointer to the function (NULL if not found)</returns>
        public Function GetPreFunction(string name)
        {
            foreach (var prefunc in PreFunctions)
            {
                if (prefunc.GetName() == name)
                    return prefunc;
            }

            return null;
        }
        public virtual bool InitModel()
        {
            LocalProperties.ResetToIC();

            return true;
        }

        protected List<Function> PreFunctions = new List<Function>();
        protected List<Function> PostFunctions = new List<Function>();
        protected PropertyReader LocalProperties = new PropertyReader();


    }
}