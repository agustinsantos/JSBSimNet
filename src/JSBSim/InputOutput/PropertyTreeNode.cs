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
#region Identification
/// $Id:$
#endregion
namespace JSBSim.InputOutput
{
	using System;
	using System.Collections;
	using System.Reflection;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Summary description for IProperty.
	/// </summary>
	public class PropertyTreeNode : PropertyNode
	{
		public delegate void PropertyChildAddedEventHandler(object sender, EventArgs e);
		public delegate void PropertyChildRemovedEventHandler(object sender, EventArgs e);


		public PropertyTreeNode(string nodeName,  object val, PropertyInfo prop) : 
			base (nodeName, val, prop)
		{
		}

		/// <summary>
		/// Get the node's root .
		/// </summary>
		public PropertyTreeNode RootNode 
		{
			get 
			{ 
				if (parent == null)
					return this;
				else
					return parent.RootNode;
			}
		}


		/// <summary>
		/// Event to notify that a child has been added.
		/// </summary>
		/// <value>
		/// Event to notify that a child has been added.
		/// </value>
		public event PropertyChildAddedEventHandler ChildAddedEvent
		{
			add { childAddedEvent += value; }
			remove { childAddedEvent -= value; }
		}

		/// <summary>
		/// Event to notify that a child has been removed.
		/// </summary>
		/// <value>
		/// Event to notify that a child has been removed.
		/// </value>
		public event PropertyChildRemovedEventHandler ChildRemoveEvent
		{
			add { childRemovedEvent += value; }
			remove { childRemovedEvent -= value; }
		}



		/// <summary>
		/// Fire a child-added event to all listeners. 
		/// </summary>
		protected void FireChildAddedEvent()
		{
			if (childAddedEvent != null)
			{
				childAddedEvent(this, EventArgs.Empty);
			}
		}
		
		/// <summary>
		/// Fire a child-removed event to all listeners. 
		/// </summary>
		protected void FireChildRemovedEvent()
		{
			if (childRemovedEvent != null)
			{
				childRemovedEvent(this, EventArgs.Empty);
			}
		}


		protected PropertyTreeNode parent;
		protected ArrayList children = new ArrayList();

		
		/// <summary>
		/// A component in a path.
		/// </summary>
		protected struct PathComponent
		{
			public string name;
			public int index;
		}


		private const string compStr = @"^(?<dir>(/?\w+(\[\d*\])?/)*)(?<fullcomp>(?<component>\w*)(\[(?<index>\d*)\])?)$";
		/// <summary>
		/// A directory: 
		/// A component: [_a-zA-Z][-._a-zA-Z0-9]*
		/// An optional integer index for a component: "[" [0-9]+ "]"
		/// </summary>
		private static Regex compRegEx =
			new Regex(compStr, RegexOptions.IgnoreCase|RegexOptions.Compiled);		

		/// <summary>
		/// Parse a path into its components.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		static ArrayList ParsePath (string path) // return a vector<PathComponent>
		{
			ArrayList components = new ArrayList();
			return ParsePath(path, components);
		}
		
		static ArrayList ParsePath (string path, ArrayList components)
		{

			Match m = compRegEx.Match(path);

			// Check for initial '/'
			/*
			if (path[pos] == '/') 
			{
				PathComponent root = new PathComponent();
				root.name = "";
				root.index = -1;
				components.Add(root;
			}
			*/

			ParsePath(m.Groups["dir"].Value, components); 

			PathComponent comp = new PathComponent();
			comp.name = m.Groups["component"].Value;
			if (m.Groups["index"] != null)
				comp.index = int.Parse(m.Groups["index"].Value);
			else 
				comp.index = -1;
			components.Add(comp);
			return components;
		}

		private event PropertyChildAddedEventHandler childAddedEvent;
		private event PropertyChildRemovedEventHandler childRemovedEvent;

	}


}
