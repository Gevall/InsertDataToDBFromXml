using InsertDataToDBFromXml.Interfaces;
using InsertDataToDBFromXml.Model;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
                //_connection.Open();
                InsertDataToDb(order, command);
                //_connection.Close();
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
            SqlCommand command = new SqlCommand();
            command.Connection = _connection;
            int id = 0;
            var quary = $"Select id from Users where client_address = N'@user_email';";
            command.CommandText = quary;
            command.Parameters.AddWithValue("@user_email", user.email);
            //Console.WriteLine("QUARY - " + quary);
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
            SqlCommand command = new SqlCommand();
            command.Connection = _connection;
            string quary = "";

            foreach (var item in products)
            {
                if (dbListProduct.Exists(x => x.name == item.name)) { continue; }
                else
                {
                    quary = $"sp_executesql Insert into Goods (goods_name, goods_price) Values (N'@item_name', @item_price);";
                    command.Parameters.AddWithValue("@item_name", item.name);
                    command.Parameters.AddWithValue("@item_price", item.price);
                    command.CommandText += quary;
                }
            }

            if (!quary.IsNullOrEmpty())
            {
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
            SqlCommand command = new SqlCommand();
            var quary = "sp_executesql Insert into Users (client_name, client_address) " +
                        $"Values (N'@user_fio', N'@user_email');";
            command.Connection = _connection;
            command.CommandText = quary;
            command.Parameters.AddWithValue("@user_fio", user.fio);
            command.Parameters.AddWithValue("@user_email", user.email);
            //Console.WriteLine("Добавление пользователя: " + quary);
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
                quary = $"Select id, goods_name from Goods Where goods_name = N'@product_name'";
                command.CommandText = quary;
                command.Parameters.AddWithValue("@product_name", product.name);
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

        async void IInsertData.InsertDataWithCheckOrderExists(Orders orders)
        {
            SqlCommand command = new SqlCommand();
            command.Connection = _connection;
            string quary;
            foreach (var order in orders.orders)
            {
                quary = "Select [no] from Goods where [no] = @number_of_order";
                command.Parameters.AddWithValue("@number_of_order", order.no);
                var response = await command.ExecuteReaderAsync();
                if (response.HasRows)
                {
                    response.Read();
                    Order findOrder = new Order
                    {
                        no = (int)response["id"],
                        product = (List<Product>)response["Goods"],
                        reg_date = (string)response["reg_date"],
                        sum = (string)response["sum"],
                        user = (User)response["user"]
                    };
                    // добавить метод сравнения заказа и обновления в случае различия данных
                    //Так же добавить отдельный метод получения данных из БД по заказу
                }
                else
                {
                    InsertDataToDb(order, command);
                }
            }
        }

        /// <summary>
        /// Вставка данных заказа в БД
        /// </summary>
        /// <param name="order"></param>
        /// <param name="command"></param>
        private async void InsertDataToDb(Order order, SqlCommand command)
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
                    $"\nvalues (@id, @order_no, '@order_reg_date', @summ);" +
                    "\nDeclare @last_order_id INT;" +
                    "\nSet @last_order_id = SCOPE_IDENTITY();" +
                    "\nInsert into BasketList (order_id, goods_id, goods_count)" +
                    $"\nValues @orduct_quary" +
                    "\nSelect b.goods_id, b.order_id, b.goods_count" +
                    "\nFrom BasketList b" +
                    "\nInner Join Goods g on g.id = b.goods_id" +
                    "\nInner Join Orders o on o.id = b.order_id;" +
                    "\nCOMMIT;";
                Console.WriteLine(quary);
                command.CommandText = quary;
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@order_no", order.no);
                command.Parameters.AddWithValue("@order_reg_date", order.reg_date);
                command.Parameters.AddWithValue("@summ", order.sum);
                command.Parameters.AddWithValue("@orduct_quary", productQuary);
                await command.ExecuteNonQueryAsync();
                _connection.Close();
            }
        }

        /// <summary>
        /// Метод сравнения заказов
        /// </summary>
        /// <param name="order"></param>
        /// <param name="esistOrder"></param>
        /// <returns>Возвращает true если заказы одинаковые и false если разные</returns>
        private bool CheckSameOrder(Order order, Order esistOrder)
        {
            // Добавить реализацию
            return false;
        }

        private async void UpdateDataInDb(Order order, SqlCommand command, int numberOfOrder)
        {
            // Добавить реализацию обновления данных в бд
        }
    }
}
