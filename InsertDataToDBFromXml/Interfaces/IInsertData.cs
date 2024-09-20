using InsertDataToDBFromXml.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsertDataToDBFromXml.Interfaces
{
    internal interface IInsertData
    {
        /// <summary>
        /// Запрос на вставку в базу данных прочитанных из файла
        /// </summary>
        /// <param name="orders"></param>
        public void InsertData(Orders orders);
    }
}
