using InsertDataToDBFromXml.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsertDataToDBFromXml.Interfaces
{
    internal interface IWorkWithXmlWithCheck : IWorkWithXml
    {

        public List<CheckReadFiles> ReadFileInFolder();
    }
}
