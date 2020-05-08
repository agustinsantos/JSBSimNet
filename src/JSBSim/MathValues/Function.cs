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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Xml;
    using CommonUtils.MathLib;
    using JSBSim.Format;
    using JSBSim.InputOutput;
     // Import log4net classes.
    using log4net;

    /// <summary>
    ///Represents a mathematical function.
    /// The FGFunction class is a powerful and versatile resource that allows
    /// algebraic functions to be defined in a JSBSim configuration file.It is
    /// similar in concept to MathML (Mathematical Markup Language, www.w3.org/Math/),
    /// but simpler and more terse.
    /// A function definition consists of an operation, a value, a table, or a property
    /// (which evaluates to a value). The currently supported operations are:
    /// - sum(takes n args)
    /// - difference(takes n args)
    /// - product(takes n args)
    /// - quotient(takes 2 args)
    /// - pow(takes 2 args)
    /// - sqrt(takes one argument)
    /// - toradians(takes one argument)
    /// - todegrees(takes one argument)
    /// - exp(takes 2 args)
    /// - log2(takes 1 arg)
    /// - ln(takes 1 arg)
    /// - log10(takes 1 arg)
    /// - abs(takes 1 arg)
    /// - sin(takes 1 arg)
    /// - cos(takes 1 arg)
    /// - tan(takes 1 arg)
    /// - asin(takes 1 arg)
    /// - acos(takes 1 arg)
    /// - atan(takes 1 arg)
    /// - atan2(takes 2 args)
    /// - min(takes n args)
    /// - max(takes n args)
    /// - avg(takes n args)
    /// - fraction
    /// - mod
    /// - floor(takes 1 arg)
    /// - ceil(takes 1 arg)
    /// - fmod(takes 2 args)
    /// - lt(less than, takes 2 args)
    /// - le(less equal, takes 2 args)
    /// - gt(greater than, takes 2 args)
    /// - ge(greater than, takes 2 args)
    /// - eq(equal, takes 2 args)
    /// - nq(not equal, takes 2 args)
    /// - and(takes n args)
    /// - or(takes n args)
    /// - not(takes 1 args)
    /// - if-then(takes 2-3 args)
    /// - switch (takes 2 or more args)
    /// - random(Gaussian distribution random number)
    /// - urandom(Uniform random number between -1 and +1)
    /// - pi
    /// - integer
    /// - interpolate 1-dimensional(takes a minimum of five arguments, odd number)
    /// 
    /// An operation is defined in the configuration file as in the following example:
    /// 
    /// @code
    ///   <sum>
    ///     <value> 3.14159 </value>
    ///     <property> velocities/qbar</property>
    ///     <product>
    ///       <value> 0.125 </value>
    ///       <property> metrics/wingarea</property>
    ///     </product>
    ///   </sum>
    /// @endcode
    /// 
    /// A full function definition, such as is used in the aerodynamics section of a
    /// configuration file includes the function element, and other elements.It should
    /// be noted that there can be only one non-optional(non-documentation) element -
    /// that is, one operation element - in the top-level function definition.
    /// Multiple value and/or property elements cannot be immediate child
    /// members of the function element.Almost always, the first operation within the
    /// function element will be a product or sum.For example:

    /// @code
    /// <function name="aero/moment/Clr">
    ///     <description>Roll moment due to yaw rate</description>
    ///     <product>
    ///         <property>aero/qbar-area</property>
    ///         <property>metrics/bw-ft</property>
    ///         <property>aero/bi2vel</property>
    ///         <property>velocities/r-aero-rad_sec</property>
    ///         <table>
    ///             <independentVar>aero/alpha-rad</independentVar>
    ///             <tableData>
    ///                  0.000  0.08
    ///                  0.094  0.19
    ///             </tableData>
    ///         </table>
    ///     </product>
    /// </function>
    ///  @endcode
    /// 
    /// The "lowest level" in a function is always a value or a property, which cannot
    /// itself contain another element. As shown, operations can contain values,
    /// properties, tables, or other operations.In the first above example, the sum
    /// element contains all three. What is evaluated is written algebraically as:
    /// 
    /// @code 3.14159 + qbar + (0.125 * wingarea) @endcode
    /// 
    ///    Some operations can take only a single argument.That argument, however, can be
    /// an operation(such as sum) which can contain other items.The point to keep in
    /// mind is that it evaluates to a single value - which is just what the
    /// trigonometric functions require(except atan2, which takes two arguments).
    /// 
    /// <h2>Specific Function Definitions</h2>
    /// 
    /// Note: In the definitions below, a "property" refers to a single property
    ///  specified within either the<property></property> tag or the shortcut tag,
    /// \<p>\</p>. The keyword "value" refers to a single numeric value specified either
    /// within the \<value>\</value> tag or the shortcut<v></v> tag.The keyword
    /// "table" refers to a single table specified either within the \<table>\</table>
    /// tag or the shortcut <t></t> tag.The plural form of any of the three words
    /// refers to one or more instances of a property, value, or table.
    /// 
    /// - @b sum, sums the values of all immediate child elements:
    ///     @code
    ///     <sum>
    ///       { properties, values, tables, or other function elements}
    ///     </sum>
    /// 
    ///     Example: Mach + 0.01
    /// 
    ///     <sum>
    ///       <p> velocities/mach</p>
    ///       <v> 0.01 </v>
    ///     </sum>
    ///     @endcode
    /// - @b difference, subtracts the values of all immediate child elements from the
    /// value of the first child element:
    /// @code
    /// <difference>
    ///    {properties, values, tables, or other function elements}
    /// </difference>
    /// 
    /// Example: Mach - 0.01
    /// 
    ///     <difference>
    ///       <p> velocities/mach</p>
    ///       <v> 0.01 </v>
    ///     </difference>
    ///     @endcode
    /// - @b product multiplies together the values of all immediate child elements:
    ///     @code
    ///     <product>
    ///       {properties, values, tables, or other function elements}
    ///     </product>
    /// 
    /// Example: qbar* S* beta* CY_beta
    /// 
    ///       <product>
    ///         <property> aero/qbar-psf</property>
    ///         <property> metrics/Sw-sqft</property>
    ///         <property> aero/beta-rad</property>
    ///         <property> aero/coefficient/CY_beta</property>
    ///       </product>
    /// 
    ///       @endcode
    ///   - @b quotient, divides the value of the first immediate child element by the
    ///     second immediate child element:
    /// 
    ///       @code
    ///       <quotient>
    ///         { property, value, table, or other function element}
    ///       {property, value, table, or other function element}
    ///     </quotient>
    /// 
    ///     Example: (2* GM)/R
    /// 
    ///      <quotient>
    ///       <product>
    ///         <v> 2.0 </v>
    ///         <p> guidance/executive/gm</p>
    ///       </product>
    ///       <p> position/radius-to-vehicle-ft</p>
    ///     </quotient>
    ///     @endcode
    /// - @b pow, raises the value of the first immediate child element to the power of
    ///     the value of the second immediate child element:
    ///     @code
    ///     <pow>
    ///       {property, value, table, or other function element}
    ///       {property, value, table, or other function element}
    ///     </pow>
    /// 
    ///    Example: Mach^2
    /// 
    ///     <pow>
    ///       <p> velocities/mach</p>
    ///       <v> 2.0 </v>
    ///    </pow>
    ///     @endcode
    /// - @b sqrt, takes the square root of the value of the immediate child element:
    ///     @code
    ///     <sqrt>
    ///       {property, value, table, or other function element}
    ///     </sqrt>
    /// 
    ///     Example: square root of 25
    /// 
    ///     <sqrt> <v> 25.0 </v> </sqrt>
    ///     @endcode
    /// - @b toradians, converts a presumed argument in degrees to radians by
    ///     multiplying the value of the immediate child element by pi/180:
    ///     @code
    ///     <toradians>
    ///       {property, value, table, or other function element}
    ///     </toradians>
    /// 
    ///     Example: convert 45 degrees to radians
    /// 
    ///     <toradians> <v> 45 </v> </toradians>
    ///     @endcode
    /// - @b todegrees, converts a presumed argument in radians to degrees by
    ///     multiplying the value of the immediate child element by 180/pi:
    ///    @code
    ///     <todegrees>
    ///       {property, value, table, or other function element}
    ///     </todegrees>
    /// 
    ///     Example: convert 0.5* pi radians to degrees
    /// 
    ///      <todegrees>
    ///       <product> <v> 0.5 </v> <pi/> </product>
    ///     </todegrees>
    ///     @endcode
    /// - @b exp, raises "e" to the power of the immediate child element:
    ///     @code
    ///     <exp>
    ///       {property, value, table, or other function element}
    ///     </exp>
    /// 
    ///     Example: raise "e" to the 1.5 power, e^1.5
    /// 
    ///     <exp> <v> 1.5 </v> </exp>
    ///     @endcode
    /// - @b log2, calculates the log base 2 value of the immediate child element:
    ///     @code
    ///     <log2>
    ///       {property, value, table, or other function element} 
    ///     </log2>
    /// 
    ///     Example:
    ///     <log2> <v> 128 </v> </log2>
    ///     @endcode
    /// - @b ln, calculates the natural logarithm of the value of the immediate child
    ///          element:
    ///     @code
    ///     <ln>
    ///       {property, value, table, or other function element}
    ///     </ln>
    ///     
    ///     Example: ln(128)
    /// 
    ///     <ln> <v> 200 </v> </ln>
    ///     @endcode
    /// - @b log10, calculates the base 10 logarithm of the value of the immediate child
    ///     element
    ///     <log10>
    ///       { property, value, table, or other function element }
    ///     </log10>
    /// 
    ///     Example log(Mach)
    /// 
    ///     <log10> <p> velocities/mach</p> </log10>
    ///     @endcode
    /// - @b abs calculates the absolute value of the immediate child element
    ///     @code
    ///     <abs>
    ///       { property, value, table, or other function element }
    ///     </abs>
    /// 
    ///     Example:
    /// 
    ///     <abs> <p> flight-path/gamma-rad</p> </abs>
    ///     @endcode
    /// - @b sin, calculates the sine of the value of the immediate child element(the
    ///           argument is expected to be in radians)
    ///     @code
    ///     <sin>
    ///      {property, value, table, or other function element}
    ///     </sin>
    /// 
    ///     Example:
    /// 
    ///     <sin> <toradians> <p> fcs/heading-true-degrees</p> </toradians> </sin>
    ///     @endcode
    /// - @b cos, calculates the cosine of the value of the immediate child element(the
    ///           argument is expected to be in radians)
    ///     @code
    ///     <cos>
    ///       {property, value, table, or other function element}
    ///     </cos>
    /// 
    ///     Example:
    /// 
    ///     <cos> <toradians> <p> fcs/heading-true-degrees</p> </toradians> </cos>
    ///     @endcode
    /// - @b tan, calculates the tangent of the value of the immediate child element
    ///           (the argument is expected to be in radians)
    ///     @code
    ///     <tan>
    ///       {property, value, table, or other function element}
    ///     </tan>
    /// 
    ///     Example:
    /// 
    ///     <tan> <toradians> <p> fcs/heading-true-degrees</p> </toradians> </tan>
    ///     @endcode
    /// - @b asin, calculates the arcsine(inverse sine) of the value of the immediate
    ///     child element.The value provided should be in the range from -1 to
    ///            +1. The value returned will be expressed in radians, and will be in
    ///     the range from -pi/2 to +pi/2.
    ///     @code
    ///     <asin>
    ///       { property, value, table, or other function element}
    ///     </asin>
    /// 
    ///     Example:
    /// 
    ///     <asin> <v> 0.5 </v> </asin>
    ///     @endcode
    /// - @b acos, calculates the arccosine(inverse cosine) of the value of the
    ///     immediate child element.The value provided should be in the range
    /// 
    ///           from -1 to +1. The value returned will be expressed in radians, and
    ///     will be in the range from 0 to pi.
    /// 
    ///    @code
    ///    <acos>
    ///       { property, value, table, or other function element}
    ///     </acos>
    /// 
    ///     Example:
    /// 
    ///     <acos> <v> 0.5 </v> </acos>
    ///     @endcode
    /// - @b atan, calculates the inverse tangent of the value of the immediate child
    /// element.The value returned will be expressed in radians, and will
    ///            be in the range from -pi/2 to +pi/2.
    ///     @code
    ///     <atan>
    ///       {property, value, table, or other function element}
    ///     </atan>
    /// 
    ///     Example:
    /// 
    ///     <atan> <v> 0.5 </v> </atan>
    ///     @endcode
    /// - @b atan2 calculates the inverse tangent of the value of the immediate child
    /// elements, Y/X(in that order). It even works for X values near zero.
    /// The value returned will be expressed in radians, and in the range
    /// -pi to +pi.
    /// @code
    /// <atan2>
    ///       { property, value, table, or other function element} {property, value, table, or other function element}
    ///     </atan2>
    /// 
    ///     Example: inverse tangent of 0.5/0.25, evaluates to: 1.107 radians
    /// 
    ///     <atan2> <v> 0.5 </<v> <v> 0.25 </v> </atan2>
    ///     @endcode
    /// - @b min returns the smallest value from all the immediate child elements
    ///     @code
    ///     <min>
    ///       {properties, values, tables, or other function elements}
    ///     </min>
    ///     
    ///     Example: returns the lesser of velocity and 2500
    /// 
    ///     <min>
    ///       <p> velocities/eci-velocity-mag-fps</p>
    ///       <v> 2500.0 </v>
    ///     </min>
    ///     @endcode
    /// - @b max returns the largest value from all the immediate child elements
    ///     @code
    ///     <max>
    ///       {properties, values, tables, or other function elements}
    ///     </max>
    ///     
    ///     Example: returns the greater of velocity and 15000
    /// 
    ///     <max>
    ///       <p> velocities/eci-velocity-mag-fps</p>
    ///       <v> 15000.0 </v>
    ///     </max>
    ///     @endcode
    /// - @b avg returns the average value of all the immediate child elements
    ///     @code
    ///     <avg>
    ///       {properties, values, tables, or other function elements} 
    ///     </avg>
    /// 
    ///     Example: returns the average of the four numbers below, evaluates to 0.50.
    /// 
    ///     <avg>
    ///       <v> 0.25 </v>
    ///       <v> 0.50 </v>
    ///       <v> 0.75 </v>
    ///       <v> 0.50 </v>
    ///     </avg>
    ///     @endcode
    /// - @b fraction, returns the fractional part of the value of the immediate child
    ///                element
    ///     @code
    ///     <fraction>
    ///       { property, value, table, or other function element }
    ///     </fraction>
    /// 
    ///     Example: returns the fractional part of pi - or, roughly, 0.1415926...
    /// 
    ///     <fraction> <pi/> </fraction>
    ///     @endcode
    /// - @b integer, returns the integer portion of the value of the immediate child
    ///      element
    ///     @code
    ///     <integer>
    ///       { property, value, table, or other function element }
    ///     </integer>
    ///     @endcode
    /// - @b mod returns the remainder from the integer division of the value of the
    ///          first immediate child element by the second immediate child element,
    ///          X/Y(X modulo Y). The value returned is the value X-I* Y, for the
    ///          largest  integer I such that if Y is nonzero, the result has the
    ///          same sign as X and magnitude less than the magnitude of Y.For
    ///          instance, the expression "5 mod 2" would evaluate to 1 because 5
    ///          divided by 2 leaves a quotient of 2 and a remainder of 1, while
    ///          "9 mod 3" would evaluate to 0 because the division of 9 by 3 has a
    ///          quotient of 3 and leaves a remainder of 0.
    ///     @code
    ///     <mod>
    ///       {property, value, table, or other function element}
    ///       {property, value, table, or other function element}
    ///     </mod>
    /// 
    ///     Example: 5 mod 2, evaluates to 1
    /// 
    ///     <mod> <v> 5 </v> <v> 2 </v> </mod>
    ///     @endcode
    /// - @b floor returns the largest integral value that is not greater than X.
    ///     @code
    ///     <floor>
    ///       { property, value, table, or other function element }
    ///     </floor>
    ///     @endcode
    ///     Examples: floor(2.3) evaluates to 2.0 while floor(-2.3) evaluates to -3.0
    /// - @b ceil returns the smallest integral value that is not less than X.
    ///     @code
    ///     <ceil>
    ///       { property, value, table, or other function element }
    ///     </ceil>
    ///     @endcode
    ///     Examples: ceil(2.3) evaluates to 3.0 while ceil(-2.3) evaluates to -2.0
    /// - @b fmod returns the floating-point remainder of X/Y(rounded towards zero)
    ///     @code
    ///     <fmod>
    ///       {property, value, table, or other function element}
    ///       {property, value, table, or other function element}
    ///     </fmod>
    ///     @endcode
    ///     Example: fmod(18.5, 4.2) evaluates to 1.7
    /// - @b lt returns a 1 if the value of the first immediate child element is less
    ///         than the value of the second immediate child element, returns 0
    ///         otherwise
    ///     @code
    ///     <lt>
    ///       { property, value, table, or other function element }
    ///       {property, value, table, or other function element}
    ///     </lt>
    /// 
    ///     Example: returns 1 if thrust is less than 10,000, returns 0 otherwise
    /// 
    ///     <lt>
    ///       <p> propulsion/engine[2]/thrust-lbs</p>
    ///       <v> 10000.0 </v>
    ///     </lt>
    ///     @endcode
    /// - @b le, returns a 1 if the value of the first immediate child element is less
    ///          than or equal to the value of the second immediate child element,
    ///          returns 0 otherwise
    ///     @code
    ///     <le>
    ///       { property, value, table, or other function element }
    ///       {property, value, table, or other function element}
    ///     </le>
    /// 
    ///     Example: returns 1 if thrust is less than or equal to 10,000, returns 0 otherwise
    /// 
    ///     <le>
    ///       <p> propulsion/engine[2]/thrust-lbs</p>
    ///       <v> 10000.0 </v>
    ///     </le>
    ///     @endcode
    /// - @b gt returns a 1 if the value of the first immediate child element is greater
    ///         than the value of the second immediate child element, returns 0
    ///         otherwise
    ///     @code
    ///     <gt>
    ///       { property, value, table, or other function element }
    ///       {property, value, table, or other function element}
    ///     </gt>
    /// 
    ///     Example: returns 1 if thrust is greater than 10,000, returns 0 otherwise
    /// 
    ///     <gt>
    ///       <p> propulsion/engine[2]/thrust-lbs</p>
    ///       <v> 10000.0 </v>
    ///     </gt>
    ///     @endcode
    /// - @b ge, returns a 1 if the value of the first immediate child element is
    ///          greater than or equal to the value of the second immediate child
    ///          element, returns 0 otherwise
    ///     @code
    ///     <ge>
    ///       { property, value, table, or other function element }
    ///       { property, value, table, or other function element}
    ///     </ge>
    /// 
    ///     Example: returns 1 if thrust is greater than or equal to 10,000, returns 0
    ///     otherwise
    /// 
    ///     <ge>
    ///       <p> propulsion/engine[2]/thrust-lbs</p>
    ///       <v> 10000.0 </v>
    ///     </ge>
    ///     @endcode
    /// - @b eq returns a 1 if the value of the first immediate child element is 
    ///         equal to the second immediate child element, returns 0
    ///         otherwise
    ///     @code
    ///     <eq>
    ///       { property, value, table, or other function element }
    ///       {property, value, table, or other function element}
    ///     </eq>
    /// 
    ///     Example: returns 1 if thrust is equal to 10,000, returns 0 otherwise
    /// 
    ///     <eq>
    ///       <p> propulsion/engine[2]/thrust-lbs</p>
    ///       <v> 10000.0 </v>
    ///     </eq>
    ///     @endcode
    /// - @b nq returns a 1 if the value of the first immediate child element is not
    ///         equal to the value of the second immediate child element, returns 0
    ///         otherwise
    ///     @code
    ///     <nq>
    ///       { property, value, table, or other function element }
    ///       {property, value, table, or other function element}
    ///     </nq>
    /// 
    ///     Example: returns 1 if thrust is not 0, returns 0 otherwise
    /// 
    ///     <nq>
    ///       <p> propulsion/engine[2]/thrust-lbs</p>
    ///       <v> 0.0 </v>
    ///     </nq>
    ///     @endcode
    /// - @b and returns a 1 if the values of the immediate child elements are all 1,
    ///          returns 0 otherwise.Values provided are expected to be either 1 or 0
    ///          within machine precision.
    ///     @code
    ///     <and>
    ///       {properties, values, tables, or other function elements}
    ///     </and>
    /// 
    ///     Example: returns 1 if the specified flags are all 1
    /// 
    ///     <and>
    ///       <p> guidance/first-stage-flight-flag</p>
    ///       <p> control/engines-running-flag</p>
    ///     </and>
    ///     @endcode
    /// - @b or returns a 1 if the values of any of the immediate child elements 1,
    ///          returns 0 otherwise.Values provided are expected to be either 1 or 0
    ///          within machine precision.
    ///     @code
    ///     <or>
    ///       {properties, values, tables, or other function elements}
    ///     </or>
    /// 
    ///     Example: returns 1 if any of the specified flags are 1
    /// 
    ///     <or>
    ///       <p> guidance/first-stage-flight-flag</p>
    ///       <p> control/engines-running-flag</p>
    ///     </or>
    ///     @endcode
    /// - @b not, returns the inverse of the value of the supplied immediate child
    ///           element(e.g., returns 1 if supplied a 0)
    ///     @code
    ///     <not>
    ///       {property, value, table, or other function element} 
    ///     </not>
    /// 
    ///     Example: returns 0 if the value of the supplied flag is 1
    /// 
    ///     <not> <p> guidance/first-stage-flight-flag</p> </not>
    ///     @endcode
    /// - @b ifthen if the value of the first immediate child element is 1, then the
    ///              value of the second immediate child element is returned, otherwise
    ///              the value of the third child element is returned
    ///      @code
    ///      <ifthen>
    ///        { property, value, table, or other function element }
    ///       {property, value, table, or other function element}
    ///        {property, value, table, or other function element}
    ///      </ifthen>
    /// 
    ///      Example: if flight-mode is greater than 2, then a value of 0.00 is
    ///               returned, otherwise the value of the property control/pitch-lag is
    ///               returned.
    /// 
    ///      <ifthen>
    ///        <gt> <p> executive/flight-mode</p> <v> 2 </v> </gt>
    ///        <v> 0.00 </v>
    ///        <p> control/pitch-lag</p>
    ///      </ifthen>
    ///      @endcode
    /// - @b switch uses the integer value of the first immediate child element as an
    ///             index to select one of the subsequent immediate child elements to
    ///             return the value of
    ///      @code
    ///      <switch>
    ///        { property, value, table, or other function element }
    ///        {property, value, table, or other function element}
    ///        {property, value, table, or other function element}
    ///        ...
    ///      </switch>
    /// 
    ///      Example: if flight-mode is 2, the switch function returns 0.50
    ///      <switch>
    ///        <p> executive/flight-mode</p>
    ///        <v> 0.25 </v>
    ///        <v> 0.50 </v>
    ///        <v> 0.75 </v>
    ///        <v> 1.00 </v>
    ///      </switch>
    ///      @endcode
    /// - @b random Returns a normal distributed random number.
    ///             The function, without parameters, returns a normal distributed
    ///             random value with a distribution defined by the parameters
    ///             mean = 0.0 and standard deviation (stddev) = 1.0
    ///             The Mean of the distribution (its expected value, μ).
    ///             Which coincides with the location of its peak.
    ///             Standard deviation (σ): The square root of variance,
    ///             representing the dispersion of values from the distribution mean.
    ///             This shall be a positive value (σ>0).
    ///     @code
    ///     <random/> 
    ///     <random seed = "1234" />
    ///     < random seed="time_now"/>
    ///     <random seed = "time_now" mean="0.0" stddev="1.0"/>
    ///     @endcode
    /// - @b urandom Returns a uniformly distributed random number.
    ///              The function, without parameters, returns a random value
    ///              between the minimum value -1.0 and the maximum value of 1.0
    ///              The two maximum and minimum values can be modified using the 
    ///              lower and upper parameters.
    ///     @code
    ///     <urandom/>
    ///     <random seed = "1234" />
    ///     < random seed= "time_now" />
    ///     < random seed= "time_now" lower= "-1.0" upper= "1.0" />
    ///     @endcode
    /// - @b pi Takes no argument and returns the value of Pi
    ///     @code<pi/> @endcode
    /// - @b interpolate1d returns the result from a 1-dimensional interpolation of the
    ///                  supplied values, with the value of the first immediate child
    ///                  element representing the lookup value into the table, and the
    ///                   following pairs of values representing the independent and
    ///                   dependent values. The first provided child element is
    ///                   expected to be a property.The interpolation does not
    ///                   extrapolate, but holds the highest value if the provided
    ///                    lookup value goes outside of the provided range.
    ///      @code
    ///     <interpolate1d>
    ///       { property, value, table, or other function element}
    ///       {property, value, table, or other function element} {property, value, table, or other function element}
    ///       ...
    ///     </interpolate1d>
    /// 
    ///     Example: If mach is 0.4, the interpolation will return 0.375. If mach is
    ///              1.5, the interpolation will return 0.60.
    /// 
    ///  <interpolate1d>
    ///    <p> velocities/mach</p>
    ///    <v> 0.00 </v>  <v> 0.25 </v>
    ///  <v> 0.80 </v>  <v> 0.50 </v>
    ///  <v> 0.90 </v>  <v> 0.60 </v>
    /// </interpolate1d>
    /// @endcode
    /// </summary>
    public class Function : IParameter
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

        /// <summary>
        ///  Default constructor.
        /// </summary>
        public Function()
        {
            cached = false; cachedValue = double.NegativeInfinity; propertyManager = null;
            pCopyTo = null;
        }
        public Function(PropertyManager pm)
        {
            propertyManager = pm;
        }
        public Function(FDMExecutive fdmex, XmlElement el, string strPrefix = "", PropertyValue var = null)
            : this(fdmex.PropertyManager)
        {
            Load(el, var, fdmex, prefix);
            CheckMinArguments(el, 1);
            CheckMaxArguments(el, 1);

            string sCopyTo = el.GetAttribute("copyto");

            if (!string.IsNullOrEmpty(sCopyTo))
            {
                if (sCopyTo.Contains("#"))
                {
                    double tmp;
                    if (double.TryParse(prefix, out tmp))
                        sCopyTo = sCopyTo.Replace("#", prefix);
                    else
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Illegal use of the special character '#'" +
                                       "The 'copyto' argument in function " + Name + " is ignored.");
                        return;
                    }
                }

                pCopyTo = propertyManager.GetNode(sCopyTo);
                if (pCopyTo == null)
                    if (log.IsErrorEnabled)
                        log.Error("Property \"" + sCopyTo +
                                  "\" must be previously defined in function " + Name +
                                  "The 'copyto' argument is ignored.");
            }
        }

        ///// <summary>
        ///// Constructor.
        ///// When this constructor is called, the XML element pointed to in memory by the
        ///// element argument is traversed.If other FGParameter-derived objects (values,
        ///// functions, properties, or tables) are encountered, this instance of the
        ///// FGFunction object will store a pointer to the found object and pass the
        ///// 
        ///// relevant Element pointer to the constructor for the new object. In other
        ///// words, each FGFunction object maintains a list of "child"
        ///// FGParameter-derived objects which in turn may each contain its own list, and
        ///// so on.At runtime, each object evaluates its child parameters, which each
        ///// may have its own child parameters to evaluate.
        ///// 
        ///// </summary>
        ///// <param name="propMan">a pointer to the property manager instance.</param>
        ///// <param name="element">a pointer to the Element object containing the function
        /////  definition.</param>
        ///// <param name="strPrefix">an optional prefix to prepend to the name given to the
        /////  property that represents this function (if given).</param>
        ///// <param name="var"></param>
        //public Function(PropertyManager pm, XmlElement element, string strPrefix = "",
        //                PropertyValue var = null) : this(pm)
        //{
        //    //if (log.IsDebugEnabled)
        //    //    log.Debug("In Function.Ctor");
        //    prefix = strPrefix;

        //    name = element.GetAttribute("name");
        //    string operation = element.LocalName;
        //    if (operation.Equals("function"))
        //    {
        //        functionType = FunctionType.TopLevel;
        //        Bind();
        //    }
        //    else if (operation.Equals("product"))
        //        functionType = FunctionType.Product;
        //    else if (operation.Equals("product"))
        //        functionType = FunctionType.TopLevel;
        //    else if (operation.Equals("difference"))
        //        functionType = FunctionType.Difference;
        //    else if (operation.Equals("sum"))
        //        functionType = FunctionType.Sum;
        //    else if (operation.Equals("quotient"))
        //        functionType = FunctionType.Quotient;
        //    else if (operation.Equals("pow"))
        //        functionType = FunctionType.Pow;
        //    else if (operation.Equals("abs"))
        //        functionType = FunctionType.Abs;
        //    else if (operation.Equals("sin"))
        //        functionType = FunctionType.Sin;
        //    else if (operation.Equals("cos"))
        //        functionType = FunctionType.Cos;
        //    else if (operation.Equals("tan"))
        //        functionType = FunctionType.Tan;
        //    else if (operation.Equals("asin"))
        //        functionType = FunctionType.ASin;
        //    else if (operation.Equals("acos"))
        //        functionType = FunctionType.ACos;
        //    else if (operation.Equals("atan"))
        //        functionType = FunctionType.ATan;
        //    else if (operation.Equals("atan2"))
        //        functionType = FunctionType.ATan2;
        //    else if (!operation.Equals("description"))
        //    {
        //        log.Error("Bad operation <" + operation + "> detected in configuration file");
        //    }

        //    foreach (XmlNode currentNode in element.ChildNodes)
        //    {
        //        if (currentNode.NodeType == XmlNodeType.Element)
        //        {
        //            XmlElement currentElement = (XmlElement)currentNode;

        //            operation = currentElement.LocalName;
        //            //if (log.IsDebugEnabled)
        //            //    log.Debug("In Function.Ctor, Procesing tag=" + operation);

        //            if (operation.Equals("property"))
        //            {
        //                string property_name = currentElement.InnerText;
        //                parameters.Add(new PropertyValue(propertyManager.GetPropertyNode(property_name)));
        //            }
        //            else if (operation.Equals("value"))
        //            {
        //                parameters.Add(new RealValue(FormatHelper.ValueAsNumber(currentElement)));
        //            }
        //            else if (operation.Equals("table"))
        //            {
        //                parameters.Add(new Table(propertyManager, currentElement));
        //                // operations
        //            }
        //            else if (operation.Equals("product") ||
        //                operation.Equals("difference") ||
        //                operation.Equals("sum") ||
        //                operation.Equals("quotient") ||
        //                operation.Equals("pow") ||
        //                operation.Equals("abs") ||
        //                operation.Equals("sin") ||
        //                operation.Equals("cos") ||
        //                operation.Equals("tan") ||
        //                operation.Equals("asin") ||
        //                operation.Equals("acos") ||
        //                operation.Equals("atan") ||
        //                operation.Equals("atan2"))
        //            {
        //                //TODO parameters.Add(new Function(propertyManager, currentElement));
        //            }
        //            else if (!operation.Equals("description"))
        //            {
        //                log.Error("Bad operation <" + operation + "> detected in configuration file");
        //            }
        //        }
        //    }
        //    //Bind();
        //}

        /// <summary>
        /// Retrieves the value of the function object.
        /// </summary>
        public double Value
        {
            get { return GetValue(); }
        }

        /// <summary>
        /// Retrieves the value of the function object.
        /// </summary>
        /// <returns>the total value of the function.</returns>
        public virtual double GetValue()
        {
            if (cached) return cachedValue;
            double val = parameters[0].GetValue();

            if (pCopyTo != null) pCopyTo.Set(val);

            return val;
        }

        /// <summary>
        /// The value that the function evaluates to, as a string.
        /// </summary>
        /// <returns>the value of the function as a string.</returns>
        public string GetValueAsString()
        {
            return GetValue().ToString("f9.6", FormatHelper.numberFormatInfo); //TODO, test this format with culture != US
        }

        /// <summary>
        /// Retrieves the name of the function.
        /// </summary>
        public string GetName() { return Name; }

        /// <summary>
        /// Retrieves the name of the function.
        /// </summary>
        public string Name { get { return name; } }

        /// <summary>
        /// Does the function always return the same result (i.e. does it apply to
        /// constant parameters) ?
        /// </summary>
        /// <returns></returns>
        public bool IsConstant()
        {
            foreach (var p in parameters)
            {
                if (!p.IsConstant())
                    return false;
            }

            return true;
        }



        /// <summary>
        /// Specifies whether to cache the value of the function, so it is calculated
        /// only once per frame.
        ///  If shouldCache is true, then the value of the function is calculated, and a
        /// flag is set so further calculations done this frame will use the cached
        ///  value.  In order to turn off caching, cacheValue must be called with a false
        ///  argument.
        /// </summary>
        /// <param name="shouldCache">specifies whether the function should cache the computed
        /// value.</param>
        public void CacheValue(bool mustCache)
        {
            cached = false; // Must set cached to false prior to calling GetValue(), else
                            // it will _never_ calculate the value;
            if (mustCache)
            {
                cachedValue = GetValue();
                cached = true;
            }
        }

        public enum OddEven { Either, Odd, Even };

        //%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
        protected void Load(XmlElement el, PropertyValue var, FDMExecutive fdmex, string prefix = "")
        {
            name = el.GetAttribute("name");
            foreach (XmlNode node in el.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                {
                    XmlElement element = node as XmlElement;

                    string operation = element.LocalName;
                    // data types
                    if (operation == "property" || operation == "p")
                    {
                        string property_name = element.InnerText;
                        if (var != null && property_name.Trim() == "#")
                            parameters.Add(var);
                        else
                        {
                            if (property_name.Contains("#"))
                            {
                                double tmp;
                                if (double.TryParse(prefix, out tmp))
                                {
                                    property_name = property_name.Replace("#", prefix);
                                }
                                else
                                {
                                    log.Error("Illegal use of the special character '#'");
                                    throw new Exception("Fatal Error.");
                                }
                            }

                            if (element.HasAttribute("apply"))
                            {
                                string function_str = element.GetAttribute("apply");
                                TemplateFunc f = fdmex.GetTemplateFunc(function_str);
                                if (f != null)
                                    parameters.Add(new FunctionValue(property_name,
                                                                     propertyManager, f));
                                else
                                {
                                    log.Error("  No function by the name " +
                                               function_str + " has been defined. This property will " +
                                               "not be logged. You should check your configuration file.");
                                }
                            }
                            else
                                parameters.Add(new PropertyValue(property_name,
                                                                 propertyManager));
                        }
                    }
                    else if (operation == "value" || operation == "v")
                    {
                        parameters.Add(new RealValue(double.Parse(element.InnerText)));
                    }
                    else if (operation == "pi")
                    {
                        parameters.Add(new RealValue(Math.PI));
                    }
                    else if (operation == "table" || operation == "t")
                    {
                        parameters.Add(new Table(propertyManager, element, prefix));
                        // operations
                    }
                    else if (operation == "product")
                    {
                        ParametersFunction mul = (parameters) =>
                        {
                            double temp = 1.0;
                            foreach (var p in parameters)
                                temp *= p.GetValue();

                            return temp;
                        };
                        parameters.Add(new aFunc(mul, fdmex, element, prefix, var));
                    }
                    else if (operation == "sum")
                    {
                        ParametersFunction sum = (parameters) =>
                        {
                            double temp = 0.0;
                            foreach (var p in parameters)
                                temp += p.GetValue();

                            return temp;
                        };
                        parameters.Add(new aFunc(sum, fdmex, element, prefix, var));
                    }
                    else if (operation == "avg")
                    {
                        ParametersFunction avg = (parameters) =>
                        {
                            double temp = 0.0;
                            foreach (var p in parameters)
                                temp += p.GetValue();

                            return temp / parameters.Count;
                        };
                        parameters.Add(new aFunc(avg, fdmex, element, prefix, var));
                    }
                    else if (operation == "difference")
                    {
                        ParametersFunction dif = (parameters) =>
                        {
                            double temp = parameters[0].GetValue();

                            for (int p = 1; p != parameters.Count; ++p)
                                temp -= parameters[p].GetValue();

                            return temp;
                        };
                        parameters.Add(new aFunc(dif, fdmex, element, prefix, var));
                    }
                    else if (operation == "min")
                    {
                        ParametersFunction min = (parameters) =>
                        {
                            double _min = double.MaxValue;

                            foreach (var p in parameters)
                            {
                                double x = p.GetValue();
                                if (x < _min)
                                    _min = x;
                            }

                            return _min;
                        };

                        parameters.Add(new aFunc(min, fdmex, element, prefix, var));
                    }
                    else if (operation == "min")
                    {
                        ParametersFunction max = (parameters) =>
                        {
                            double _max = double.MinValue;

                            foreach (var p in parameters)
                            {
                                double x = p.GetValue();
                                if (x > _max)
                                    _max = x;
                            }

                            return _max;
                        };

                        parameters.Add(new aFunc(max, fdmex, element, prefix, var));
                    }
                    else if (operation == "and")
                    {
                        string ctxMsg = element.InnerText;
                        ParametersFunction and = (parameters) =>
                        {
                            foreach (var p in parameters)
                            {
                                if (!GetBinary(p.GetValue(), ctxMsg)) // As soon as one parameter is false, the expression is guaranteed to be false.
                                    return 0.0;
                            }

                            return 1.0;
                        };
                        parameters.Add(new aFunc(and, fdmex, element, prefix, var, short.MaxValue, 2));
                    }
                    else if (operation == "or")
                    {
                        string ctxMsg = element.InnerText;
                        ParametersFunction or = (parameters) =>
                        {
                            foreach (var p in parameters)
                            {
                                if (!GetBinary(p.GetValue(), ctxMsg)) // As soon as one parameter is true, the expression is guaranteed to be true.
                                    return 1.0;
                            }

                            return 0.0;
                        };
                        parameters.Add(new aFunc(or, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "quotient")
                    {
                        ParametersFunction q = (parameters) =>
                        {
                            double y = parameters[1].GetValue();
                            return y != 0.0 ? parameters[0].GetValue() / y : double.MaxValue;
                        };
                        parameters.Add(new aFunc(q, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "pow")
                    {
                        ParametersFunction p = (parameters) =>
                        {
                            double y = parameters[1].GetValue();
                            return Math.Pow(parameters[0].GetValue(), parameters[1].GetValue());
                        };
                        parameters.Add(new aFunc(p, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "toradians")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return parameters[0].GetValue() * Math.PI / 180.0;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "todegrees")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return parameters[0].GetValue() * 180.0 / Math.PI;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "sqrt")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            double x = parameters[0].GetValue();
                            return x >= 0.0 ? Math.Sqrt(x) : double.MinValue;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "log2")
                    {
                        double invlog2val = 1.0 / Math.Log10(2.0);

                        ParametersFunction f = (parameters) =>
                        {
                            double x = parameters[0].GetValue();
                            return x >= 0.0 ? Math.Log10(x) * invlog2val : double.MinValue;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "ln")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            double x = parameters[0].GetValue();
                            return x >= 0.0 ? Math.Log(x) : double.MinValue;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "log10")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            double x = parameters[0].GetValue();
                            return x >= 0.0 ? Math.Log10(x) : double.MinValue;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "sign")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return parameters[0].GetValue() < 0.0 ? -1 : 1; // 0.0 counts as positive.
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "exp")
                    {
                        ParametersFunction f = (parameters) => Math.Exp(parameters[0].GetValue());
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "abs")
                    {
                        ParametersFunction f = (parameters) => Math.Abs(parameters[0].GetValue());
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "sin")
                    {
                        ParametersFunction f = (parameters) => Math.Sin(parameters[0].GetValue());
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "cos")
                    {
                        ParametersFunction f = (parameters) => Math.Cos(parameters[0].GetValue());
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "tan")
                    {
                        ParametersFunction f = (parameters) => Math.Tan(parameters[0].GetValue());
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "asin")
                    {
                        ParametersFunction f = (parameters) => Math.Asin(parameters[0].GetValue());
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "acos")
                    {
                        ParametersFunction f = (parameters) => Math.Acos(parameters[0].GetValue());
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "atan")
                    {
                        ParametersFunction f = (parameters) => Math.Atan(parameters[0].GetValue());
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "floor")
                    {
                        ParametersFunction f = (parameters) => Math.Floor(parameters[0].GetValue());
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "ceil")
                    {
                        ParametersFunction f = (parameters) => Math.Ceiling(parameters[0].GetValue());
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "fmod")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            double y = parameters[1].GetValue();
                            return y != 0.0 ? parameters[0].GetValue() % y : double.MaxValue;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "atan2")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return Math.Atan2(parameters[0].GetValue(), parameters[1].GetValue());
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "mod")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return (int)parameters[0].GetValue() % (int)parameters[1].GetValue();
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "fraction")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            double val = parameters[0].GetValue();
                            return val - (int)val;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "integer")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            int vint = (int)parameters[0].GetValue();
                            return vint;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "lt")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return parameters[0].GetValue() < parameters[1].GetValue() ? 1.0 : 0.0;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "le")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return parameters[0].GetValue() <= parameters[1].GetValue() ? 1.0 : 0.0;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "gt")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return parameters[0].GetValue() > parameters[1].GetValue() ? 1.0 : 0.0;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "ge")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return parameters[0].GetValue() >= parameters[1].GetValue() ? 1.0 : 0.0;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "eq")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return parameters[0].GetValue() == parameters[1].GetValue() ? 1.0 : 0.0;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "nq")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            return parameters[0].GetValue() != parameters[1].GetValue() ? 1.0 : 0.0;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 2, 2));
                    }
                    else if (operation == "not")
                    {
                        string ctxMsg = element.InnerText;
                        ParametersFunction f = (parameters) =>
                        {
                            return GetBinary(parameters[0].GetValue(), ctxMsg) ? 0.0 : 1.0;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 1, 1));
                    }
                    else if (operation == "ifthen")
                    {
                        string ctxMsg = element.InnerText;
                        ParametersFunction f = (parameters) =>
                        {
                            if (GetBinary(parameters[0].GetValue(), ctxMsg))
                                return parameters[1].GetValue();
                            else
                                return parameters[2].GetValue();
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 3, 3));
                    }
                    else if (operation == "random")
                    {
                        double mean = 0.0;
                        double stddev = 1.0;
                        string mean_attr = element.GetAttribute("mean");
                        string stddev_attr = element.GetAttribute("stddev");
                        if (!string.IsNullOrEmpty(mean_attr))
                            mean = double.Parse(mean_attr, CultureInfo.InvariantCulture);
                        if (!string.IsNullOrEmpty(stddev_attr))
                            stddev = double.Parse(stddev_attr, CultureInfo.InvariantCulture);
                        var generator = MakeRandomEngine(element, fdmex);
                        var distribution = new NormalRandom(mean, stddev, generator);
                        ParametersFunction f = (parameters) =>
                        {
                            return distribution.Next();
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 0, 0));
                    }
                    else if (operation == "urandom")
                    {
                        double lower = -1.0;
                        double upper = 1.0;
                        string lower_attr = element.GetAttribute("lower");
                        string upper_attr = element.GetAttribute("upper");
                        if (!string.IsNullOrEmpty(lower_attr))
                            lower = double.Parse(lower_attr, CultureInfo.InvariantCulture);
                        if (!string.IsNullOrEmpty(upper_attr))
                            upper = double.Parse(upper_attr, CultureInfo.InvariantCulture);
                        var generator = MakeRandomEngine(element, fdmex);
                        var distribution = new UniformRandom(lower, upper, generator);
                        ParametersFunction f = (parameters) =>
                        {
                            return distribution.Next();
                        };

                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 0, 0));
                    }
                    else if (operation == "switch")
                    {
                        string ctxMsg = element.InnerText;
                        ParametersFunction f = (parameters) =>
                        {
                            double temp = parameters[0].GetValue();
                            if (temp < 0.0)
                            {
                                log.Error(ctxMsg + "The switch function index (" + temp + ") is negative.");
                                throw new Exception("Fatal error");
                            }
                            int n = parameters.Count - 1;
                            int i = (int)(temp + 0.5);

                            if (i < n)
                                return parameters[i + 1].GetValue();
                            else
                            {
                                log.Error(ctxMsg + "The switch function index (" + temp +
                                       ") selected a value above the range of supplied values" +
                                       "[0:" + (n - 1) + "]" +
                                       " - not enough values were supplied.");
                                throw new Exception("Fatal error");
                            }
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, short.MaxValue, 2));
                    }
                    else if (operation == "interpolate1d")
                    {
                        ParametersFunction f = (parameters) =>
                        {
                            // This is using the bisection algorithm. Special care has been
                            // taken to evaluate each parameter only once.
                            int n = parameters.Count;
                            double x = parameters[0].GetValue();
                            double xmin = parameters[1].GetValue();
                            double ymin = parameters[2].GetValue();
                            if (x <= xmin) return ymin;

                            double xmax = parameters[n - 2].GetValue();
                            double ymax = parameters[n - 1].GetValue();
                            if (x >= xmax) return ymax;

                            int nmin = 0;
                            int nmax = (n - 3) / 2;
                            while (nmax - nmin > 1)
                            {
                                int m = (nmax - nmin) / 2 + nmin;
                                double xm = parameters[2 * m + 1].GetValue();
                                double ym = parameters[2 * m + 2].GetValue();
                                if (x < xm)
                                {
                                    xmax = xm;
                                    ymax = ym;
                                    nmax = m;
                                }
                                else if (x > xm)
                                {
                                    xmin = xm;
                                    ymin = ym;
                                    nmin = m;
                                }
                                else
                                    return ym;
                            }

                            return ymin + (x - xmin) * (ymax - ymin) / (xmax - xmin);
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, short.MaxValue, 5, OddEven.Odd));
                    }
                    else if (operation == "rotation_alpha_local")
                    {
                        // Calculates local angle of attack for skydiver body component.
                        // Euler angles from the intermediate body frame to the local body frame
                        // must be from a z-y-x axis rotation order
                        ParametersFunction f = (p) =>
                        {
                            double alpha = p[0].GetValue() * Constants.degtorad; //angle of attack of intermediate body frame
                            double beta = p[1].GetValue() * Constants.degtorad;  //sideslip angle of intermediate body frame
                            double phi = p[3].GetValue() * Constants.degtorad;   //x-axis Euler angle from the intermediate body frame to the local body frame
                            double theta = p[4].GetValue() * Constants.degtorad; //y-axis Euler angle from the intermediate body frame to the local body frame
                            double psi = p[5].GetValue() * Constants.degtorad;   //z-axis Euler angle from the intermediate body frame to the local body frame

                            Quaternion qTb2l = new Quaternion(phi, theta, psi);
                            double cos_beta = Math.Cos(beta);
                            Vector3D wind_body = new Vector3D(Math.Cos(alpha) * cos_beta, Math.Sin(beta),
                                                     Math.Sin(alpha) * cos_beta);
                            Vector3D wind_local = qTb2l.GetTransformationMatrix() * wind_body;

                            if (Math.Abs(Math.Abs(wind_local.Y) - 1.0) < 1E-9)
                                return 0.0;
                            else
                                return Math.Atan2(wind_local.Z, wind_local.X) * Constants.radtodeg;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 6, 6));
                    }
                    else if (operation == "rotation_beta_local")
                    {
                        // Calculates local angle of sideslip for skydiver body component.
                        // Euler angles from the intermediate body frame to the local body frame
                        // must be from a z-y-x axis rotation order
                        ParametersFunction f = (p) =>
                        {
                            double alpha = p[0].GetValue() * Constants.degtorad; //angle of attack of intermediate body frame
                            double beta = p[1].GetValue() * Constants.degtorad;  //sideslip angle of intermediate body frame
                            double phi = p[3].GetValue() * Constants.degtorad;   //x-axis Euler angle from the intermediate body frame to the local body frame
                            double theta = p[4].GetValue() * Constants.degtorad; //y-axis Euler angle from the intermediate body frame to the local body frame
                            double psi = p[5].GetValue() * Constants.degtorad;   //z-axis Euler angle from the intermediate body frame to the local body frame
                            Quaternion qTb2l = new Quaternion(phi, theta, psi);
                            double cos_beta = Math.Cos(beta);
                            Vector3D wind_body = new Vector3D(Math.Cos(alpha) * cos_beta, Math.Sin(beta),
                                                      Math.Sin(alpha) * cos_beta);
                            Vector3D wind_local = qTb2l.GetTransformationMatrix() * wind_body;

                            if (Math.Abs(Math.Abs(wind_local.Y) - 1.0) < 1E-9)
                                return wind_local.Y > 0.0 ? 0.5 * Math.PI : -0.5 * Math.PI;

                            double alpha_local = Math.Atan2(wind_local.Z, wind_local.X);
                            double cosa = Math.Cos(alpha_local);
                            double sina = Math.Sin(alpha_local);
                            double cosb;

                            if (Math.Abs(cosa) > Math.Abs(sina))
                                cosb = wind_local.X / cosa;
                            else
                                cosb = wind_local.Z / sina;

                            return Math.Atan2(wind_local.Y, cosb) * Constants.radtodeg;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 6, 6));
                    }
                    else if (operation == "rotation_gamma_local")
                    {
                        // Calculates local roll angle for skydiver body component.
                        // Euler angles from the intermediate body frame to the local body frame
                        // must be from a z-y-x axis rotation order
                        ParametersFunction f = (p) =>
                        {
                            double alpha = p[0].GetValue() * Constants.degtorad; //angle of attack of intermediate body frame
                            double beta = p[1].GetValue() * Constants.degtorad;  //sideslip angle of intermediate body frame
                            double gamma = p[2].GetValue() * Constants.degtorad; //roll angle of intermediate body frame
                            double phi = p[3].GetValue() * Constants.degtorad;   //x-axis Euler angle from the intermediate body frame to the local body frame
                            double theta = p[4].GetValue() * Constants.degtorad; //y-axis Euler angle from the intermediate body frame to the local body frame
                            double psi = p[5].GetValue() * Constants.degtorad;   //z-axis Euler angle from the intermediate body frame to the local body frame
                            double cos_alpha = Math.Cos(alpha), sin_alpha = Math.Sin(alpha);
                            double cos_beta = Math.Cos(beta), sin_beta = Math.Sin(beta);
                            double cos_gamma = Math.Cos(gamma), sin_gamma = Math.Sin(gamma);
                            Quaternion qTb2l = new Quaternion(phi, theta, psi);
                            Vector3D wind_body_X = new Vector3D(cos_alpha * cos_beta, sin_beta,
                                                            sin_alpha * cos_beta);
                            Vector3D wind_body_Y = new Vector3D(-sin_alpha * sin_gamma - sin_beta * cos_alpha * cos_gamma,
                                                 cos_beta * cos_gamma,
                                                                  -sin_alpha * sin_beta * cos_gamma + sin_gamma * cos_alpha);
                            Vector3D wind_local_X = qTb2l.GetTransformationMatrix() * wind_body_X;
                            Vector3D wind_local_Y = qTb2l.GetTransformationMatrix() * wind_body_Y;
                            double cosacosb = wind_local_X.X;
                            double sinb = wind_local_X.Y;
                            double sinacosb = wind_local_X.Z;
                            double sinc, cosc;

                            if (Math.Abs(sinb) < 1E-9)
                            { // cos(beta_local) == 1.0
                                cosc = wind_local_Y.Y;

                                if (Math.Abs(cosacosb) > Math.Abs(sinacosb))
                                    sinc = wind_local_Y.Z / cosacosb;
                                else
                                    sinc = -wind_local_Y.X / sinacosb;
                            }
                            else if (Math.Abs(Math.Abs(sinb) - 1.0) < 1E-9)
                            { // cos(beta_local) == 0.0
                                sinc = wind_local_Y.Z;
                                cosc = -wind_local_Y.X;
                            }
                            else
                            {
                                sinc = cosacosb * wind_local_Y.Z - sinacosb * wind_local_Y.X;
                                cosc = (-sinacosb * wind_local_Y.Z - cosacosb * wind_local_Y.X) / sinb;
                            }

                            return Math.Atan2(sinc, cosc) * Constants.radtodeg;
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 6, 6));
                    }
                    else if (operation == "rotation_wf_to_bf")
                    {
                        // Transforms the input vector from q wind frame to a body frame. The
                        // origin of the vector remains the same.
                        string ctxMsg = element.InnerText;
                        ParametersFunction f = (p) =>
                        {
                            double rx = p[0].GetValue();             //x component of input vector
                            double ry = p[1].GetValue();             //y component of input vector
                            double rz = p[2].GetValue();             //z component of input vector
                            double alpha = p[3].GetValue() * Constants.degtorad; //angle of attack of the body frame
                            double beta = p[4].GetValue() * Constants.degtorad;  //sideslip angle of the body frame
                            double gamma = p[5].GetValue() * Constants.degtorad; //roll angle of the body frame
                            int idx = (int)p[6].GetValue();

                            if ((idx < 1) || (idx > 3))
                            {
                                log.Error(ctxMsg + "The index must be one of the integer value 1, 2 or 3.");
                                throw new Exception("Fatal error");
                            }

                            Quaternion qa = new Quaternion() { Y = -alpha };
                            Quaternion qb = new Quaternion() { Z = beta };
                            Quaternion qc = new Quaternion() { X = -gamma };
                            Matrix3D mT = (qa * qb * qc).GetTransformationMatrix();
                            Vector3D r0 = new Vector3D(rx, ry, rz);
                            mT.Transpose();
                            Vector3D r = mT * r0;

                            return r[idx];
                        };
                        parameters.Add(new aFunc(f, fdmex, element, prefix, var, 7, 7));
                    }
                    else if (operation != "description")
                    {
                        log.Error("Bad operation <" + operation + "> detected in configuration file");
                    }

                    // Optimize functions applied on constant parameters by replacing them by
                    // their constant result.
                    if (parameters.Count != 0)
                    {
#if TODO
                        Function  p = dynamic_cast<FGFunction*>(Parameters.back().ptr());

                        if (p != null && p.IsConstant())
                        {
                            double constant = p->GetValue();
                            FGPropertyNode_ptr node = p->pNode;
                            string pName = p->GetName();

                            Parameters.pop_back();
                            Parameters.push_back(new FGRealValue(constant));
                            if (debug_lvl > 0)
                                cout << element->ReadFrom() << fggreen << highint
                                     << "<" << operation << "> is applied on constant parameters."
                                     << endl << "It will be replaced by its result ("
                                     << constant << ")";

                            if (node)
                            {
                                node->setDoubleValue(constant);
                                node->setAttribute(SGPropertyNode::WRITE, false);
                                if (debug_lvl > 0)
                                    cout << " and the property " << pName
                                         << " will be unbound and made read only.";
                            }
                            cout << reset << endl << endl;
                        }
#endif
                    }
                }
            }

            Bind(el, prefix); // Allow any function to save its value
        }

        protected bool GetBinary(double val, string ctxMsg)
        {
            val = Math.Abs(val);
            if (val < 1E-9) return false;
            else if (val - 1 < 1E-9) return true;
            else
            {
                log.Error(ctxMsg + "Malformed conditional check in function definition.");
                throw new Exception("Fatal Error.");
            }
        }
        protected Random MakeRandomEngine(XmlElement el, FDMExecutive fdmex)
        {
            string seed_attr = el.GetAttribute("seed");
            int seed;
            if (string.IsNullOrEmpty(seed_attr))
                return fdmex.GetRandomEngine();
            else if (seed_attr == "time_now")
                seed = Environment.TickCount;
            else
                seed = int.Parse(seed_attr);
            return new Random(seed);
        }
        protected void CheckMinArguments(XmlElement el, int _min)
        {
            if (parameters.Count < _min)
            {
                string msg = "<" + el.Name + "> should have at least " + _min + " argument(s).";
                log.Error(msg);
                throw new WrongNumberOfArguments(msg, parameters, el);
            }
        }
        protected void CheckMaxArguments(XmlElement el, int _max)
        {
            if (parameters.Count > _max)
            {
                string msg = "<" + el.Name + "> should have no more than " + _max + " argument(s).";
                log.Error(msg);
                throw new WrongNumberOfArguments(msg, parameters, el);
            }
        }

        protected void CheckOddOrEvenArguments(XmlElement el, OddEven odd_even)
        {
            switch (odd_even)
            {
                case OddEven.Even:
                    if (parameters.Count % 2 == 1)
                    {
                        string msg = "<" + el.Name + "> must have an even number of arguments.";
                        log.Error(msg);
                        throw new Exception("Fatal Error");
                    }
                    break;
                case OddEven.Odd:
                    if (parameters.Count % 2 == 0)
                    {
                        string msg = "<" + el.Name + "> must have an odd number of arguments.";
                        log.Error(msg);
                        throw new Exception("Fatal Error");
                    }
                    break;
                default:
                    break;
            }
        }
        protected string CreateOutputNode(XmlElement el, string prefix)
        {
            string nName = "";

            if (!string.IsNullOrEmpty(name))
            {
                if (string.IsNullOrEmpty(prefix))
                    nName = PropertyManager.MakePropertyName(name, false);
                else
                {
                    double tmp;
                    if (double.TryParse(prefix, out tmp))
                    {
                        if (name.Contains("#"))
                        { // if "#" is found
                            name = name.Replace("#", prefix);
                            nName = PropertyManager.MakePropertyName(name, false);
                        }
                        else
                        {
                            if (log.IsErrorEnabled)
                                log.Error("Malformed function name with number: " + prefix +
                                      " and property name: " + name +
                                      " but no \"#\" sign for substitution.");
                        }
                    }
                    else
                    {
                        nName = PropertyManager.MakePropertyName(prefix + "/" + Name, false);
                    }
                }

                pNode = propertyManager.GetNode(nName, true);
                if (pNode.IsTied())
                {
                    if (log.IsErrorEnabled)
                        log.Error("Property " + nName + " has already been successfully bound (late).");
                    throw new Exception("Failed to bind the property to an existing already tied node.");
                }
            }

            return nName;
        }
        protected virtual void Bind()
        {
            //TODO Check that
            /*
                        string tmp = propertyManager.mkPropertyName(Prefix + Name, false); // Allow upper case
                        propertyManager.Tie( tmp, this, GetValue);
            */
            if (Name.Length != 0)
            {
                propertyManager.Tie(prefix + Name, this, this.GetType().GetProperty("Value"), false);
            }
        }
        protected virtual void Bind(XmlElement el, string prefix)
        {
            string nName = CreateOutputNode(el, prefix);

            if (!string.IsNullOrEmpty(nName))
                propertyManager.Tie(nName, this.GetValue, null);
        }

        protected bool cached = false;
        protected double cachedValue;
        protected List<IParameter> parameters = new List<IParameter>(); //vector <FGParameter*>
        protected PropertyManager propertyManager;
        protected PropertyNode pNode;

        private string prefix;
        private enum FunctionType
        {
            TopLevel = 0, Product, Difference, Sum, Quotient, Pow,
            Abs, Sin, Cos, Tan, ASin, ACos, ATan, ATan2
        };

        private FunctionType functionType;
        private string name;
        private PropertyNode pCopyTo; // Property node for CopyTo property string
    }

    public class WrongNumberOfArguments : Exception
    {
        public WrongNumberOfArguments(string message, List<IParameter> parameters,
                         XmlElement el) : base(message)
        {
            this.parameters = parameters;
            this.element = el;
        }

        public WrongNumberOfArguments(string message)
            : base(message)
        {
        }

        public int NumberOfArguments() { return parameters.Count; }
        public IParameter FirstParameter() { return parameters[0]; }
        public XmlElement GetElement() { return element; }

        private List<IParameter> parameters;
        private XmlElement element;

    }

    public delegate double ParametersFunction(List<IParameter> parameters);

    public class aFunc : Function
    {
        public aFunc(ParametersFunction _f, FDMExecutive fdmex, XmlElement el,
                  string prefix, PropertyValue v, int Nmax = short.MaxValue, int Nmin = 0,
                 Function.OddEven odd_even = Function.OddEven.Either)
            : base(fdmex.PropertyManager)
        {
            f = _f;
            Load(el, v, fdmex, prefix);
            CheckMinArguments(el, Nmin);
            CheckMaxArguments(el, Nmax);
            CheckOddOrEvenArguments(el, odd_even);
        }

        public override double GetValue()
        {
            return cached ? cachedValue : f(parameters);
        }

        protected override void Bind(XmlElement el, string Prefix)
        {
            string nName = CreateOutputNode(el, Prefix);
            if (!string.IsNullOrEmpty(nName))
                propertyManager.Tie(nName, this.GetValue, null);
        }

        private ParametersFunction f;
    };
}
