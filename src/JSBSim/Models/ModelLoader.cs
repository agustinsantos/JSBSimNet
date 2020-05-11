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
    using System.IO;

    public class ModelLoader
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


        public ModelLoader(Model _model)
        {
            model = _model;
        }
        public XmlElement Open(XmlElement el)
        {
            XmlElement document = el;
            string fname = el.GetAttribute("file");

            if (!string.IsNullOrEmpty(fname))
            {
                string path = fname;

                if (!Path.IsPathRooted(path))
                    path = model.FindFullPathName(path);

                if (cachedFiles.ContainsKey(path))
                    document = cachedFiles[path];
                else
                {
                    XmlDocument doc = new XmlDocument();
                    try
                    {
                        doc.Load(path);
                    }
                    catch
                    {
                        log.Error("Could not open file: " + fname);
                        return null;
                    }
                    cachedFiles[path] = document;
                }

                if (document.Name != el.Name)
                {
                    el.AppendChild(document);
                }
            }

            return document;
        }
        public static string CheckPathName(string path, string filename)
        {
            string fullName = Path.Combine(path, filename);

            if (Path.GetExtension(fullName) != "xml")
                fullName += ".xml";

            return File.Exists(fullName) ? fullName : "";
        }

        private Model model;
        private Dictionary<string, XmlElement> cachedFiles = new Dictionary<string, XmlElement>();
    };
}

