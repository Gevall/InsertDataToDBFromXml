using InsertDataToDBFromXml.Interfaces;
using InsertDataToDBFromXml.Model;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsertDataToDBFromXml.Classes
{
    internal class InsertData : IInsertData
    {
        SqlConnection _connection;
        public InsertData()
        {
            _connection = GetSqlConnection();
        }

        /// <summary>
        /// Добавление заказа в БД
        /// </summary>
        /// <param name="orders"></param>
        async void IInsertData.InsertData(Orders orders)
        {
            SqlCommand command = new SqlCommand();
            command.Connection = _connection;

            foreach (var order in orders.orders)
            {
                _connection.Open();
                await checkingExisitingGoods(order.product);
                var prdouctFromDB = await GetDataFromDb();
                var id = await chekUserExists(order.user);
                var productQuary = await GetQuaryToProducts(order.product);
                if (id != 0)
                {

                    var quary = "BEGIN TRANSACTION;" +
                        "\ninsert into Orders (client_id, [no], reg_date, [sum])" +
                        $"\nvalues ({id}, {order.no}, '{order.reg_date}', {order.sum});" +
                        "\nDeclare @last_order_id INT;" +
                        "\nSet @last_order_id = SCOPE_IDENTITY();" +
                        "\nInsert into BasketList (order_id, goods_id, goods_count)" +
                        $"\nValues {productQuary}" +
                        "\nSelect b.goods_id, b.order_id, b.goods_count" +
                        "\nFrom BasketList b" +
                        "\nInner Join Goods g on g.id = b.goods_id" +
                        "\nInner Join Orders o on o.id = b.order_id;" +
                        "\nCOMMIT;";
                    Console.WriteLine(quary);
                    command.CommandText = quary;
                    await command.ExecuteNonQueryAsync();
                }
                _connection.Close();
            }
        }

        /// <summary>
        /// Получение строки соединения с БД
        /// </summary>
        /// <returns></returns>
        private SqlConnection GetSqlConnection()
        {
            if (File.Exists(".\\connectionString.txt"))
            {
                var connectionString = File.ReadAllText(".\\connectionString.txt");
                SqlConnection connection = new SqlConnection(connectionString);
                return connection;
            }
            else
            {
                Console.WriteLine("Файл с кофнигурацией подключения не найден!\n" +
                    "Невозможно установить содинения с базой данных!");
            }
            return null;
        }
        
        /// <summary>
        /// Проверка наличия пользователя в БД
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Возвращает id пользователя</returns>
        async Task<int> chekUserExists(User user)
        {
            int id = 0;
            var quary = $"Select id from Users where client_address = N'{user.email}';";
            //Console.WriteLine("QUARY - " + quary);
            SqlCommand command = new SqlCommand(quary, _connection);
            var response = command.ExecuteReader();
            if (response.HasRows)
            {
                response.Read();
                object _id = response["id"];
                //Console.WriteLine($"_id = {_id}");
                response.Close();
                return (int)_id;
            }
            // Добавление пользователя при его отсутствии в БД
            else 
            {
                response.Close();
                //Console.WriteLine("Пользователь не найден!");
                addNewUser(user);
                // После добавления пользователя вызываем метод заного для возврата id
                id = await chekUserExists(user);
                
            }
            response.Close();
            return 0;
        }

        /// <summary>
        /// Проверка товаров из XML на наличие записей в БД
        /// </summary>
        /// <param name="products"></param>
        /// <returns></returns>
        async Task checkingExisitingGoods(List<Product> products)
        {
            List<Product> dbListProduct = await GetDataFromDb();
            string quary = "";

            foreach (var item in products)
            {
                if (dbListProduct.Exists(x => x.name == item.name)) { continue; }
                else
                {
                    quary += $"Insert into Goods (goods_name, goods_price) Values (N'{item.name}', {item.price});\n";
                }
            }

            if (!quary.IsNullOrEmpty())
            {
                SqlCommand command = new SqlCommand(quary, _connection);
                //Console.WriteLine("Quary:\n" + quary);
                await command.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Получение данных по товарам из БД
        /// </summary>
        /// <returns></returns>
        private async Task<List<Product>> GetDataFromDb()
        {
            var quary = "Select id, goods_name From Goods"; //Выборка из базы существующих элементов
            SqlCommand command = new SqlCommand(quary, _connection);
            var result = await command.ExecuteReaderAsync();

            List<Product> dbListProduct = new List<Product>();

            while (await result.ReadAsync())
            {
                object id = result["id"];
                object name = result["goods_name"];
                dbListProduct.Add(new Product { id = (int)id, name = name.ToString() });
            }
            result.Close();
            return dbListProduct;
        }

        /// <summary>
        /// Добавление пользователя при его отсутствии в БД
        /// </summary>
        /// <param name="user">Данные пользователя</param>
        private void addNewUser(User user)
        {
            var quary = "Insert into Users (client_name, client_address) " +
                        $"Values (N'{user.fio}', N'{user.email}');";
            //Console.WriteLine("Добавление пользователя: " + quary);
            SqlCommand command = new SqlCommand(quary , _connection);
            command.ExecuteNonQuery();
        }
        
        /// <summary>
        /// Формирование строки с товарами для вставки в запрос
        /// </summary>
        /// <param name="products"></param>
        /// <returns>Строка с товарами для вставки в запрос</returns>
        private async Task<string> GetQuaryToProducts(List<Product> products)
        {
            SqlCommand command = new SqlCommand();
            command.Connection = _connection;
            List<Product> productsListWithIds = new List<Product>();
            var quary = "";
            foreach (var product in products)
            {
                quary = $"Select id, goods_name from Goods Where goods_name = N'{product.name}'";
                command.CommandText = quary;
                var response = await command.ExecuteReaderAsync();
                response.Read();
                productsListWithIds.Add(new Product
                {
                    id = (int)response["id"],
                    name = response["goods_name"].ToString()
                });
                response.Close();
            }
            quary = "";
            for (int i = 0; i < productsListWithIds.Count; i++)
            {
                quary  += $"(@last_order_id,{productsListWithIds[i].id},{products[i].quantity}),";
            }
            quary = quary.TrimEnd(',');
            //Console.WriteLine(">>>>>> " + quary);
            return quary;
        }
    }
}
