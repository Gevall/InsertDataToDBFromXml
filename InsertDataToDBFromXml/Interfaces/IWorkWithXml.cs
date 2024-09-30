using InsertDataToDBFromXml.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsertDataToDBFromXml.Interfaces
{
    internal interface IWorkWithXml
    {
        /// <summary>
        /// Возвращает обьект Orders прочитаный из файла
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public Orders GetDataFromFile(string filename);

        /// <summary>
        /// Возвращает список файлов в дирректории для чтения
        /// </summary>
        /// <returns></returns>
        public string[] FilesInFolder();

        /// <summary>
        /// Удаление прочитанных файлов
        /// </summary>
        /// <param name="files"></param>
        public void DeleteFiles(string file);
    }
}
