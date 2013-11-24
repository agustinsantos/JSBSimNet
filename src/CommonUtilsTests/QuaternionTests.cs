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
	/// Summary description for QuaternionTests.
	/// </summary>
	[TestFixture]
	public class QuaternionTests
	{
		[Test]
		public void Construction()
		{
			Quaternion q;

			// Zero vector
			q = Quaternion.Zero;
			Assert.AreEqual(0, q.W, "Zero Quaternion - W");
			Assert.AreEqual(0, q.X, "Zero Quaternion - X");
			Assert.AreEqual(0, q.Y, "Zero Quaternion - Y");
			Assert.AreEqual(0, q.Z, "Zero Quaternion - Z");

			// W-Axis vector
			q = Quaternion.WAxis;
			Assert.AreEqual(1, q.W, "WAxis Quaternion - W");
			Assert.AreEqual(0, q.X, "WAxis Quaternion - X");
			Assert.AreEqual(0, q.Y, "WAxis Quaternion - Y");
			Assert.AreEqual(0, q.Z, "WAxis Quaternion - Z");

			// X-Axis vector
			q = Quaternion.XAxis;
			Assert.AreEqual(0, q.W, "XAxis Quaternion - W");
			Assert.AreEqual(1, q.X, "XAxis Quaternion - X");
			Assert.AreEqual(0, q.Y, "XAxis Quaternion - Y");
			Assert.AreEqual(0, q.Z, "XAxis Quaternion - Z");

			// Y-Axis vector
			q = Quaternion.YAxis;
			Assert.AreEqual(0, q.W, "YAxis Quaternion - W");
			Assert.AreEqual(0, q.X, "YAxis Quaternion - X");
			Assert.AreEqual(1, q.Y, "YAxis Quaternion - Y");
			Assert.AreEqual(0, q.Z, "YAxis Quaternion - Z");

			// Z-Axis vector
			q = Quaternion.ZAxis;
			Assert.AreEqual(0, q.W, "ZAxis Quaternion - W");
			Assert.AreEqual(0, q.X, "ZAxis Quaternion - X");
			Assert.AreEqual(0, q.Y, "ZAxis Quaternion - Y");
			Assert.AreEqual(1, q.Z, "ZAxis Quaternion - Z");

			// Constructors
			Quaternion a = new Quaternion(1,2,3,4);
			Assert.AreEqual(1, a.W, "Constructors Quaternion - W");
			Assert.AreEqual(2, a.X, "Constructors Quaternion - X");
			Assert.AreEqual(3, a.Y, "Constructors Quaternion - Y");
			Assert.AreEqual(4, a.Z, "Constructors Quaternion - Z");
			
			Quaternion b = new Quaternion(4,3,2,1);
			Assert.AreEqual(4, b.W, "Constructors Quaternion - W");
			Assert.AreEqual(3, b.X, "Constructors Quaternion - X");
			Assert.AreEqual(2, b.Y, "Constructors Quaternion - Y");
			Assert.AreEqual(1, b.Z, "Constructors Quaternion - Z");

			double [] fa = { 0.1, 0.2, 0.3, 0.4 };
			q = new Quaternion(fa);
			Assert.AreEqual(0.1, q.W, "Constructors Quaternion - W");
			Assert.AreEqual(0.2, q.X, "Constructors Quaternion - X");
			Assert.AreEqual(0.3, q.Y, "Constructors Quaternion - Y");
			Assert.AreEqual(0.4, q.Z, "Constructors Quaternion - Z");

			q = new Quaternion(new Quaternion(0.1, 0.2, 0.3, 0.4));
			Assert.AreEqual(0.1, q.W, "Constructors Quaternion - W");
			Assert.AreEqual(0.2, q.X, "Constructors Quaternion - X");
			Assert.AreEqual(0.3, q.Y, "Constructors Quaternion - Y");
			Assert.AreEqual(0.4, q.Z, "Constructors Quaternion - Z");

		}

		[Test]
		public void ScalarOperators()
		{
			Quaternion q = new Quaternion(0.1, 0.2, 0.3, 0.4);

			q *= 2.0;
			Assert.AreEqual(0.2, q.W, "ScalarOperators Mult. Quaternion - W");
			Assert.AreEqual(0.4, q.X, "ScalarOperators Mult. Quaternion - X");
			Assert.AreEqual(0.6, q.Y, "ScalarOperators Mult. Quaternion - Y");
			Assert.AreEqual(0.8, q.Z, "ScalarOperators Mult. Quaternion - Z");

			q /= 2.0f;
			Assert.AreEqual(0.1, q.W, "ScalarOperators Div. Quaternion - W");
			Assert.AreEqual(0.2, q.X, "ScalarOperators Div. Quaternion - X");
			Assert.AreEqual(0.3, q.Y, "ScalarOperators Div. Quaternion - Y");
			Assert.AreEqual(0.4, q.Z, "ScalarOperators Div. Quaternion - Z");

			q.Multiply(3.0);
			Assert.AreEqual(0.3, q.W, "ScalarOperators Mult. Quaternion - W");
			Assert.AreEqual(0.6, q.X, "ScalarOperators Mult. Quaternion - X");
			Assert.AreEqual(0.9, q.Y, "ScalarOperators Mult. Quaternion - Y");
			Assert.AreEqual(1.2, q.Z, "ScalarOperators Mult. Quaternion - Z");

			q.Divide(3.0);
			Assert.AreEqual(0.1, q.W, "ScalarOperators Div. Quaternion - W");
			Assert.AreEqual(0.2, q.X, "ScalarOperators Div. Quaternion - X");
			Assert.AreEqual(0.3, q.Y, "ScalarOperators Div. Quaternion - Y");
			Assert.AreEqual(0.4, q.Z, "ScalarOperators Div. Quaternion - Z");


		}

		[Test]
		public void QuaternionOperators()
		{
			Quaternion a = new Quaternion(0.1, 0.2, 0.3, 0.4);
			Quaternion b = new Quaternion(0.4, 0.3, 0.2, 0.1);

			Assert.AreEqual(new Quaternion(0.1, 0.2, 0.3, 0.4), a);
			Assert.AreEqual(new Quaternion(0.4, 0.3, 0.2, 0.1), b);

			Quaternion result1 = a + b;
			Quaternion result2 = a - b;

			Assert.AreEqual(0.5, result1.W, "VectorOperators Sum Quaternion - W");
			Assert.AreEqual(0.5, result1.X, "VectorOperators Sum Quaternion - X");
			Assert.AreEqual(0.5, result1.Y, "VectorOperators Sum Quaternion - Y");
			Assert.AreEqual(0.5, result1.Z, "VectorOperators Sum Quaternion - Z");

			Assert.AreEqual(-0.3, result2.W, "VectorOperators Subs Quaternion - W");
			Assert.AreEqual(-0.1, result2.X, "VectorOperators Subs Quaternion - X");
			Assert.AreEqual(0.1, result2.Y, "VectorOperators Subs Quaternion - Y");
			Assert.AreEqual(0.3, result2.Z, "VectorOperators Subs Quaternion - Z");

			Assert.AreEqual(result1, a+b);
			Assert.AreEqual(new Quaternion(0.5, 0.5, 0.5, 0.5), result1);
			Assert.AreEqual(new Quaternion(0.5, 0.5, 0.5, 0.5), a+b);

			Assert.AreEqual(result2, a-b);

			Assert.AreEqual(new Quaternion(-0.3, -0.1, 0.1, 0.3), result2);
			Assert.AreEqual(new Quaternion(-0.3, -0.1, 0.1, 0.3), a-b);

			Assert.AreEqual(new Quaternion(0.5, 0.5, 0.5, 0.5), a +=b);
			Assert.AreEqual(new Quaternion(0.1, 0.2, 0.3, 0.4), a -= b);

			a.Add(b);
			Assert.AreEqual(new Quaternion(0.5, 0.5, 0.5, 0.5), a);

			a.Subtract(b);
			Assert.AreEqual(new Quaternion(0.1, 0.2, 0.3, 0.4), a);

		}

		[Test]
		public void QuaternionIndex()
		{
			Quaternion q = new Quaternion(0.1, 0.2, 0.3, 0.4);

			Assert.AreEqual(0.1, q[0], "Quaternion Index - W");
			Assert.AreEqual(0.2, q[1], "Quaternion Index - X");
			Assert.AreEqual(0.3, q[2], "Quaternion Index - Y");
			Assert.AreEqual(0.4, q[3], "Quaternion Index - Z");

		}

		[Test]
		public void QuaternionMultiply()
		{
			Assert.AreEqual( new Quaternion(1,0,0,0),  Quaternion.WAxis * Quaternion.WAxis); // quat * quat
			Assert.AreEqual( new Quaternion(-1,0,0,0), Quaternion.XAxis * Quaternion.XAxis);
			Assert.AreEqual( new Quaternion(-1,0,0,0), Quaternion.YAxis * Quaternion.YAxis);
			Assert.AreEqual( new Quaternion(-1,0,0,0), Quaternion.ZAxis * Quaternion.ZAxis);

			Assert.AreEqual( new Quaternion(0,1,0,0),  Quaternion.WAxis * Quaternion.XAxis);
			Assert.AreEqual( new Quaternion(0,0,1,0),  Quaternion.WAxis * Quaternion.YAxis);
			Assert.AreEqual( new Quaternion(0,0,0,1),  Quaternion.WAxis * Quaternion.ZAxis);
			Assert.AreEqual( new Quaternion(0,0,0,1),  Quaternion.XAxis * Quaternion.YAxis);
			Assert.AreEqual( new Quaternion(0,0,-1,0), Quaternion.XAxis * Quaternion.ZAxis);
			Assert.AreEqual( new Quaternion(0,1,0,0),  Quaternion.YAxis * Quaternion.ZAxis);
			Assert.AreEqual( new Quaternion(0,1,0,0),  Quaternion.XAxis * Quaternion.WAxis);
			Assert.AreEqual( new Quaternion(0,0,1,0),  Quaternion.YAxis * Quaternion.WAxis);
			Assert.AreEqual( new Quaternion(0,0,0,1),  Quaternion.ZAxis * Quaternion.WAxis);
			Assert.AreEqual( new Quaternion(0,0,0,-1), Quaternion.YAxis * Quaternion.XAxis);
			Assert.AreEqual( new Quaternion(0,0,1,0),  Quaternion.ZAxis * Quaternion.XAxis);
			Assert.AreEqual( new Quaternion(0,-1,0,0), Quaternion.ZAxis * Quaternion.YAxis);
		}

		/*
		[Test]
		public void QuaternionRandom()
		{

			Random rand = new Random();
			for (int i = 0; i < 50; i++) 
			{
				Quaternion a = new Quaternion(rand.NextDouble(),rand.NextDouble(),rand.NextDouble(),rand.NextDouble());
				Quaternion b = new Quaternion(rand.NextDouble(),rand.NextDouble(),rand.NextDouble(),rand.NextDouble());
				double f = rand.NextDouble();		
				
				Quaternion c;

				// random divisions
				// avoid a zero divide
				if (b.GetMagnitude() < 1e-3 || b.GetMagnitude() < 1e-3 || Math.Abs(f) < 1e-3) continue; 
				//c = a / b;     Assert.AreEqual( c * b , a );
				//c = a / f;     Assert.AreEqual( c * f , a );
				//c = f / a;     Assert.AreEqual( c * a , QuatD(f) );
				//c = a / 3.0;     Assert.AreEqual( c * 3.0 , a );
				//c = 3.0 / a;     Assert.AreEqual( c * a , QuatD(3) );
  
			}
		}
		*/
	}
}
