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
namespace JSBSim.MathValues
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Represents a real value.
    /// </summary>
    public class RealValue : IParameter
    {
        public RealValue(double vparam)
        {
            val = vparam;
        }

        public double GetValue()
        {
            return val;
        }

        public string GetName()
        {
            return string.Format(CultureInfo.InvariantCulture, "constant value {0}", val);
        }

        public bool IsConstant()
        {
            return true;
        }

        private readonly double val;
    }
}
