using InsertDataToDBFromXml.Interfaces;
using InsertDataToDBFromXml.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsertDataToDBFromXml.Classes
{
    internal class WorkWithXmlWithCheck : WorkWithXML, IWorkWithXmlWithCheck
    {
        List<CheckReadFiles> checkReadFiles; // Файлы для обработки, а так же свойство был ли файл обработан ранее 
        public WorkWithXmlWithCheck(List<CheckReadFiles> _checkReadFiles) 
        {
            checkReadFiles = _checkReadFiles;
        }


        /// <summary>
        /// Проверка файлов на повторную обработку
        /// </summary>
        /// <returns></returns>
        public List<CheckReadFiles> ReadFileInFolder()
        {
            var files = FilesInFolder();
            foreach (var file in files)
            {
                if (checkReadFiles.Find(x => x.path == file) == null)
                {
                    checkReadFiles.Add(new CheckReadFiles
                    {
                        path = file,
                        read = false
                    });
                }
                else
                {
                    Console.WriteLine("Такой файл уже был в обработке!");
                }
            }
            return checkReadFiles;
        }
    }
}
