using InsertDataToDBFromXml.Interfaces;
using InsertDataToDBFromXml.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InsertDataToDBFromXml.Classes
{
    internal class WorkWithXML : IWorkWithXml
    {
        public void DeleteFiles(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch(Exception ex) { Console.WriteLine("Ошибка при удалении: " + ex.Message); }
        }

        public string[] FilesInFolder()
        {
            if (Directory.Exists(".\\inputFolder"))
            {
                var files = Directory.GetFiles(".\\inputFolder\\");
                if (files.Length > 0)
                {
                    Console.WriteLine("\nСписок файлов для обработки:");
                    int count = 0;
                    foreach (var file in files)
                    {
                        Console.WriteLine($"{count++}) {file}");
                    }
                    return files;
                }
                else
                {
                    Console.WriteLine("\nФайлов в каталоге нет.");
                }
            }
            else
            {
                Console.WriteLine("\nДирректории не отсутствует. Создаю...");
                Directory.CreateDirectory(".\\inputFolder");
                Console.WriteLine("Положите туда файлы для обработки!");
            }
            return null;
        }

        public Orders GetDataFromFile(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Orders));
            try
            {
                using (StreamReader reader = new StreamReader(filename))
                {
                    var result = (Orders)serializer.Deserialize(reader);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

    }
}
