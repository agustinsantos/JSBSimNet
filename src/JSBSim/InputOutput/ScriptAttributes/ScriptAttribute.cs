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
namespace JSBSim.Script
{

	using System;

	/// <summary>
	/// 	Summary description for ScriptAttribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple=true)]
	public sealed class ScriptAttribute : Attribute 
	{

		#region Fields

		/// <summary>
		///    Name of the command the target class will be registered to handle.
		/// </summary>
		private string name;
		
		/// <summary>
		///    Description of what this command does.
		/// </summary>
		private string description;

		#endregion Fields

		#region Constructors

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="description"></param>
		/// <param name="target"></param>
		public ScriptAttribute(string name, string description) 
		{
			this.name = name;
			this.description = description;
		}

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="name"></param>
		public ScriptAttribute(string name) 
		{
			this.name = name;
		}

		#endregion Constructors

		#region InputOutput

		/// <summary>
		///    Name of this command.
		/// </summary>
		public string Name 
		{
			get 
			{
				return name;
			}
		}

		/// <summary>
		///    Optional description of what this command does.
		/// </summary>
		public string Description 
		{
			get 
			{
				return description;
			}
		}

		#endregion
	}
}
