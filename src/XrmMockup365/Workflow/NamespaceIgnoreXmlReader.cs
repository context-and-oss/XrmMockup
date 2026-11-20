using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;

namespace WorkflowParser {
    internal class NamespaceIgnoreXmlReader : XmlTextReader {

        public NamespaceIgnoreXmlReader(System.IO.Stream stream) : base(stream) { }

        public NamespaceIgnoreXmlReader(string xaml) : base(new StringReader(xaml)) { }

        public override string NamespaceURI {
            get {
                return "";
            }
        }
    }
}
