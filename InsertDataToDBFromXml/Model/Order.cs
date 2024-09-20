using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InsertDataToDBFromXml.Model
{
    [XmlRoot(ElementName = "order")]
    public class Order
    {

        [XmlElement(ElementName = "no")]
        public int no { get; set; }

        [XmlElement(ElementName = "reg_date")]
        public string reg_date { get; set; }

        [XmlElement(ElementName = "sum")]
        public string sum { get; set; }

        [XmlElement(ElementName = "product")]
        public List<Product> product { get; set; }

        [XmlElement(ElementName = "user")]
        public User user { get; set; }
    }
}
