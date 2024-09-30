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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orders"></param>
        async void IInsertData.InsertDataWithCheckOrderExists(Orders orders)
        {
            _connection.Open();
            SqlCommand command = new SqlCommand();
            command.Connection = _connection;
            string quary;
            foreach (var order in orders.orders)
            {
                //Console.WriteLine("Введите номер заказа");
                //var testNumber = int.Parse(Console.ReadLine());
                quary = "Select o.id, u.client_address from Orders o " +
                    "Left Join Users u on o.client_id = u.id " +
                    "where [no] = @number_of_order";
                command.Parameters.AddWithValue("@number_of_order", order.no);
                command.CommandText= quary;
                var response = await command.ExecuteReaderAsync();
                if (response.HasRows)
                {
                    var cheker = await CheckSameOrder(order, response);
                    if (cheker == false)
                    {
                         UpdateDataOfOrder(order.product, (int)response["id"]);
                    }
                }
                else
                {
                    InsertDataToDb(order, command);
                }
            }
            _connection.Close();
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
        private async Task<bool> CheckSameOrder(Order order, SqlDataReader orderFromDb)
        {
            orderFromDb.Read();
            var orderId = (int)orderFromDb["id"];
            var userAddress = orderFromDb["client_address"].ToString();
            orderFromDb.Close();
            var product = await GetProductListFromConcreteOrder(orderId);
            if (order.user.email == userAddress)
            {
                List<Product> updateProductList = new List<Product>();
                for (int i = 0; i < product.Count; i++)
                {
                    if (product[i].name != order.product[i].name ||
                        product[i].price != order.product[i].price ||
                        product[i].quantity != order.product[i].quantity)
                    {
                        return false;
                    }
                }
            }
            else
            {
                Console.WriteLine("Ошибка! Одинаковый номер заказа для разных пользователей.");
            }
            return true;
        }

        /// <summary>
        /// Получение списка продуктов по id заказа
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        private async Task<List<Product>> GetProductListFromConcreteOrder(int orderId)
        {
            List<Product> products = new List<Product>();
            SqlCommand command = new SqlCommand();
            command.Connection = _connection;
            var quary = "Select g.goods_name, g.goods_price, b.goods_count From BasketList b" +
                "\r\nJoin Goods g on b.goods_id = g.id" +
                "\r\nwhere b.order_id = @orderId";
            command.Parameters.AddWithValue("@orderId", orderId);
            command.CommandText = quary;
            var response = await command.ExecuteReaderAsync();
            if (response.HasRows)
            {
                while (response.Read())
                {
                    products.Add(new Product
                    {
                        quantity = (int)response["goods_count"],
                        name = response.GetString("goods_name"),
                        price = response["goods_price"].ToString()
                    });
                }
                response.Close();
                return products;
            }
            response.Close();
            return null;
        }

        /// <summary>
        /// Обновление данных в таблице заказов и таблице продуктов
        /// </summary>
        /// <param name="product"></param>
        /// <param name="oderId"></param>
        private async void UpdateDataOfOrder(List<Product> product, int oderId)
        {
            SqlCommand command = new SqlCommand();
            command.Connection = _connection;
            var quary = "Begin transaction;";
            foreach (var item in product)
            {
                quary += "\nUpdate BasketList" +
                         "\nSet goods_count = @goodsCount" +
                         "\nWhere (order_id = @orderId) and (goods_id = @goodsId);" +
                         "\nUpdate Goods" +
                         "\nSet goods_price = @goodsPrice, goods_name = N'@goodsName'" +
                         "\nWhere id = @goodsId;";
                command.Parameters.AddWithValue("@goodsCount", item.quantity);
                command.Parameters.AddWithValue("@orderId", oderId);
                command.Parameters.AddWithValue("@goodsId", item.id);
                command.Parameters.AddWithValue("@goodsPrice", item.price);
                command.Parameters.AddWithValue("@goodsName", item.name);
            }
            quary += "\nCommit;";
            command.CommandText = quary;
            await command.ExecuteNonQueryAsync();
        }

    }
}
