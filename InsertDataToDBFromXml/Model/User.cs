using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InsertDataToDBFromXml.Model
{
    [XmlRoot(ElementName = "user")]
    public class User
    {

        [XmlElement(ElementName = "fio")]
        public string fio { get; set; }

        [XmlElement(ElementName = "email")]
        public string email { get; set; }
    }
}
