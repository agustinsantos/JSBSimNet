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
namespace JSBSim.MathValues
{
	using System;
	using System.Collections;
    using System.Collections.Generic;
	using System.Xml;
	using System.IO;
	using System.Text;

	// Import log4net classes.
	using log4net;

	using CommonUtils.IO;
	using JSBSim.InputOutput;
	using JSBSim.Format;

	/// <summary>
	/// Lookup table class.
	///  Models a one, two, or three dimensional lookup table for use in FGCoefficient,
	/// /// FGPropeller, etc.  A one-dimensional table is called a "VECTOR" in a coefficient
	/// definition. For example:
	/// <pre>
	/// \<COEFFICIENT NAME="{short name}" TYPE="VECTOR">
	///  {name}
	/// {number of rows}
	/// {row lookup property}
	/// {non-dimensionalizing properties}
	/// {row_1_key} {col_1_data}
	/// {row_2_key} {...       }
	/// { ...     } {...       }
	/// {row_n_key} {...       }
	/// \</COEFFICIENT>
	/// </pre>
	/// A "real life" example is as shown here:
	/// <pre>
	///  \<COEFFICIENT NAME="CLDf" TYPE="VECTOR">
	/// Delta_lift_due_to_flap_deflection
	/// 4
	///  fcs/flap-pos-deg
	/// aero/qbar-psf | metrics/Sw-sqft
	/// 0   0
	///  10  0.20
	/// 20  0.30
	/// 30  0.35
	/// \</COEFFICIENT>
	/// </pre>
	/// The first column in the data table represents the lookup index (or "key").  In
	/// this case, the lookup index is fcs/flap-pos-deg (flap extension in degrees).
	/// If the flap position is 10 degrees, the value returned from the lookup table
	/// would be 0.20.  This value would be multiplied by qbar (aero/qbar-psf) and wing
	///  area (metrics/Sw-sqft) to get the total lift force that is a result of flap
	/// deflection (measured in pounds force).  If the value of the flap-pos-deg property
	/// was 15 (degrees), the value output by the table routine would be 0.25 - an
	/// interpolation.  If the flap position in degrees ever went below 0.0, or above
	/// 30 (degrees), the output from the table routine would be 0 and 0.35, respectively.
	/// That is, there is no _extrapolation_ to values outside the range of the lookup
	/// index.  This is why it is important to chose the data for the table wisely.

	/// The definition for a 2D table - referred to simply as a TABLE, is as follows:
	/// <pre>
	/// \<COEFFICIENT NAME="{short name}" TYPE="TABLE">
	/// {name}
	/// {number of rows}
	/// {number of columns}
	/// {row lookup property}
	/// {column lookup property}
	/// {non-dimensionalizing}
	///             {col_1_key   col_2_key   ...  col_n_key }
	/// {row_1_key} {col_1_data  col_2_data  ...  col_n_data}
	/// {row_2_key} {...         ...         ...  ...       }
	/// { ...     } {...         ...         ...  ...       }
	/// {row_n_key} {...         ...         ...  ...       }
	/// \</COEFFICIENT>
	/// </pre>
	/// A "real life" example is as shown here:
	/// <pre>
	/// \<COEFFICIENT NAME="CYb" TYPE="TABLE">
	/// Side_force_due_to_beta
	///  3
	/// 2
	///  aero/beta-rad
	/// fcs/flap-pos-deg
	///  aero/qbar-psf | metrics/Sw-sqft
	///           0     30
	/// -0.349   0.137  0.106
	///  0       0      0
	///  0.349  -0.137 -0.106
	/// \</COEFFICIENT>
	/// </pre>
	/// The definition for a 3D table in a coefficient would be (for example):
	/// <pre>
	/// \<COEFFICIENT NAME="{short name}" TYPE="TABLE3D">
	/// {name}
	/// {number of rows}
	/// {number of columns}
	/// {number of tables}
	/// {row lookup property}
	/// {column lookup property}
	/// {table lookup property}
	/// {non-dimensionalizing}
	///  {first table key}
	///             {col_1_key   col_2_key   ...  col_n_key }
	/// {row_1_key} {col_1_data  col_2_data  ...  col_n_data}
	/// {row_2_key} {...         ...         ...  ...       }
	/// { ...     } {...         ...         ...  ...       }
	///  {row_n_key} {...         ...         ...  ...       }
	/// 
	/// {second table key}
	///             {col_1_key   col_2_key   ...  col_n_key }
	/// {row_1_key} {col_1_data  col_2_data  ...  col_n_data}
	/// {row_2_key} {...         ...         ...  ...       }
	/// { ...     } {...         ...         ...  ...       }
	/// {row_n_key} {...         ...         ...  ...       }
	/// 
	///  ...
	/// 
	/// \</COEFFICIENT>
	/// </pre>
	///   [At the present time, all rows and columns for each table must have the
	///   same dimension.]

	///  In addition to using a Table for something like a coefficient, where all the
	///  row and column elements are read in from a file, a Table could be created
	///  and populated completely within program code:
	/// <pre>
	///  First column is thi, second is neta (combustion efficiency)
	/// Lookup_Combustion_Efficiency = new FGTable(12);
	/// Lookup_Combustion_Efficiency << 0.00 << 0.980;
	/// Lookup_Combustion_Efficiency << 0.90 << 0.980;
	/// Lookup_Combustion_Efficiency << 1.00 << 0.970;
	/// Lookup_Combustion_Efficiency << 1.05 << 0.950;
	/// Lookup_Combustion_Efficiency << 1.10 << 0.900;
	/// Lookup_Combustion_Efficiency << 1.15 << 0.850;
	/// Lookup_Combustion_Efficiency << 1.20 << 0.790;
	/// Lookup_Combustion_Efficiency << 1.30 << 0.700;
	/// Lookup_Combustion_Efficiency << 1.40 << 0.630;
	/// Lookup_Combustion_Efficiency << 1.50 << 0.570;
	/// Lookup_Combustion_Efficiency << 1.60 << 0.525;
	/// Lookup_Combustion_Efficiency << 2.00 << 0.345;
	/// </pre>
	///  The first column in the table, above, is thi (the lookup index, or key). The
	/// second column is the output data - in this case, "neta" (the Greek letter
	/// referring to combustion efficiency). Later on, the table is used like this:
	/// 
	/// combustion_efficiency = Lookup_Combustion_Efficiency->GetValue(equivalence_ratio);

	/// @author Jon S. Berndt
	/// </summary>
	public class Table : IParameter, ICloneable
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

        public Table(PropertyManager propMan, XmlElement element)
        {
            propertyManager = propMan;
            PropertyNode node;
            dimension = 0;
            XmlElement parent_element;
            XmlNodeList tableDataList;
            string tableData = null;
            int numLines = 0, numColumns = 0;

            isInternal = false;
            string call_type = element.GetAttribute("type");
            if (call_type.Equals("internal"))
            {
                parent_element = element.ParentNode as XmlElement;
                string parent_type = parent_element.Name;
                if (!operation_types.Contains(parent_type))
                {
                    isInternal = true;
                }
                else
                {
                    // internal table is a child element of a restricted type
                    if (log.IsErrorEnabled)
                    {
                        log.Error("An internal table cannot be nested within another type,");
                        log.Error("  such as a function. The 'internal' keyword is ignored.");
                    }
                }
            }
            else if (call_type.Length != 0)
            {
                if (log.IsErrorEnabled)
                    log.Error("  An unknown table type attribute is listed: " + call_type + ". Execution cannot continue.");
                throw new Exception("An unknown table type attribute is listed: " + call_type);
            }


            // Determine and store the lookup properties for this table unless this table
            // is part of a 3D table, in which case its independentVar property indexes will
            // be set by a call from the owning table during creation

            XmlNodeList varNodes = element.GetElementsByTagName("independentVar");
            if (varNodes.Count > 0)
            {
                foreach (XmlNode currentNode in varNodes)
                {
                    // The 'internal' attribute of the table element cannot be specified
                    // at the same time that independentVars are specified.
                    if (isInternal && log.IsErrorEnabled)
                    {
                        log.Error("  This table specifies both 'internal' call type");
                        log.Error("  and specific lookup properties via the 'independentVar' element.");
                        log.Error("  These are mutually exclusive specifications. The 'internal'");
                        log.Error("  attribute will be ignored.");
                        isInternal = false;
                    }

                    if (currentNode.NodeType == XmlNodeType.Element)
                    {
                        XmlElement currentElement = (XmlElement)currentNode;

                        node = propertyManager.GetPropertyNode(currentElement.InnerText.Trim());
                        if (node == null)
                        {
                            if (log.IsErrorEnabled)
                                log.Error("IndependenVar property, " + currentElement.InnerText.Trim() + " in Table definition is not defined.");
                            throw new Exception("IndependenVar property, " + currentElement.InnerText.Trim() + " in Table definition is not defined.");
                        }

                        if (currentElement.GetAttribute("lookup").Equals("row"))
                        {
                            lookupProperty[(int)AxisType.Row] = node.GetDoubleDelegate;
                        }
                        else if (currentElement.GetAttribute("lookup").Equals("column"))
                        {
                            lookupProperty[(int)AxisType.Column] = node.GetDoubleDelegate;
                        }
                        else if (currentElement.GetAttribute("lookup").Equals("table"))
                        {
                            lookupProperty[(int)AxisType.Table] = node.GetDoubleDelegate;
                        }
                        else
                        { // assumed single dimension table; row lookup
                            lookupProperty[(int)AxisType.Row] = node.GetDoubleDelegate;
                        }
                        dimension++;
                    }
                }
            }
            else if (isInternal) // This table is an internal table
            {
                // determine how many rows, columns, and tables in this table (dimension).
                tableDataList = element.GetElementsByTagName("tableData");
                if (tableDataList.Count > 1)
                {
                    dimension = 3; // this is a 3D table
                }
                else
                {
                    tableData = tableDataList[0].InnerText;  // examine second line in table for dimension
                    FindNumColumnsAndLines(tableData, out numColumns, out numLines);
                    if (numColumns == 2) 
                        dimension = 1;    // 1D table
                    else if (numColumns > 2) 
                        dimension = 2; // 2D table
                    else
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Invalid number of columns in table");
                    }
                }
            }
            else
            {
                // no independentVars found, and table is not marked as internal
                if (log.IsErrorEnabled)
                    log.Error("No independent variable found for table.");
                throw new Exception("No independent variable found for table.");
            }

            /* Obsolete
            for (int i = 0; i < 3; i++)
                if (lookupProperty[i] != null) dimension++;
            */

            if (tableData == null)
            {
                tableDataList = element.GetElementsByTagName("tableData");
                tableData = tableDataList[0].InnerText;  // examine second line in table for dimension
                FindNumColumnsAndLines(tableData, out numColumns, out numLines);
            }

            ReaderText rtxt = new ReaderText(new StringReader(tableData));
            switch (dimension)
            {
                case 1:
                    nRows = numLines;
                    nCols = 1;
                    tableType = TableType.Table1D;
                    data = new double[nRows + 1, nCols + 1];
                    lastRowIndex = lastColumnIndex = 2;
                    ReadTable(rtxt);
                    break;
                case 2:
                    nRows = numLines - 1;
                    nCols = numColumns;
                    if (nCols > 1)
                    {
                        tableType = TableType.Table2D;
                    }
                    else if (nCols == 1)
                    {
                        tableType = TableType.Table1D;
                    }
                    else
                    {
                        log.Error("Table cannot accept 'Rows=0'");
                    }

                    data = new double[nRows + 1, nCols + 1];
                    lastRowIndex = lastColumnIndex = 2;
                    ReadTable(rtxt);
                    break;
                case 3:
                    nTables = varNodes.Count;
                    nRows = nTables;
                    nCols = 1;
                    tableType = TableType.Table3D;
                    /*
                    Data = Allocate(); // this data array will contain the keys for the associated tables
                    Tables.reserve(nTables); // necessary?
                    tableData = el->FindElement("tableData");
                    for (i = 0; i < nTables; i++)
                    {
                        Tables.push_back(new FGTable(PropertyManager, tableData));
                        Data[i + 1][1] = tableData->GetAttributeValueAsNumber("breakPoint");
                        Tables[i]->SetRowIndexProperty(lookupProperty[eRow]);
                        Tables[i]->SetColumnIndexProperty(lookupProperty[eColumn]);
                        tableData = el->FindNextElement("tableData");
                    }  
                    */
                    break;
                default:
                    log.Error("No dimension given");
                    break;
            }
        }

        private void FindNumColumnsAndLines(string test_line, out int numColumns, out int numLines)
        {
            // determine number of data columns in table (first column is row lookup - don't count)
            ReaderText rtxt = new ReaderText(new StringReader(test_line));
            numLines = 0;
            numColumns = 0;
            while (rtxt.Done)
            {
                string tmp = rtxt.ReadLine().Trim();
                if (tmp.Length != 0)
                {
                    // determine number of data columns in table (first column is row lookup - don't count)
                    if (numColumns == 0)
                    {
                        ReaderText rcnt = new ReaderText(new StringReader(tmp));
                        while (rcnt.Done)
                        {
                            rcnt.ReadDouble();
                            if (rcnt.Done)
                                numColumns++;
                        }
                    }
                    numLines++;
                }
            }
            /*
              int position=0;
              int nCols=0;
              while ((position = test_line.find_first_not_of(" \t", position)) != string::npos) {
                nCols++;
                position = test_line.find_first_of(" \t", position);
              }
              return nCols;
             */
        }

		/// <summary>
		/// The constructor for a VECTOR table
		/// </summary>
		/// <param name="rows">the number of rows in this VECTOR table.</param>
		public Table(int rows)
		{
			nRows = rows;
			nCols = 1;
			tableType = TableType.Table1D;

			data = new double[nRows+1,nCols+1];
			lastRowIndex=lastColumnIndex=2;
		}

        public Table(double[,] useData)
        {
            data = useData;
            nRows = data.GetLength(0) - 1;
            nCols = data.GetLength(1) - 1;
            if (nCols == 1)
            {
                tableType = TableType.Table1D;
            }
            else if (nCols == 2)
            {
                tableType = TableType.Table2D;
            }
            //else if (nCols == 3)
            //{
            //    tableType = TableType.Table3D;
            //}
            else
            {
                if (log.IsErrorEnabled)

                    log.Error("Table must have 1, or 2 Dimensions");
                throw new ArgumentException("Table must have 1, or 2 Dimensions");
            }
            lastRowIndex = lastColumnIndex = 2;
        }

		public Table(int rows, int cols)		
		{
			nRows = rows;
			nCols = cols;

			if (cols > 1) 
			{
				tableType = TableType.Table2D;
			} 
			else if (cols == 1) 
			{
				tableType = TableType.Table1D;
			} 
			else 
			{
				if (log.IsErrorEnabled)
					log.Error("Table cannot accept 'Rows=0'");
			}

			data = new double[rows+1,nCols+1];
			lastRowIndex=lastColumnIndex=2;
		}


		public Table(int rows, int cols, int numTables)		
		{
			nRows = rows;
			nCols = cols;
			nTables = numTables;

			tableType = TableType.Table3D;

			data = new double[rows+1,nCols+1]; // this data array will contain the keys for the associated tables
			tables = new List<Table>(nTables);
			for (int i=0; i<nTables; i++) 
				tables.Add( new Table(rows, cols));
		}

		public double GetValue() 
		{
			switch (tableType) 
			{ 
				case TableType.Table1D:
					return GetValue(lookupProperty[(int)AxisType.Row]());
				case TableType.Table2D:
					return GetValue(lookupProperty[(int)AxisType.Row](),
									lookupProperty[(int)AxisType.Column]());
				case TableType.Table3D:
					return GetValue(lookupProperty[(int)AxisType.Row](),
									lookupProperty[(int)AxisType.Column](),
									lookupProperty[(int)AxisType.Table]());
			}
			return 0.0;
		}

        public double GetLowerKey(int dimension)
        {
            if (dimension < 1 || dimension > 3)
                throw new Exception("Dimension must be between 1 and 3");
            if (dimension == 1)
                return data[1, 0];
            else if (dimension == 2)
            {
            }
            return 0.0;//Not Implemented
        }

        public double GetUpperKey(int dimension)
        {
            if (dimension < 1 || dimension > 3)
                throw new Exception("Dimension must be between 1 and 3");
            if (dimension == 1)
                return data[nRows, 0];
            else if (dimension == 2)
            {
            }
            return 0.0;//Not Implemented
        }

		public double GetValue(double key)		
		{
			double Factor, Value, Span;
			int r=lastRowIndex;

			//if the key is off the end of the table, just return the
			//end-of-table value, do not extrapolate
			if( key <= data[1,0] ) 
			{
				lastRowIndex=2;
				//cout << "Key underneath table: " << key << endl;
				return data[1,1];
			} 
			else if ( key >= data[nRows, 0] ) 
			{
				lastRowIndex=nRows;
				//cout << "Key over table: " << key << endl;
				return data[nRows, 1];
			}

			// the key is somewhere in the middle, search for the right breakpoint
			// assume the correct breakpoint has not changed since last frame or
			// has only changed very little

			if ( r > 2 && data[r-1, 0] > key ) 
			{
				while( data[r-1, 0] > key && r > 2) { r--; }
			} 
			else if ( data[r, 0] < key ) 
			{
				while( data[r, 0] <= key && r <= nRows) { r++; }
			}

			lastRowIndex=r;
			// make sure denominator below does not go to zero.

			Span = data[r, 0] - data[r-1, 0];
			if (Span != 0.0) 
			{
				Factor = (key - data[r-1, 0]) / Span;
				if (Factor > 1.0) Factor = 1.0;
			} 
			else 
			{
				Factor = 1.0;
			}

			Value = Factor*(data[r, 1] - data[r-1, 1]) + data[r-1, 1];

			return Value;
		}

		public double GetValue(double rowKey, double colKey)		
		{
			double rFactor, cFactor, col1temp, col2temp, Value;
			int r=lastRowIndex;
			int c=lastColumnIndex;

			if ( r > 2 && data[r-1, 0] > rowKey ) 
			{
				while ( data[r-1, 0] > rowKey && r > 2) { r--; }
			} 
			else if ( data[r, 0] < rowKey ) 
			{
				//    cout << Data[r, 0] << endl;
				while ( r <= nRows && data[r, 0] <= rowKey ) { r++; }
				if ( r > nRows ) r = nRows;
			}

			if ( c > 2 && data[0, c-1] > colKey ) 
			{
				while( data[0, c-1] > colKey && c > 2) { c--; }
			} 
			else if ( data[0, c] < colKey ) 
			{
				while(c <= nCols && data[0, c] <= colKey) { c++; }
				if ( c > nCols ) c = nCols;
			}

			lastRowIndex=r;
			lastColumnIndex=c;

			rFactor = (rowKey - data[r-1, 0]) / (data[r, 0] - data[r-1, 0]);
			cFactor = (colKey - data[0, c-1]) / (data[0, c] - data[0, c-1]);

			if (rFactor > 1.0) rFactor = 1.0;
			else if (rFactor < 0.0) rFactor = 0.0;

			if (cFactor > 1.0) cFactor = 1.0;
			else if (cFactor < 0.0) cFactor = 0.0;

			col1temp = rFactor*(data[r, c-1] - data[r-1, c-1]) + data[r-1, c-1];
			col2temp = rFactor*(data[r, c] - data[r-1, c]) + data[r-1, c];

			Value = col1temp + cFactor*(col2temp - col1temp);

			return Value;
		}

		public double GetValue(double rowKey, double colKey, double tableKey)		
		{
			double Factor, Value, Span;
			int r=lastRowIndex;

			//if the key is off the end  (or before the beginning) of the table,
			// just return the boundary-table value, do not extrapolate

			if( tableKey <= data[1, 1] ) 
			{
				lastRowIndex=2;
				return ((Table)tables[0]).GetValue(rowKey, colKey);
			} 
			else if ( tableKey >= data[nRows, 1] ) 
			{
				lastRowIndex=nRows;
				return ((Table)tables[nRows-1]).GetValue(rowKey, colKey);
			}

			// the key is somewhere in the middle, search for the right breakpoint
			// assume the correct breakpoint has not changed since last frame or
			// has only changed very little

			if ( r > 2 && data[r-1, 1] > tableKey ) 
			{
				while( data[r-1, 1] > tableKey && r > 2) { r--; }
			} 
			else if ( data[r, 1] < tableKey ) 
			{
				while( data[r, 1] <= tableKey && r <= nRows) { r++; }
			}

			lastRowIndex=r;
			// make sure denominator below does not go to zero.

			Span = data[r, 1] - data[r-1, 1];
			if (Span != 0.0) 
			{
				Factor = (tableKey - data[r-1, 1]) / Span;
				if (Factor > 1.0) Factor = 1.0;
			} 
			else 
			{
				Factor = 1.0;
			}

			Value = Factor*(((Table)tables[r-1]).GetValue(rowKey, colKey) - 
				((Table)tables[r-2]).GetValue(rowKey, colKey))
				+ ((Table)tables[r-1]).GetValue(rowKey, colKey);

			return Value;
		}

		/** Read the table in.
			Data in the config file should be in matrix format with the row
			independents as the first column and the column independents in
			the first row.  The implication of this layout is that there should
			be no value in the upper left corner of the matrix e.g:
			<pre>
				 0  10  20 30 ...
			-5   1  2   3  4  ...
			 ...
			 </pre>

			 For multiple-table (i.e. 3D) data sets there is an additional number
			 key in the table definition. For example:

			<pre>
			 0.0
				 0  10  20 30 ...
			-5   1  2   3  4  ...
			 ...
			 </pre>
			 */

        public TableType GetTableType()
        {
            return tableType;
        }
		public void ReadTable(ReaderText rtxt)
		{
			int startRow=0;
			int startCol=0;
			int tableCtr=0;

			if (tableType == TableType.Table1D || 
				tableType == TableType.Table3D) 
				startRow = 1;
			if (tableType == TableType.Table3D) startCol = 1;

            try
            {
                for (int r = startRow; r <= nRows; r++)
                {
                    for (int c = startCol; c <= nCols; c++)
                    {
                        if (r != 0 || c != 0)
                        {
                            data[r, c] = rtxt.ReadDouble();
                            if (tableType == TableType.Table3D)
                            {
                                ((Table)tables[tableCtr]).ReadTable(rtxt);
                                tableCtr++;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("Exception " + e + " reading Table data");

            }

			if (log.IsDebugEnabled) 
			{
				StringBuilder buff = new StringBuilder();
				Print(0, buff);
				log.Debug(buff);
			}
		}


		void Print(int spaces, StringBuilder buff)
		{

			string tabspace ="";
			int startRow=0;
			int startCol=0;

			if (tableType == TableType.Table1D || tableType == TableType.Table3D) startRow = 1;
			if (tableType == TableType.Table3D) startCol = 1;

			buff.Append("\n");

			for (int i=0;i<spaces;i++) tabspace+=" ";
			
			for (int r=startRow; r<=nRows; r++) 
			{
				buff.Append(tabspace);
				for (int c=startCol; c<=nCols; c++) 
				{
					if (r == 0 && c == 0) 
					{
						log.Debug(" ");
					} 
					else 
					{
						buff.Append(data[r,c].ToString("F4",FormatHelper.numberFormatInfo) + "\t");
						if (tableType == TableType.Table3D) 
						{
							buff.Append("\n");
							((Table)tables[r-1]).Print(spaces, buff);
						}
					}
				}
				buff.Append("\n");
			}
		}

		public enum TableType {Table1D, Table2D, Table3D};
		private enum AxisType {Row=0, Column, Table};

		private TableType tableType;
		private double[,] data;

        private bool isInternal = false;
		private List<Table> tables = new List<Table>();
		private int nRows, nCols, nTables, dimension;
        private int lastRowIndex, lastColumnIndex, tableCounter;
		private PropertyManager propertyManager;
		private PropertyNode.GetDoubleValueDelegate[] lookupProperty = new PropertyNode.GetDoubleValueDelegate[3];
		
        private const string operation_types = "function, product, sum, difference, quotient, pow, abs, sin, cos, asin, acos, tan, atan, table";

		#region ICloneable Members

		public object Clone()
		{
			// TODO:  Add Table.Clone implementation
			return null;
		}

		#endregion
	}
}
