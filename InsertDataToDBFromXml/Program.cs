using InsertDataToDBFromXml.Classes;
using InsertDataToDBFromXml.Interfaces;
using InsertDataToDBFromXml.Model;
using System.Data.Common;
using System.Net.Http.Headers;
using System.Text.Unicode;
using System.Xml;
using System.Xml.Serialization;

namespace InsertDataToDBFromXml
{
    internal class Program
    {
        static List<CheckReadFiles> fileToRead = new List<CheckReadFiles>();

        static void Main(string[] args)
        {
            bool start = true;
            while (start)
            {
                //Console.Clear();
                Console.WriteLine("1) Нажмите \"1\" для чтения файлов и занесения данных в БД" +
                                "\n2) Нажмите \"2\" для генерации тестового файла \"output.xml\"" +
                                "\n0) Нажмите \"0\" для выхода из программы");
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.D1:
                        startProgram(); //Запуск программы
                        break;
                    case ConsoleKey.D2:
                        CreateXMLDoc(); // Создание тестового xml
                        break;
                    case ConsoleKey.D0: // Выход
                        start = false; 
                        break;
                }
            }
        }

        private static void startProgram()
        {
            IWorkWithXmlWithCheck worker = new WorkWithXmlWithCheck(fileToRead);
            IInsertData sqlData = new InsertData();


            var files = worker.ReadFileInFolder();
            if (files != null)
            {
                foreach (var file in files)
                {
                    var data = worker.GetDataFromFile(file.path);
                    if (!file.read)
                    {
                        file.read = true;  // После прочтения файл помечается как прочитанный
                        sqlData.InsertDataWithCheckOrderExists(data); // Метод изменен на проверяющий уже созданные заказы в БД
                        Console.WriteLine("\nДанные успешно внесены!");
                    }
                    else Console.WriteLine("Файл уже был добавлен ранее");
                    worker.DeleteFiles(file.path); // Прочитанный файл удаляется из дирректории
                }
            }
        }


        #region Тестовый файл xml
        /// <summary>
        /// Для тестового создания xml файла
        /// </summary>
        /// <param name="serializer"></param>
        private static void CreateXMLDoc()
        {
            if (Directory.Exists(".\\output"))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Orders));
                Orders orders = new Orders();
                orders.orders = new List<Order>();
                orders.orders.Add(new Order
                {
                    no = 12,
                    product = new List<Product>
                {
                    new Product { name = "Samsung S24", price = "49990.81", quantity = 1 },
                    new Product {name = "Huawei Nova 9", price = "29990.00", quantity = 1 },
                    new Product {name = "LG g9", price = "18990.02", quantity = 1 }
                },
                    reg_date = DateTime.Now.ToString(),
                    sum = "24000.81",
                    user = new User { fio = "ABS", email = "xyz@gmail.com" }
                });

                orders.orders.Add(new Order
                {
                    no = 132,
                    product = new List<Product>
                {
                    new Product { name = "Sony S33", price = "10000.02", quantity = 1 },
                    new Product {name = "LG G8", price = "29990.00", quantity = 1 }
                },
                    reg_date = DateTime.Now.AddDays(10).ToString(),
                    sum = "38991.82",
                    user = new User { fio = "SMC", email = "qwerty@gmail.com" }
                });
                try
                {

                    using (StreamWriter stream = new StreamWriter(".\\output\\output.xml"))
                    {
                        serializer.Serialize(stream, orders);
                        Console.WriteLine("\nФайл сгенерирован: \"output.xml\"");
                    }

                }
                catch (Exception ex) { }
            }
            else
            {
                Directory.CreateDirectory(".\\output");
                CreateXMLDoc();
            }
        }
        #endregion

    }
}
