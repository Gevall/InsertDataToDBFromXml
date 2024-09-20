using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InsertDataToDBFromXml.Model
{
    [XmlRoot(ElementName = "orders")]
    public class Orders
    {

        [XmlElement(ElementName = "order")]
        public List<Order>? orders { get; set; }
    }
}
