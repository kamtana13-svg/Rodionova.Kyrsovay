using System;
using System.Data.SqlClient;
using System.Windows;

namespace магазин_игрушек
{
    public partial class MainWindow : Window
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=MagazinIgrushek;Integrated Security=True;";

        public MainWindow() { InitializeComponent(); }

        private void Войти(object sender, RoutedEventArgs e)
        {
            string login = LoginBox.Text.Trim();

            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин!");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Поиск логина без учёта регистра
                    string q = @"SELECT Id_pokupatel, Fio FROM Pokupatel
                                 WHERE LOWER(Login) = LOWER(@l);";

                    using (var cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@l", login);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Пользователь с таким логином не найден!");
                                return;
                            }

                            int id = (int)reader["Id_pokupatel"];
                            string fio = reader["Fio"].ToString();

                            new покупатель(id, fio).Show();
                            this.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка входа:\n" + ex.Message);
            }
        }

        private void Зарегестрироваться(object sender, RoutedEventArgs e)
        {
            new окно_регестрации().Show();
            this.Close();
        }

        private void Войти_как_гость(object sender, RoutedEventArgs e)
        {
            new окно_гостя().Show();
            this.Close();
        }
    }
}