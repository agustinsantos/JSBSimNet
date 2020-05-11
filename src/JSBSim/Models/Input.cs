﻿#region Copyright(C)  Licensed under GNU GPL.
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
using System.IO;
using System.Xml;
using System.Net.Sockets;

// Import log4net classes.
using log4net;

using CommonUtils.MathLib;
using JSBSim.Script;
using JSBSim.Format;


namespace JSBSim.Models
{
    public class Input : Model
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

        public Input(FDMExecutive exec)
            : base(exec)
        {
            Name = "FGInput";
            sFirstPass = dFirstPass = true;
            port = 0;
            enabled = true;
        }


        public override bool Run(bool Holding)
        {
            if (InternalRun()) return true;

            /* TODO TODO TODO 
  string line, token, info_string;
  int start=0, string_start=0, string_end=0;
  int token_start=0, token_end=0;
  char buf[100];
  double value=0;
  FGPropertyManager* node=0;

  if (base.Run()) return true; // fast exit if nothing to do
  if (port == 0) return true; // Do nothing here if port not defined

  // This model DOES execute if "Exec->Holding"

  data = socket->Receive(); // get socket transmission if present

  if (data.size() > 0) {
    // parse lines
    while (1) {
      string_start = data.find_first_not_of("\r\n", start);
      if (string_start == string::npos) break;
      string_end = data.find_first_of("\r\n", string_start);
      if (string_end == string::npos) break;
      line = data.substr(string_start, string_end-string_start);
      if (line.size() == 0) break;

      // now parse individual line
      token_start = line.find_first_not_of(" ", 0);
      token_end = line.find_first_of(" ", token_start);
      token = line.substr(token_start, token_end - token_start);

      if (token == "set" || token == "SET" ) {                   // SET PROPERTY

        token_start = line.find_first_not_of(" ", token_end);
        token_end = line.find_first_of(" ", token_start);
        token = line.substr(token_start, token_end-token_start);
        node = PropertyManager->GetNode(token);
        if (node == 0) socket->Reply("Unknown property\n");
        else {
          token_start = line.find_first_not_of(" ", token_end);
          token_end = line.find_first_of(" ", token_start);
          token = line.substr(token_start, token_end-token_start);
          value = atof(token.c_str());
          node->setDoubleValue(value);
        }

      } else if (token == "get" || token == "GET") {             // GET PROPERTY

        token_start = line.find_first_not_of(" ", token_end);
        if (token_start == string::npos) {
          socket->Reply("No property argument supplied.\n");
          break;
        } else {
          token = line.substr(token_start, line.size()-token_start);
        }
        try {
          node = PropertyManager->GetNode(token);
        } catch(...) {
          socket->Reply("Badly formed property query\n");
          break;
        }
        if (node == 0) {
          if (FDMExec->Holding()) { // if holding can query property list
            string query = FDMExec->QueryPropertyCatalog(token);
            socket->Reply(query);
          } else {
            socket->Reply("Must be in HOLD to search properties\n");
          }
        } else if (node > 0) {
          sprintf(buf, "%s = %12.6f\n", token.c_str(), node->getDoubleValue());
          socket->Reply(buf);
        }

      } else if (token == "hold" || token == "HOLD") {                  // PAUSE

        FDMExec->Hold();

      } else if (token == "resume" || token == "RESUME") {             // RESUME

        FDMExec->Resume();

      } else if (token == "quit" || token == "QUIT") {                   // QUIT

        // close the socket connection
        socket->Reply("");
        socket->Close();

      } else if (token == "info" || token == "INFO") {                   // INFO

        // get info about the sim run and/or aircraft, etc.
        sprintf(buf, "%8.3f\0", State->Getsim_time());
        info_string  = "JSBSim version: " + JSBSim_version + "\n";
        info_string += "Config File version: " + needed_cfg_version + "\n";
        info_string += "Aircraft simulated: " + Aircraft->GetAircraftName() + "\n";
        info_string += "Simulation time: " + string(buf) + "\n";
        socket->Reply(info_string);

      } else if (token == "help" || token == "HELP") {                   // HELP

        socket->Reply(
        " JSBSim Server commands:\n\n"
        "   get {property name}\n"
        "   set {property name} {value}\n"
        "   hold\n"
        "   resume\n"
        "   help\n"
        "   quit\n"
        "   info\n\n");

      } else {
        socket->Reply(string("Unknown command: ") +  token + string("\n"));
      }

      start = string_end;
    }
  }
            */
            return false;
        }

        public void SetType(string type)
        {
            throw new System.NotImplementedException("Input.SetType");
        }

        public void Enable() { enabled = true; }
        public void Disable() { enabled = false; }
        public bool Toggle() { enabled = !enabled; return enabled; }


        public void Load(XmlElement element)
        {
            port = uint.Parse(element.GetAttribute("port"), FormatHelper.numberFormatInfo);
            if (port == 0)
            {
                if (log.IsErrorEnabled)
                    log.Error("No port assigned in input element");
            }
            else
            {
                if (log.IsErrorEnabled)
                    log.Error("Input.Load, socket creation not implemented");
                // TODO socket = new Socket(port);
                //throw new System.NotImplementedException("Input.Load, socket creation");
            }
        }
        
        private bool sFirstPass, dFirstPass, enabled;
        private uint port;
        private Socket socket;
        private string data;
    }
}
