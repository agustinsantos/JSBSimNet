#region Copyright(C)  Licensed under GNU GPL.
/// Copyright (C) 2005-2006 Agustin Santos Mendez
/// 
/// JSBSim was developed by Jon S. Berndt, Tony Peden, and
/// David Megginson. 
/// Agustin Santos Mendez implemented and maintains this C# version.
/// 
/// This program is free software; you can redistribute it and/or
///  modify it under the terms of the GNU General Public License
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
namespace CommonUtils.Tests
{
	using System;

	using NUnit.Framework;

	using CommonUtils.MathLib;

	/// <summary>
	/// Summary description for Matrix3DTests.
	/// </summary>
	[TestFixture]
	public class Matrix3DTests
	{
		[Test]
		public void Determinant()
		{
			Matrix3D m = new Matrix3D(0,0,1,
				0,1,0,
				1,0,0);

			Assert.AreEqual(-1, m.Determinant());

			m = new Matrix3D(
				5,	7,	1,
				17,	2,	64,
				10,	14,	2);

			Assert.AreEqual(0, m.Determinant());

			m = new Matrix3D(
				1,	2,	3,
				4,	5,	6,
				7,	8,	9);

			Assert.AreEqual(0, m.Determinant());
		}
		[Test]
		public void Transpose()
		{
			Matrix3D m = new Matrix3D(
				1,	2,	3,	
				4,	5,	6,	
				7,	8,	9);

			Matrix3D result = new Matrix3D(
				1,	4,	7,
				2,	5,	8,
				3,	6,	9);

			Assert.AreEqual(result, m.GetTranspose());
		}

		[Test]
		public void MatrixMultiply()
		{
			Matrix3D m1 = new Matrix3D(
				1,	2,	3,
				4,	5,	6,
				7,	8,	9);

			Matrix3D m2 = new Matrix3D(
				5,	7,	1,
				17,	2,	64,
				10,	14,	2);

			Matrix3D r = new Matrix3D(
				69,		53,		135,
				165,	122,	336,
				261,	191,	537);

			Assert.AreEqual(m1, m1 * Matrix3D.Identity);
			Assert.AreEqual(r, m1*m2);
		}
	}
}
