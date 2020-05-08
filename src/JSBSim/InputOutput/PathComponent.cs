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
namespace JSBSim.InputOutput
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public class PathComponent
    {
        public string name;
        public int index;
    }
    public static class PathComponentUtis
    {
        /// <summary>
        /// Validate a Name
        /// 
        /// Name: [_a-zA-Z][-._a-zA-Z0-9]*
        /// </summary>
        /// <returns></returns>
        public static bool ValidateName(string name)
        {
            return validRegexPattern.IsMatch(name);
        }

        /// <summary>
        /// Parse the name for a path component.
        /// 
        /// Name: [_a-zA-Z][-._a-zA-Z0-9]*
        /// </summary>
        /// <param name="name"></param>
        /// <param name="i">Initial position</param>
        /// <returns></returns>
        public static string ParseName(string path, ref int i)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Illegal path. Cant be null or empty");
            if (i >= path.Length)
                return null;

            string name = "";
            int max = path.Length;

            if (path[i] == '.')
            {
                i++;
                if (i < max && path[i] == '.')
                {
                    i++;
                    name = "..";
                }
                else
                {
                    name = ".";
                }
                if (i < max && path[i] != '/')
                    throw new Exception("Illegal character after " + name);
            }

            else if (char.IsLetter(path[i]) || path[i] == '_')
            {
                name += path[i];
                i++;

                // The rules inside a name are a little
                // less restrictive.
                while (i < max)
                {
                    if (char.IsLetter(path[i]) || char.IsDigit(path[i]) || path[i] == '_' ||
                    path[i] == '-' || path[i] == '.')
                    {
                        name += path[i];
                    }
                    else if (path[i] == '[' || path[i] == '/')
                    {
                        break;
                    }
                    else
                    {
                        throw new Exception("name may contain only ._- and alphanumeric characters");
                    }
                    i++;
                }
            }

            else
            {
                if (name.Length == 0)
                    throw new Exception("name must begin with alpha or '_'");
            }

            return name;
        }

        /// <summary>
        /// Parse the optional integer index for a path component.
        /// 
        /// Index: "[" [0-9]+ "]"
        /// </summary>
        /// <param name="path"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        public static int ParseIndex(string path, ref int i)
        {
            if (string.IsNullOrEmpty(path) || i >= path.Length)
                return 0;

            int index = 0;

            if (path[i] != '[')
                return 0;
            else
                i++;

            for (int max = (int)path.Length; i < max; i++)
            {
                if (char.IsDigit(path[i]))
                {
                    index = (index * 10) + (path[i] - '0');
                }
                else if (path[i] == ']')
                {
                    i++;
                    return index;
                }
                else
                {
                    break;
                }
            }

            throw new Exception("unterminated index (looking for ']')");
        }

        public static PathComponent ParseComponent(string path, ref int i)
        {
            PathComponent component = new PathComponent();
            component.name = ParseName(path, ref i);
            if (component.name[0] != '.')
                component.index = ParseIndex(path, ref i);
            else
                component.index = -1;
            return component;
        }

        public static void ParsePath(string path, List<PathComponent> components)
        {
            int pos = 0;
            int max = path.Length;

            // Check for initial '/'
            if (path[pos] == '/')
            {
                PathComponent root = new PathComponent();
                root.name = "";
                root.index = -1;
                components.Add(root);
                pos++;
                while (pos < max && path[pos] == '/')
                    pos++;
            }

            while (pos < max)
            {
                components.Add(ParseComponent(path, ref pos));
                while (pos < max && path[pos] == '/')
                    pos++;
            }
        }

        private static readonly Regex validRegexPattern = new Regex(@"^[_a-zA-Z][-._a-zA-Z0-9]*$", RegexOptions.IgnoreCase);
    }
}
