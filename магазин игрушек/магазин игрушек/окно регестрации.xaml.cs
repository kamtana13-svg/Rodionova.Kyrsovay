using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace магазин_игрушек
{
    public partial class окно_регестрации : Window
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=MagazinIgrushek;Integrated Security=True;";

        public окно_регестрации() { InitializeComponent(); }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }

        // Только цифры (буквы, пробелы, знаки — запрещены)
        private void PhoneBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        // Если поле пустое или без "+7" — подставляем "+7", курсор в конец
        private void PhoneBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PhoneBox.Text) ||
                !PhoneBox.Text.StartsWith("+7"))
            {
                PhoneBox.Text = "+7";
            }
            PhoneBox.CaretIndex = PhoneBox.Text.Length;
        }

        private void Войти(object sender, RoutedEventArgs e)
        {
            string fio = FioBox.Text.Trim();

            // Приводим телефон к виду +7XXXXXXXXXX
            string phone = (PhoneBox.Text ?? "").Trim();
            if (!phone.StartsWith("+7"))
                phone = "+7" + phone.Replace("+7", "");

            string login = LoginBox.Text.Trim();

            // Проверки
            if (string.IsNullOrEmpty(fio) || string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Заполните ФИО и логин!");
                return;
            }

            if (login.Length < 3)
            {
                MessageBox.Show("Логин должен быть не короче 3 символов.");
                return;
            }

            if (phone.Length != 12) // +7 и 10 цифр
            {
                MessageBox.Show("Введите номер телефона полностью: +7 и 10 цифр.");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Проверка уникальности логина без учёта регистра
                    string check = @"SELECT COUNT(*) FROM Pokupatel
                                     WHERE LOWER(Login) = LOWER(@l);";
                    using (var cmd = new SqlCommand(check, conn))
                    {
                        cmd.Parameters.AddWithValue("@l", login);
                        if ((int)cmd.ExecuteScalar() > 0)
                        {
                            MessageBox.Show($"Логин «{login}» уже занят. Придумайте другой.");
                            return;
                        }
                    }

                    // Вставка покупателя
                    string ins = @"INSERT INTO Pokupatel (Fio, Nomer_telefona, Login)
                                   VALUES (@f, @p, @l);";
                    using (var cmd = new SqlCommand(ins, conn))
                    {
                        cmd.Parameters.AddWithValue("@f", fio);
                        cmd.Parameters.AddWithValue("@p", phone);
                        cmd.Parameters.AddWithValue("@l", login);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Регистрация успешна! Войдите под своим логином.");
                new MainWindow().Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка регистрации:\n" + ex.Message);
            }
        }
    }
}