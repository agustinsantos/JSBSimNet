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
/// $Id: ReaderText.cs 3 2006-05-23 19:41:11Z sxta $
#endregion
namespace CommonUtils.IO
{
    using System;
    using System.IO;
    using System.Text;
    using System.Globalization;

    /// <summary>
    /// Summary description for ReaderText.
    /// </summary>
    public class ReaderText
    {
        public const char eof = '\uffff';   // signals end of file
        private const char empty = '\ufffe';   // signals: no lookahead character available

        private bool done = true;       // success of most recent operation
        private TextReader input = null;  // input stream
        private char ch = ' ';          // auxiliary for reading
        private char buf = empty;       // the lookahead character


        // Gets a NumberFormatInfo associated with the en-US culture.
        static NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

        public bool Done
        {
            get { return done; }
        }

        public ReaderText(TextReader reader)
        {
            input = reader;
            done = true;
        }

        public ReaderText(string path)
        {
            Open(new FileStream(path, FileMode.Open));
        }

        public void Open(Stream s)
        {
            input = new StreamReader(s);
            done = true;
        }

        public void Close()
        {
            if (input != null) input.Close();
            input = null;
        }

        private char CharAfterWhiteSpace()
        {
            char ch;
            do ch = Read(); while (ch <= ' ');
            return ch;
        }

        private string ReadDigits()
        {
            StringBuilder b = new StringBuilder();
            char ch = CharAfterWhiteSpace();
            if (ch == '-')
            {
                b.Append(ch);
                ch = Read();
            }
            while (Char.IsDigit(ch))
            {
                b.Append(ch);
                ch = Read();
            }
            buf = ch;
            return b.ToString();
        }

        private string ReadFloatDigits()
        {
            StringBuilder b = new StringBuilder();
            char ch = CharAfterWhiteSpace();
            if (ch == '+' || ch == '-')
            {
                b.Append(ch);
                ch = Read();
            }
            while (Char.IsDigit(ch))
            {
                b.Append(ch);
                ch = Read();
            }
            if (ch == '.')
            {
                b.Append(ch);
                ch = Read();
                while (Char.IsDigit(ch))
                {
                    b.Append(ch);
                    ch = Read();
                }
            }
            if (ch == 'e' || ch == 'E')
            {
                b.Append(ch);
                ch = Read();
                if (ch == '+' || ch == '-')
                {
                    b.Append(ch);
                    ch = Read();
                }
                while (Char.IsDigit(ch))
                {
                    b.Append(ch);
                    ch = Read();
                }
            }
            buf = ch;
            return b.ToString();
        }

        public char Read()
        {
            if (buf != empty)
            {
                ch = buf;
                if (buf != eof)
                    buf = empty;
            }
            else
            {
                int x;
                if (input == null)
                    x = Console.Read();
                else
                    x = input.Read();
                if (x < 0)
                {
                    ch = eof; buf = eof; done = false;
                }
                else
                {
                    ch = (char)x;
                }
            }
            return ch;
        }

        public int ReadInt()
        {
            string s = ReadDigits();
            try
            {
                done = true;
                if (s.Length != 0)
                    return Convert.ToInt32(s, nfi);
                else
                {
                    done = false;
                    return 0;
                }
            }
            catch
            {
                done = false;
                return 0;
            }
        }

        public long ReadLong()
        {
            string s = ReadDigits();
            try
            {
                done = true;
                if (s.Length != 0)
                    return Convert.ToInt64(s, nfi);
                else
                {
                    done = false;
                    return 0;
                }
            }
            catch
            {
                done = false;
                return 0;
            }
        }

        public float ReadFloat()
        {
            string s = ReadFloatDigits();
            try
            {
                done = true;
                if (s.Length != 0)
                    return Convert.ToSingle(s, nfi);
                else
                {
                    done = false;
                    return 0.0f;
                }
            }
            catch
            {
                done = false;
                return 0.0f;
            }
        }

        public double ReadDouble()
        {
            string s = ReadFloatDigits();
            try
            {
                done = true;
                if (s.Length != 0)
                    return Convert.ToDouble(s, nfi);
                else
                {
                    done = false;
                    return 0.0;
                }
            }
            catch
            {
                done = false;
                return 0.0;
            }
        }

        public bool ReadBool()
        {
            string s = ReadIdent();
            done = true;
            if (s == "true") return true;
            else if (s == "false") return false;
            else { done = false; return false; }
        }

        public string ReadIdent()
        {
            StringBuilder b = new StringBuilder();
            char ch = CharAfterWhiteSpace();
            if (Char.IsLetter(ch) || ch == '_')
            {
                b.Append(ch);
                ch = Read();
                while (Char.IsLetterOrDigit(ch) || ch == '_')
                {
                    b.Append(ch);
                    ch = Read();
                }
            }
            buf = ch;
            done = b.Length > 0;
            return b.ToString();
        }

        public string ReadString()
        {
            StringBuilder b = new StringBuilder();
            char ch = CharAfterWhiteSpace();
            if (ch == '"')
            {
                ch = Read();
                while (ch != eof && ch != '"')
                {
                    b.Append(ch);
                    ch = Read();
                }
                if (ch == '"') { done = true; ch = Read(); }
                else done = false;
            }
            else done = false;
            buf = ch;
            return b.ToString();
        }

        public string ReadWord()
        {
            StringBuilder b = new StringBuilder();
            char ch = CharAfterWhiteSpace();
            while (ch > ' ' && ch != eof)
            {
                b.Append(ch);
                ch = Read();
            }
            buf = ch;
            done = b.Length > 0;
            return b.ToString();
        }

        public string ReadLine()
        {
            StringBuilder b = new StringBuilder();
            char ch = Read();
            done = ch != eof;
            while ((ch != eof) && !((ch == '\r') || (ch == '\n')))
            {
                b.Append(ch);
                ch = Read();
            }
            buf = empty;
            return b.ToString();
        }

        public string ReadFile()
        {
            StringBuilder b = new StringBuilder();
            char ch = Read();
            while (done)
            {
                b.Append(ch);
                ch = Read();
            }
            buf = eof;
            done = true;
            return b.ToString();
        }

        public void SkipNewlines(ref int line_pos)
        {
            char ch;
            ch = Read();

            while (ch == '\n' || ch == '\r')
            {
                ch = Read();
                line_pos = 0;
            }
            buf = ch;
        }

        public string ReadItem(int width, ref int line_pos)
        {
            char ch;
            StringBuilder b = new StringBuilder();

            for (int i = 0; i < width; i++)
            {
                ch = Read();
                line_pos++;
                if ((ch == '\n') || (ch == '\r'))
                { // premature termination
                    line_pos--;
                    i--;
                    while (line_pos < 80 && i < width)
                    {
                        //line += ' ';
                        b.Append(' ');
                        line_pos++;
                        i++;
                    }
                    if (line_pos == 80)
                        line_pos = 0;
                    else
                        buf = ch;
                }
                else
                {
                    //line += ch;
                    b.Append(ch);
                }
            }
            return b.ToString();
        }

        public char Peek()
        {
            char ch = CharAfterWhiteSpace();
            buf = ch;
            return ch;
        }
    }
}
