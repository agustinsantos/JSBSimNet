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
    /// Models a one, two, or three dimensional lookup table for use in aerodynamics
    /// and function definitions.
    /// 
    /// For a single "vector" lookup table, the format is as follows:
    /// 
    /// @code
    /// <table name="property_name">
    ///   <independentVar lookup = "row" > property_name </ independentVar >
    /// 
    ///   <tableData>
    ///         key_1 value_1
    ///         key_2 value_2
    ///         ...  ...
    ///         key_n value_n
    ///   </tableData>
    /// </table>
    /// @endcode
    /// 
    /// The lookup= "row" attribute in the independentVar element is option in this case;
    /// it is assumed that the independentVar is a row variable.
    /// 
    /// A "real life" example is as shown here:
    /// 
    /// @code
    /// <table>
    ///   <independentVar lookup = "row"> aero/alpha-rad </independentVar>
    ///   < tableData >
    ///    -1.57  1.500
    ///    -0.26  0.033
    ///     0.00  0.025
    ///     0.26  0.033
    ///     1.57  1.500
    ///   </tableData>
    /// </table>
    /// @endcode
    /// 
    /// The first column in the data table represents the lookup index(or "key").  In
    /// this case, the lookup index is aero/alpha-rad (angle of attack in radians).
    /// If alpha is 0.26 radians, the value returned from the lookup table
    /// would be 0.033.
    /// 
    /// The definition for a 2D table, is as follows:
    /// 
    /// @code
    /// <table name="property_name">
    ///   <independentVar lookup="row"> property_name </independentVar>
    ///   <independentVar lookup="column"> property_name </independentVar>
    ///   <tableData>
    ///                  { col_1_key col_2_key   ...  col_n_key }
    ///     { row_1_key} { col_1_data col_2_data  ...  col_n_data}
    ///     { row_2_key} {...         ...         ...  ...       }
    ///     { ...      } {...         ...         ...  ...       }
    ///     { row_n_key} {...         ...         ...  ...       }
    ///   </tableData>
    /// </table>
    /// @endcode
    /// 
    /// The data is in a gridded format.
    /// 
    /// A "real life" example is as shown below. Alpha in radians is the row lookup (alpha
    /// breakpoints are arranged in the first column) and flap position in degrees is
    /// 
    /// @code
    /// <table>
    ///   <independentVar lookup="row">aero/alpha-rad</independentVar>
    ///   <independentVar lookup="column">fcs/flap-pos-deg</independentVar>
    ///   <tableData>
    ///                 0.0         10.0        20.0         30.0
    ///     -0.0523599  8.96747e-05 0.00231942  0.0059252    0.00835082
    ///     -0.0349066  0.000313268 0.00567451  0.0108461    0.0140545
    ///     -0.0174533  0.00201318  0.0105059   0.0172432    0.0212346
    ///      0.0        0.0051894   0.0168137   0.0251167    0.0298909
    ///      0.0174533  0.00993967  0.0247521   0.0346492    0.0402205
    ///      0.0349066  0.0162201   0.0342207   0.0457119    0.0520802
    ///      0.0523599  0.0240308   0.0452195   0.0583047    0.0654701
    ///      0.0698132  0.0333717   0.0577485   0.0724278    0.0803902
    ///      0.0872664  0.0442427   0.0718077   0.088081     0.0968405
    ///   </tableData>
    /// </table>
    /// @endcode
    /// 
    /// The definition for a 3D table in a coefficient would be (for example):
    /// 
    /// @code
    /// <table name="property_name">
    ///   <independentVar lookup="row"> property_name </independentVar>
    ///   <independentVar lookup="column"> property_name </independentVar>
    ///   <tableData breakpoint="table_1_key">
    ///                  { col_1_key col_2_key   ...  col_n_key }
    ///     { row_1_key} { col_1_data col_2_data  ...  col_n_data}
    ///     { row_2_key} {...         ...         ...  ...       }
    ///     { ...      } {...         ...         ...  ...       }
    ///     { row_n_key} {...         ...         ...  ...       }
    ///   </tableData>
    ///   <tableData breakpoint="table_2_key">
    ///                  { col_1_key col_2_key   ...  col_n_key }
    ///     { row_1_key} { col_1_data col_2_data  ...  col_n_data}
    ///     { row_2_key} {...         ...         ...  ...       }
    ///     { ...      } {...         ...         ...  ...       }
    ///     { row_n_key} {...         ...         ...  ...       }
    ///   </tableData>
    ///   ...
    ///   <tableData breakpoint="table_n_key">
    ///                  { col_1_key col_2_key   ...  col_n_key }
    ///     { row_1_key} { col_1_data col_2_data  ...  col_n_data}
    ///     { row_2_key} {...         ...         ...  ...       }
    ///     { ...      } {...         ...         ...  ...       }
    ///     { row_n_key} {...         ...         ...  ...       }
    ///   </tableData>
    /// </table>
    /// @endcode
    /// 
    /// [Note the "breakpoint" attribute in the tableData element, above.]
    /// 
    ///     Here's an example:
    /// 
    /// @code
    /// <table>
    ///   <independentVar lookup="row">fcs/row-value</independentVar>
    ///   <independentVar lookup="column">fcs/column-value</independentVar>
    ///   <independentVar lookup="table">fcs/table-value</independentVar>
    ///   <tableData breakPoint="-1.0">
    ///            -1.0     1.0
    ///     0.0     1.0000  2.0000
    ///     1.0     3.0000  4.0000
    ///   </tableData>
    ///   <tableData breakPoint="0.0000">
    ///             0.0     10.0
    ///     2.0     1.0000  2.0000
    ///     3.0     3.0000  4.0000
    ///   </tableData>
    ///   <tableData breakPoint="1.0">
    ///            0.0     10.0     20.0
    ///      2.0   1.0000   2.0000   3.0000
    ///      3.0   4.0000   5.0000   6.0000
    ///     10.0   7.0000   8.0000   9.0000
    ///   </tableData>
    /// </table>
    /// @endcode
    /// 
    /// In addition to using a Table for something like a coefficient, where all the
    /// row and column elements are read in from a file, a Table could be created
    /// and populated completely within program code:
    /// 
    /// @code
    /// // First column is thi, second is neta (combustion efficiency)
    /// Lookup_Combustion_Efficiency = new FGTable(12);
    /// 
    /// * Lookup_Combustion_Efficiency &lt;&lt; 0.00 &lt;&lt; 0.980;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 0.90 &lt;&lt; 0.980;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 1.00 &lt;&lt; 0.970;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 1.05 &lt;&lt; 0.950;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 1.10 &lt;&lt; 0.900;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 1.15 &lt;&lt; 0.850;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 1.20 &lt;&lt; 0.790;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 1.30 &lt;&lt; 0.700;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 1.40 &lt;&lt; 0.630;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 1.50 &lt;&lt; 0.570;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 1.60 &lt;&lt; 0.525;
    /// * Lookup_Combustion_Efficiency &lt;&lt; 2.00 &lt;&lt; 0.345;
    /// @endcode
    /// 
    /// The first column in the table, above, is thi(the lookup index, or key). The
    /// second column is the output data - in this case, "neta" (the Greek letter
    /// referring to combustion efficiency). Later on, the table is used like this:
    /// 
    /// @code
    /// combustion_efficiency = Lookup_Combustion_Efficiency->GetValue(equivalence_ratio);
    /// @endcode
    /// 
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

        public Table(PropertyManager propMan, XmlElement element, string prefix = "")
        {
            propertyManager = propMan;
            PropertyValue node;
            this.prefix = prefix;

            nTables = 0;

            // Is this an internal lookup table?

            isInternal = false;
            name = element.GetAttribute("name"); // Allow this table to be named with a property

            string brkpt_string = null;
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
                        log.Error("such as a function. The 'internal' keyword of table " + name + " is ignored.");
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

            dimension = 0;

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
                        string property_string = currentElement.InnerText.Trim();
                        if (property_string.Contains("#"))
                        {
                            double n;
                            if (double.TryParse(prefix, out n))
                            {
                                property_string = property_string.Replace("#", prefix);
                            }
                        }
                        node = new PropertyValue(property_string, propertyManager);
                        if (node == null)
                        {
                            if (log.IsErrorEnabled)
                                log.Error("IndependenVar property, " + currentElement.InnerText.Trim() + " in Table definition is not defined.");
                            throw new Exception("IndependenVar property, " + currentElement.InnerText.Trim() + " in Table definition is not defined.");
                        }

                        if (currentElement.GetAttribute("lookup").Equals("row"))
                        {
                            lookupProperty[(int)AxisType.Row] = node;
                        }
                        else if (currentElement.GetAttribute("lookup").Equals("column"))
                        {
                            lookupProperty[(int)AxisType.Column] = node;
                        }
                        else if (currentElement.GetAttribute("lookup").Equals("table"))
                        {
                            lookupProperty[(int)AxisType.Table] = node;
                        }
                        else
                        { // assumed single dimension table; row lookup
                            lookupProperty[(int)AxisType.Row] = node;
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
                brkpt_string = element.GetAttribute("breakPoint");
                if (string.IsNullOrEmpty(brkpt_string))
                {
                    // no independentVars found, and table is not marked as internal
                    if (log.IsErrorEnabled)
                        log.Error("No independent variable found for table.");
                    throw new Exception("No independent variable found for table.");
                }
            }

            // end lookup property code

            if (string.IsNullOrEmpty(brkpt_string)) // Not a 3D table "table element"
            {
                //TODO tableData = element.GetElementsByTagName("tableData")[0] as XmlElement;
            }
            else // This is a table in a 3D table
            {
                //TODO  tableData = element;
                dimension = 2; // Currently, infers 2D table
            }

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
                    colCounter = 0;
                    rowCounter = 1;
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
                        colCounter = 1;
                        rowCounter = 0;
                    }
                    else if (nCols == 1)
                    {
                        tableType = TableType.Table1D;
                        colCounter = 1;
                        rowCounter = 1;
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
                    colCounter = 1;
                    rowCounter = 1;
                    lastRowIndex = lastColumnIndex = 2;
                    data = new double[nRows + 1, nCols + 1]; // this data array will contain the keys for the associated tables
                    tables.Capacity = nTables; // necessary?
                    XmlNodeList tableDataElems = element.GetElementsByTagName("tableData");
                    tableData = tableDataElems[0].InnerText;
                    for (int i = 0; i < nTables; i++)
                    {
                        XmlElement tableDataElem = tableDataElems[i] as XmlElement;
                        tables.Add(new Table(propertyManager, tableDataElem));
                        data[i + 1, 1] = double.Parse(tableDataElem.GetAttribute("breakPoint"));
                        tables[i].lookupProperty[(int)AxisType.Row] = lookupProperty[(int)AxisType.Row];
                        tables[i].lookupProperty[(int)AxisType.Column] = lookupProperty[(int)AxisType.Column];
                    }
                    break;
                default:
                    log.Error("No dimension given");
                    break;
            }

            // Sanity checks: lookup indices must be increasing monotonically
            int r, c, b;

            // find next xml element containing a name attribute
            // to indicate where the error occured
            XmlElement nameel = element;
            while (nameel != null && nameel.GetAttribute("name") == "")
                nameel = nameel.ParentNode as XmlElement;

            // check breakpoints, if applicable
            if (dimension > 2)
            {
                for (b = 2; b <= nTables; ++b)
                {
                    if (data[b, 1] <= data[b - 1, 1])
                    {
                        string errormsg = "  Table: breakpoint lookup is not monotonically increasing\n" +
                                          "  in breakpoint " + b;
                        if (nameel != null) errormsg += " of table in " + nameel.GetAttribute("name");
                        errormsg += ":\n" + "  " + data[b, 1] + "<=" + data[b - 1, 1] + "\n";
                        throw new Exception(errormsg);
                    }
                }
            }

            // check columns, if applicable
            if (dimension > 1)
            {
                for (c = 2; c <= nCols; ++c)
                {
                    if (data[0, c] <= data[0, c - 1])
                    {
                        string errormsg = "  FGTable: column lookup is not monotonically increasing\n" +
                                          "  in column " + c;
                        if (nameel != null) errormsg += " of table in " + nameel.GetAttribute("name");
                        errormsg += ":\n" + "  " + data[0, c] + "<=" + data[0, c - 1] + "\n";
                        throw new Exception(errormsg);
                    }
                }
            }

            // check rows
            if (dimension < 3)
            { // in 3D tables, check only rows of subtables
                for (r = 2; r <= nRows; ++r)
                {
                    if (data[r, 0] <= data[r - 1, 0])
                    {
                        string errormsg = "  FGTable: row lookup is not monotonically increasing\n" +
                                          "  in row " + r;
                        if (nameel != null) errormsg += " of table in " + nameel.GetAttribute("name");
                        errormsg += ":\n" + "  " + data[r, 0] + "<=" + data[r - 1, 0] + "\n";
                        throw new Exception(errormsg);
                    }
                }
            }

            Bind(element);

            if (log.IsDebugEnabled)
                log.Debug(Print());
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
            propertyManager = null;
            tableType = TableType.Table1D;
            colCounter = 0;
            rowCounter = 1;
            nTables = 0;

            data = new double[nRows + 1, nCols + 1];
            lastRowIndex = lastColumnIndex = 2;
        }

        /// <summary>
        /// Data in the config file should be in matrix format with the row
        /// independents as the first column and the column independents in
        /// the first row.The implication of this layout is that there should
        /// be no value in the upper left corner of the matrix e.g:
        /// <pre>
        ///      0  10  20 30 ...
        /// -5   1  2   3  4  ...
        ///  ...
        ///  </pre>
        /// </summary>
        /// <param name="useData"></param>
        public Table(double[,] useData)
        {
            if (useData == null)
            {
                log.Error("No table data");
                throw new ArgumentException("No table data");
            }
            
            nRows = useData.GetLength(0) - 1;
            nCols = useData.GetLength(1) - 1;
            if (nCols == 1)
            {
                tableType = TableType.Table1D;
            }
            else 
                tableType = TableType.Table2D;
             lastRowIndex = lastColumnIndex = 2;

            data = (double[,])useData.Clone();
        }

        public Table(int rows, int cols)
        {
            nRows = rows;
            nCols = cols;
            propertyManager = null;

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

            data = new double[rows + 1, nCols + 1];
            lastRowIndex = lastColumnIndex = 2;
        }

        public Table(int rows, int cols, int numTables)
        {
            nRows = rows;
            nCols = cols;
            nTables = numTables;

            tableType = TableType.Table3D;

            data = new double[rows + 1, nCols + 1]; // this data array will contain the keys for the associated tables
            tables = new List<Table>(nTables);
            for (int i = 0; i < nTables; i++)
                tables.Add(new Table(rows, cols));
        }

        public double GetValue()
        {
            switch (tableType)
            {
                case TableType.Table1D:
                    return GetValue(lookupProperty[(int)AxisType.Row].GetDoubleValue());
                case TableType.Table2D:
                    return GetValue(lookupProperty[(int)AxisType.Row].GetDoubleValue(),
                                    lookupProperty[(int)AxisType.Column].GetDoubleValue());
                case TableType.Table3D:
                    return GetValue(lookupProperty[(int)AxisType.Row].GetDoubleValue(),
                                    lookupProperty[(int)AxisType.Column].GetDoubleValue(),
                                    lookupProperty[(int)AxisType.Table].GetDoubleValue());
                default:
                    if (log.IsErrorEnabled)
                        log.Error("Attempted to GetValue() for invalid/unknown table type");
                    throw new Exception("Attempted to GetValue() for invalid/unknown table type");
            }
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
            int r = lastRowIndex;

            //if the key is off the end of the table, just return the
            //end-of-table value, do not extrapolate
            if (key <= data[1, 0])
            {
                lastRowIndex = 2;
                //cout << "Key underneath table: " << key << endl;
                return data[1, 1];
            }
            else if (key >= data[nRows, 0])
            {
                lastRowIndex = nRows;
                //cout << "Key over table: " << key << endl;
                return data[nRows, 1];
            }

            // the key is somewhere in the middle, search for the right breakpoint
            // assume the correct breakpoint has not changed since last frame or
            // has only changed very little

            if (r > 2 && data[r - 1, 0] > key)
            {
                while (data[r - 1, 0] > key && r > 2) { r--; }
            }
            else if (data[r, 0] < key)
            {
                while (data[r, 0] <= key && r <= nRows) { r++; }
            }

            lastRowIndex = r;
            // make sure denominator below does not go to zero.

            Span = data[r, 0] - data[r - 1, 0];
            if (Span != 0.0)
            {
                Factor = (key - data[r - 1, 0]) / Span;
                if (Factor > 1.0) Factor = 1.0;
            }
            else
            {
                Factor = 1.0;
            }

            Value = Factor * (data[r, 1] - data[r - 1, 1]) + data[r - 1, 1];

            return Value;
        }

        public double GetValue(double rowKey, double colKey)
        {
            double rFactor, cFactor, col1temp, col2temp, Value;
            int r = lastRowIndex;
            int c = lastColumnIndex;

            if (r > 2 && data[r - 1, 0] > rowKey)
            {
                while (data[r - 1, 0] > rowKey && r > 2) { r--; }
            }
            else if (data[r, 0] < rowKey)
            {
                //    cout << Data[r, 0] << endl;
                while (r <= nRows && data[r, 0] <= rowKey) { r++; }
                if (r > nRows) r = nRows;
            }

            if (c > 2 && data[0, c - 1] > colKey)
            {
                while (data[0, c - 1] > colKey && c > 2) { c--; }
            }
            else if (data[0, c] < colKey)
            {
                while (c <= nCols && data[0, c] <= colKey) { c++; }
                if (c > nCols) c = nCols;
            }

            lastRowIndex = r;
            lastColumnIndex = c;

            rFactor = (rowKey - data[r - 1, 0]) / (data[r, 0] - data[r - 1, 0]);
            cFactor = (colKey - data[0, c - 1]) / (data[0, c] - data[0, c - 1]);

            if (rFactor > 1.0) rFactor = 1.0;
            else if (rFactor < 0.0) rFactor = 0.0;

            if (cFactor > 1.0) cFactor = 1.0;
            else if (cFactor < 0.0) cFactor = 0.0;

            col1temp = rFactor * (data[r, c - 1] - data[r - 1, c - 1]) + data[r - 1, c - 1];
            col2temp = rFactor * (data[r, c] - data[r - 1, c]) + data[r - 1, c];

            Value = col1temp + cFactor * (col2temp - col1temp);

            return Value;
        }

        public double GetValue(double rowKey, double colKey, double tableKey)
        {
            double Factor, Value, Span;
            int r = lastRowIndex;

            //if the key is off the end  (or before the beginning) of the table,
            // just return the boundary-table value, do not extrapolate

            if (tableKey <= data[1, 1])
            {
                lastRowIndex = 2;
                return ((Table)tables[0]).GetValue(rowKey, colKey);
            }
            else if (tableKey >= data[nRows, 1])
            {
                lastRowIndex = nRows;
                return ((Table)tables[nRows - 1]).GetValue(rowKey, colKey);
            }

            // the key is somewhere in the middle, search for the right breakpoint
            // assume the correct breakpoint has not changed since last frame or
            // has only changed very little

            if (r > 2 && data[r - 1, 1] > tableKey)
            {
                while (data[r - 1, 1] > tableKey && r > 2) { r--; }
            }
            else if (data[r, 1] < tableKey)
            {
                while (data[r, 1] <= tableKey && r <= nRows) { r++; }
            }

            lastRowIndex = r;
            // make sure denominator below does not go to zero.

            Span = data[r, 1] - data[r - 1, 1];
            if (Span != 0.0)
            {
                Factor = (tableKey - data[r - 1, 1]) / Span;
                if (Factor > 1.0) Factor = 1.0;
            }
            else
            {
                Factor = 1.0;
            }

            Value = Factor * (((Table)tables[r - 1]).GetValue(rowKey, colKey) -
                ((Table)tables[r - 2]).GetValue(rowKey, colKey))
                + ((Table)tables[r - 1]).GetValue(rowKey, colKey);

            return Value;
        }

        public double GetElement(int r, int c) { return data[r, c]; }
        public void SetRowIndexProperty(PropertyNode node)
        { lookupProperty[(int)AxisType.Row] = new PropertyValue(node); }
        public void SetColumnIndexProperty(PropertyNode node)
        { lookupProperty[(int)AxisType.Column] = new PropertyValue(node); }

        public int GetNumRows() { return nRows; }


        private string Print()
        {
            StringBuilder buff = new StringBuilder();
            int startRow = 0;
            int startCol = 0;

            if (tableType == TableType.Table1D || tableType == TableType.Table3D) startRow = 1;
            if (tableType == TableType.Table3D) startCol = 1;

            buff.Append("\n");

            switch (tableType)
            {
                case TableType.Table1D:
                    buff.Append("    1 dimensional table with " + nRows + " rows.\n");
                    break;
                case TableType.Table2D:
                    buff.Append("    2 dimensional table with " + nRows + " rows, " + nCols + " columns.\n");
                    break;
                case TableType.Table3D:
                    buff.Append("    3 dimensional table with " + nRows + " rows, "
                                                        + nCols + " columns "
                                                        + nTables + " tables.\n");
                    break;
            }

            for (int r = startRow; r <= nRows; r++)
            {
                buff.Append("	");
                for (int c = startCol; c <= nCols; c++)
                {
                    if (r == 0 && c == 0)
                    {
                        log.Debug(" ");
                    }
                    else
                    {
                        buff.Append(data[r, c].ToString("F4", FormatHelper.numberFormatInfo) + "\t");
                        if (tableType == TableType.Table3D)
                        {
                            buff.Append("\n");
                            ((Table)tables[r - 1]).Print();
                        }
                    }
                }
                buff.Append("\n");
            }
            return buff.ToString();
        }

        public string GetName()
        {
            throw new NotImplementedException();
        }
        public bool IsConstant() { return false; }

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
            int startRow = 0;
            int startCol = 0;
            int tableCtr = 0;

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
                log.Debug(Print());
            }
        }
        private void Bind(XmlElement el)
        {

            // typedef double(FGTable::* PMF)(void) const;
            if (!string.IsNullOrEmpty(name) && !isInternal)
            {
                string tmp = null;
                if (string.IsNullOrEmpty(prefix))
                    tmp = PropertyManager.MakePropertyName(name, false); // Allow upper
                else
                {
                    double n;
                    if (double.TryParse(prefix, out n))
                    {
                        if (name.Contains("#"))
                        { // if "#" is found
                            name = name.Replace("#", prefix);
                            tmp = PropertyManager.MakePropertyName(name, false); // Allow upper
                        }
                        else
                        {
                            if (log.IsErrorEnabled)
                                log.Error("Malformed table name with number: " + prefix
                                        + " and property name: " + name
                                        + " but no \"#\" sign for substitution.");
                        }
                    }
                    else
                    {
                        tmp = PropertyManager.MakePropertyName(prefix + "/" + name, false);
                    }
                }

                if (propertyManager.HasNode(tmp))
                {
                    PropertyNode _property = propertyManager.GetNode(tmp);
                    if (_property.IsTied())
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Property " + tmp + " has already been successfully bound (late).");
                        throw new Exception("Failed to bind the property to an existing already tied node.");
                    }
                }
                propertyManager.Tie(tmp, this.GetValue, null);
            }
        }
        public enum TableType { Table1D, Table2D, Table3D };
        private enum AxisType { Row = 0, Column, Table };

        private TableType tableType;
        private double[,] data;

        private bool isInternal = false;
        private List<Table> tables = new List<Table>();
        private int nRows, nCols, nTables, dimension;
        private int colCounter, rowCounter, tableCounter;
        private int lastRowIndex, lastColumnIndex, lastTableIndex;
        private PropertyManager propertyManager;
        private PropertyValue[] lookupProperty = new PropertyValue[3];
        private string prefix;
        private string name;

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
