using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WorkflowParser {
    internal class Parser {
        internal static ActivityRoot Parse(string xaml) {
            XmlSerializer serializer = new XmlSerializer(typeof(ActivityRoot));
            return (ActivityRoot)serializer.Deserialize(new NamespaceIgnoreXmlReader(xaml));
        }
    }
}
