using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace магазин_игрушек
{
    public partial class корзина : Window
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=MagazinIgrushek;Integrated Security=True;";

        private readonly int currentId;
        private readonly string currentFio;

        private DataTable cartTable = new DataTable();
        private DataRowView selectedCartRow = null;

        public корзина(int id, string fio)
        {
            InitializeComponent();
            currentId = id;
            currentFio = fio;
            FioLabel.Content = fio;

            Loaded += Корзина_Loaded;
            CartGrid.SelectionChanged += CartGrid_SelectionChanged;

            // Двойной клик по строке — удаление
            CartGrid.MouseDoubleClick += CartGrid_MouseDoubleClick;
        }

        private void Корзина_Loaded(object sender, RoutedEventArgs e) => LoadCart();

        private void LoadCart()
        {
            cartTable = new DataTable();
            cartTable.Columns.Add("Id_igrushki", typeof(int));
            cartTable.Columns.Add("Name");
            cartTable.Columns.Add("Price");
            cartTable.Columns.Add("Kolichestvo", typeof(int));
            cartTable.Columns.Add("Summa");
            cartTable.Columns.Add("CenaRaw", typeof(int));

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string q = @"
                        SELECT k.Id_igrushka, i.Nazvanie, i.Cena,
                               i.Kolichestvo_na_sklade, k.Kolichestvo,
                               ISNULL(a.Procent, 0) AS Procent
                        FROM Korzina k
                        JOIN Igrushki i ON k.Id_igrushka = i.Id_igrushki
                        LEFT JOIN Akciya a ON i.Id_akciya = a.Id_akciya
                        WHERE k.Id_pokupatel = @p
                        ORDER BY i.Nazvanie;";

                    using (var cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@p", currentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int id = (int)reader["Id_igrushka"];
                                string name = reader["Nazvanie"].ToString();
                                decimal cena = Convert.ToDecimal(reader["Cena"]);
                                int sklad = Convert.ToInt32(reader["Kolichestvo_na_sklade"]);
                                int kolvo = Convert.ToInt32(reader["Kolichestvo"]);
                                int procent = Convert.ToInt32(reader["Procent"]);

                                int cenaRub = (int)Math.Round(cena, 0, MidpointRounding.AwayFromZero);
                                if (sklad <= 10 && procent > 0)
                                    cenaRub = (int)Math.Round(cenaRub * (1 - procent / 100.0), 0,
                                        MidpointRounding.AwayFromZero);

                                int summa = cenaRub * kolvo;

                                cartTable.Rows.Add(
                                    id, name, $"{cenaRub} ₽", kolvo, $"{summa} ₽", cenaRub);
                            }
                        }
                    }
                }

                CartGrid.ItemsSource = cartTable.DefaultView;
                UpdateTotal();
            }
            catch (Exception ex) { MessageBox.Show("Ошибка корзины:\n" + ex.Message); }
        }

        private void UpdateTotal()
        {
            int total = 0;
            foreach (DataRow r in cartTable.Rows)
                total += Convert.ToInt32(r["CenaRaw"]) * Convert.ToInt32(r["Kolichestvo"]);
            TotalBox.Text = $"{total} ₽";
        }

        private void CartGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedCartRow = CartGrid.SelectedItem as DataRowView;
        }

        // === ДВОЙНОЙ КЛИК ПО ТОВАРУ — УДАЛЕНИЕ ===
        private void CartGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (selectedCartRow == null) return;

            int id = Convert.ToInt32(selectedCartRow["Id_igrushki"]);
            string name = selectedCartRow["Name"].ToString();

            var result = MessageBox.Show(
                $"Удалить «{name}» из корзины?",
                "Удаление",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string q = @"DELETE FROM Korzina
                                 WHERE Id_pokupatel = @p AND Id_igrushka = @i;";
                    using (var cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@p", currentId);
                        cmd.Parameters.AddWithValue("@i", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                LoadCart(); // перезагружаем таблицу
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления:\n" + ex.Message);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new покупатель(currentId, currentFio).Show();
            this.Close();
        }

        private void добавить(object sender, RoutedEventArgs e)
        {
            if (selectedCartRow == null) { MessageBox.Show("Выберите товар."); return; }
            int id = Convert.ToInt32(selectedCartRow["Id_igrushki"]);
            int kolvo = Convert.ToInt32(selectedCartRow["Kolichestvo"]) + 1;
            UpdateKorzinaDb(id, kolvo);
            LoadCart();
        }

        private void убрать(object sender, RoutedEventArgs e)
        {
            if (selectedCartRow == null) { MessageBox.Show("Выберите товар."); return; }
            int id = Convert.ToInt32(selectedCartRow["Id_igrushki"]);
            int kolvo = Convert.ToInt32(selectedCartRow["Kolichestvo"]) - 1;

            if (kolvo <= 0) DeleteFromKorzinaDb(id);
            else UpdateKorzinaDb(id, kolvo);

            LoadCart();
        }

        private void UpdateKorzinaDb(int idIgrushka, int kolvo)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string q = @"UPDATE Korzina SET Kolichestvo = @k
                             WHERE Id_pokupatel = @p AND Id_igrushka = @i;";
                using (var cmd = new SqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@k", kolvo);
                    cmd.Parameters.AddWithValue("@p", currentId);
                    cmd.Parameters.AddWithValue("@i", idIgrushka);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void DeleteFromKorzinaDb(int idIgrushka)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string q = @"DELETE FROM Korzina
                             WHERE Id_pokupatel = @p AND Id_igrushka = @i;";
                using (var cmd = new SqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@p", currentId);
                    cmd.Parameters.AddWithValue("@i", idIgrushka);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void добавить_в_корзину(object sender, RoutedEventArgs e)
        {
            if (cartTable.Rows.Count == 0)
            { MessageBox.Show("Корзина пуста."); return; }

            new оплата(currentId, currentFio).Show();
            this.Close();
        }
    }
}