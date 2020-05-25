using System;
using System.Xml;
using System.Xml.Linq;
using log4net;

namespace CommonUtils.IO
{
    public static class XmlExtensions
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

        public static string ReadFrom(this XElement el)
        {
            var lineInfo = (IXmlLineInfo)el;
            string message = "";
            if (lineInfo.HasLineInfo())
            {
                if (string.IsNullOrEmpty(el.BaseUri))
                    message += "In line " + lineInfo.LineNumber + ": position " + lineInfo.LinePosition + "\n";
                else
                    message += "In file: " + el.BaseUri + ": line " + lineInfo.LineNumber + ": position " + lineInfo.LinePosition + "\n";
            }
            return message;
        }

        public static void MergeAttributes(this XmlElement org, XmlElement el)
        {
#if TODO
            foreach (var it in el.Attributes())
            {
                if (org.Attribute(it.Name) == null)
                    org.aattributes[it.Name] = it.Value;
                else
                {
                    if (FGJSBBase::debug_lvl > 0 && (attributes[it->first] != it->second))
                        cout << el->ReadFrom() << " Attribute '" << it->first << "' is overridden in file "
                             << GetFileName() << ": line " << GetLineNumber() << endl
                             << " The value '" << attributes[it->first] << "' will be used instead of '"
                             << it->second << "'." << endl;
                }
            }
#endif
            throw new System.NotImplementedException();
        }
        public static XmlElement FindElement(this XmlElement doc, string name)
        {
            XmlNodeList list = doc.GetElementsByTagName(name);
            if (list != null && list.Count > 0)
                return list[0] as XmlElement;
            else
                return null;
        }
        public static bool FindElement(this XmlElement doc, string name, out XmlElement rst)
        {
            XmlNodeList list = doc.GetElementsByTagName(name);
            if (list != null && list.Count > 0)
                rst = list[0] as XmlElement;
            else
                rst = null;
            return (rst != null);
        }

    }
}
