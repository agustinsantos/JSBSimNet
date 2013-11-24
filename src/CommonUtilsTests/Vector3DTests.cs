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
	/// Summary description for Vector3DTests.
	/// </summary>
	[TestFixture]
	public class Vector3DTests
	{
		[Test]
		public void Construction()
		{
			Vector3D v;

			// Zero vector
			v = Vector3D.Zero;
			Assert.AreEqual(0, v.X, "Zero Vector - X");
			Assert.AreEqual(0, v.Y, "Zero Vector - Y");
			Assert.AreEqual(0, v.Z, "Zero Vector - Z");

			// X-Axis vector
			v = Vector3D.XAxis;
			Assert.AreEqual(1, v.X, "XAxis Vector - X");
			Assert.AreEqual(0, v.Y, "XAxis Vector - Y");
			Assert.AreEqual(0, v.Z, "XAxis Vector - Z");

			// Y-Axis vector
			v = Vector3D.YAxis;
			Assert.AreEqual(0, v.X, "YAxis Vector - X");
			Assert.AreEqual(1, v.Y, "YAxis Vector - Y");
			Assert.AreEqual(0, v.Z, "YAxis Vector - Z");

			// Z-Axis vector
			v = Vector3D.ZAxis;
			Assert.AreEqual(0, v.X, "ZAxis Vector - X");
			Assert.AreEqual(0, v.Y, "ZAxis Vector - Y");
			Assert.AreEqual(1, v.Z, "ZAxis Vector - Z");

			// Constructors

			v = new Vector3D(1.2, 0.3, 0.5);
			Assert.AreEqual(1.2, v.X);
			Assert.AreEqual(0.3, v.Y);
			Assert.AreEqual(0.5, v.Z);

			double [] fa = { 0.1, 0.2, 0.3 };
			v = new Vector3D(fa);
			Assert.AreEqual(0.1, v.X);
			Assert.AreEqual(0.2, v.Y);
			Assert.AreEqual(0.3, v.Z);

			v = new Vector3D(new Vector3D(0.1, 0.2, 0.3));
			Assert.AreEqual(0.1, v.X);
			Assert.AreEqual(0.2, v.Y);
			Assert.AreEqual(0.3, v.Z);
		}

		[Test]
		public void ScalarOperators()
		{
			Vector3D v = new Vector3D(0.1, 0.2, 0.3);

			v *= 2.0f;
			Assert.AreEqual(0.2, v.X);
			Assert.AreEqual(0.4, v.Y);
			Assert.AreEqual(0.6, v.Z);

			v /= 2.0f;
			Assert.AreEqual(0.1, v.X);
			Assert.AreEqual(0.2, v.Y);
			Assert.AreEqual(0.3, v.Z);
		}

		[Test]
		public void VectorOperators()
		{
			Vector3D v,u;

			v = new Vector3D(1,2,3) + new Vector3D(3,2,1);
			Assert.AreEqual(new Vector3D(4,4,4), v);

			v = new Vector3D(1,2,3) - new Vector3D(3,2,1);
			Assert.AreEqual(new Vector3D(-2,0,2), v);

			u = new Vector3D(1,2,3);
			v = new Vector3D(4,5,6);

			Assert.AreEqual(32, Vector3D.Dot(u,v));
			Assert.AreEqual(new Vector3D(-3,6,-3), Vector3D.Cross(u,v));
			//Assert.AreEqual(true, MathUtils.ApproxEquals(u.GetUnit().GetMagnitude(),1.0f));

			v.Normalize();
			//Assert.AreEqual(true, MathUtils.ApproxEquals(v.GetMagnitude(), 1.0f));
		}
		[Test]
		public void RelationalOperators()
		{
			Assert.AreEqual(true,new Vector3D(2,3,-1) < new Vector3D(3,10,0));
			Assert.AreEqual(true,new Vector3D(2,3,-1) <= new Vector3D(3,10,0));
			Assert.AreEqual(true,new Vector3D(2,3,-1) != new Vector3D(3,10,0));
			Assert.AreEqual(false,new Vector3D(2,3,-1) == new Vector3D(3,10,0));
			Assert.AreEqual(false,new Vector3D(2,3,-1) >= new Vector3D(3,10,0));
			Assert.AreEqual(false,new Vector3D(2,3,-1) > new Vector3D(3,10,0));

			Assert.AreEqual(false,new Vector3D(2,3,-1) < new Vector3D(1,0,-5));
			Assert.AreEqual(false,new Vector3D(2,3,-1) <= new Vector3D(1,0,-5));
			Assert.AreEqual(true,new Vector3D(2,3,-1) != new Vector3D(1,0,-5));
			Assert.AreEqual(false,new Vector3D(2,3,-1) == new Vector3D(1,0,-5));
			Assert.AreEqual(true,new Vector3D(2,3,-1) >= new Vector3D(1,0,-5));
			Assert.AreEqual(true,new Vector3D(2,3,-1) > new Vector3D(1,0,-5));

			Assert.AreEqual(false,new Vector3D(3,4,5) < new Vector3D(3,4,5));
			Assert.AreEqual(true,new Vector3D(3,4,5) <= new Vector3D(3,4,5));
			Assert.AreEqual(false,new Vector3D(3,4,5) != new Vector3D(3,4,5));
			Assert.AreEqual(true,new Vector3D(3,4,5) == new Vector3D(3,4,5));
			Assert.AreEqual(true,new Vector3D(3,4,5) >= new Vector3D(3,4,5));
			Assert.AreEqual(false,new Vector3D(3,4,5) > new Vector3D(3,4,5));

			Assert.AreEqual(false,new Vector3D(1,2,3) < new Vector3D(0,1,3));
			Assert.AreEqual(false,new Vector3D(1,2,3) <= new Vector3D(0,1,3));
			Assert.AreEqual(true,new Vector3D(1,2,3) != new Vector3D(0,1,3));
			Assert.AreEqual(false,new Vector3D(1,2,3) == new Vector3D(0,1,3));
			Assert.AreEqual(true,new Vector3D(1,2,3) >= new Vector3D(0,1,3));
			Assert.AreEqual(false,new Vector3D(1,2,3) > new Vector3D(0,1,3));

			Assert.AreEqual(false,new Vector3D(1,2,3) < new Vector3D(0,1,4));
			Assert.AreEqual(false,new Vector3D(1,2,3) <= new Vector3D(0,1,4));
			Assert.AreEqual(true,new Vector3D(1,2,3) != new Vector3D(0,1,4));
			Assert.AreEqual(false,new Vector3D(1,2,3) == new Vector3D(0,1,4));
			Assert.AreEqual(false,new Vector3D(1,2,3) >= new Vector3D(0,1,4));
			Assert.AreEqual(false,new Vector3D(1,2,3) > new Vector3D(0,1,4));
		}

		[Test]
		public void ToStringTest()
		{
			Vector3D v = new Vector3D(0.1, 0.2, 0.3);
            Assert.AreEqual(string.Format("{0}, {1}, {2}", 0.1, 0.2, 0.3), v.ToString());

            Assert.AreEqual("0.1, 0.2, 0.3", v.ToString("G", UsFormat));
            Assert.AreEqual("0,1, 0,2, 0,3", v.ToString("G", EsFormat));
            Assert.AreEqual("0,1, 0,2, 0,3", v.ToString("G", DeFormat));
        }

        static readonly IFormatProvider UsFormat = new System.Globalization.CultureInfo("en-US", true);
        static readonly IFormatProvider EsFormat = new System.Globalization.CultureInfo("es-ES", true);
        static readonly IFormatProvider DeFormat = new System.Globalization.CultureInfo("de-DE", true);
	}
}
