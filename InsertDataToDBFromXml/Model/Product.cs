using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InsertDataToDBFromXml.Model
{
    [XmlRoot(ElementName = "product")]
    public class Product
    {
        public int id {  get; set; }

        [XmlElement(ElementName = "quantity")]
        public int quantity { get; set; }

        [XmlElement(ElementName = "name")]
        public string name { get; set; }

        [XmlElement(ElementName = "price")]
        public string price { get; set; }
    }
}
