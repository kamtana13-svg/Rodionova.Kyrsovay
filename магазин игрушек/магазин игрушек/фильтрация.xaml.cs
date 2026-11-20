using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace магазин_игрушек
{
    public partial class фильтрация : Window
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=MagazinIgrushek;Integrated Security=True;";

        public static List<int> SelectedMaterialIds = new List<int>();
        public static List<int> SelectedFillerIds = new List<int>();
        public static List<int> SelectedProducerIds = new List<int>();
        public static decimal? SelectedPriceFrom = null;
        public static decimal? SelectedPriceTo = null;

        private readonly int currentId;
        private readonly string currentFio;

        // Соответствие "индекс чекбокса -> Id из БД"
        private readonly List<int> prodIds = new List<int>();
        private readonly List<int> matIds = new List<int>();
        private readonly List<int> napIds = new List<int>();

        public фильтрация() : this(0, null) { }

        public фильтрация(int id, string fio)
        {
            InitializeComponent();
            currentId = id;
            currentFio = fio;

            Loaded += Фильтрация_Loaded;
        }

        private void Фильтрация_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSpravochniki();
            RestoreChecks();
        }

        // Загружаем названия из БД и заполняем чекбоксы
        private void LoadSpravochniki()
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    LoadOne(conn,
                        "SELECT Id_proizvoditel, Nazvanie FROM Proizvoditel ORDER BY Id_proizvoditel",
                        new[] { Prod1, Prod2, Prod3, Prod4, Prod5 },
                        prodIds);

                    LoadOne(conn,
                        "SELECT Id_material, Nazvanie FROM Material ORDER BY Id_material",
                        new[] { Mat1, Mat2, Mat3, Mat4 },
                        matIds);

                    LoadOne(conn,
                        "SELECT Id_napolnitel, Nazvanie FROM Napolnitel ORDER BY Id_napolnitel",
                        new[] { Nap1, Nap2, Nap3, Nap4 },
                        napIds);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников:\n" + ex.Message);
            }
        }

        private void LoadOne(SqlConnection conn, string query,
                             CheckBox[] boxes, List<int> ids)
        {
            ids.Clear();

            var names = new List<string>();
            using (var cmd = new SqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    ids.Add(Convert.ToInt32(reader[0]));
                    names.Add(reader[1].ToString());
                }
            }

            for (int i = 0; i < boxes.Length; i++)
            {
                if (i < names.Count)
                {
                    boxes[i].Content = names[i];
                    boxes[i].Visibility = Visibility.Visible;
                }
                else
                {
                    // строк в БД меньше, чем чекбоксов — лишние прячем
                    boxes[i].Content = "";
                    boxes[i].Visibility = Visibility.Hidden;
                    boxes[i].IsChecked = false;
                }
            }
        }

        // Восстанавливаем галочки по ранее выбранным ID
        private void RestoreChecks()
        {
            for (int i = 0; i < prodIds.Count && i < 5; i++)
                GetBox(Prod1, Prod2, Prod3, Prod4, Prod5, i).IsChecked =
                    SelectedProducerIds.Contains(prodIds[i]);

            for (int i = 0; i < matIds.Count && i < 4; i++)
                GetBox(Mat1, Mat2, Mat3, Mat4, null, i).IsChecked =
                    SelectedMaterialIds.Contains(matIds[i]);

            for (int i = 0; i < napIds.Count && i < 4; i++)
                GetBox(Nap1, Nap2, Nap3, Nap4, null, i).IsChecked =
                    SelectedFillerIds.Contains(napIds[i]);

            if (SelectedPriceFrom.HasValue)
                PriceFromBox.Text = SelectedPriceFrom.Value.ToString();
            if (SelectedPriceTo.HasValue)
                PriceToBox.Text = SelectedPriceTo.Value.ToString();
        }

        private CheckBox GetBox(CheckBox b1, CheckBox b2, CheckBox b3, CheckBox b4, CheckBox b5, int i)
        {
            switch (i)
            {
                case 0: return b1;
                case 1: return b2;
                case 2: return b3;
                case 3: return b4;
                default: return b5;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (currentId == 0)
                new окно_гостя().Show();
            else
                new покупатель(currentId, currentFio).Show();

            this.Close();
        }

        private void Применить(object sender, RoutedEventArgs e)
        {
            // Собираем выбранные ID (по индексам чекбоксов)
            SelectedProducerIds = new List<int>();
            for (int i = 0; i < 5 && i < prodIds.Count; i++)
            {
                var box = GetBox(Prod1, Prod2, Prod3, Prod4, Prod5, i);
                if (box.IsChecked == true) SelectedProducerIds.Add(prodIds[i]);
            }

            SelectedMaterialIds = new List<int>();
            for (int i = 0; i < 4 && i < matIds.Count; i++)
            {
                var box = GetBox(Mat1, Mat2, Mat3, Mat4, null, i);
                if (box.IsChecked == true) SelectedMaterialIds.Add(matIds[i]);
            }

            SelectedFillerIds = new List<int>();
            for (int i = 0; i < 4 && i < napIds.Count; i++)
            {
                var box = GetBox(Nap1, Nap2, Nap3, Nap4, null, i);
                if (box.IsChecked == true) SelectedFillerIds.Add(napIds[i]);
            }

            SelectedPriceFrom = decimal.TryParse(
                PriceFromBox.Text.Trim().Replace(".", ","),
                out decimal from) ? from : (decimal?)null;

            SelectedPriceTo = decimal.TryParse(
                PriceToBox.Text.Trim().Replace(".", ","),
                out decimal to) ? to : (decimal?)null;

            if (currentId == 0)
                new окно_гостя().Show();
            else
                new покупатель(currentId, currentFio).Show();

            this.Close();
        }
    }
}