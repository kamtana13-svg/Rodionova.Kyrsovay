using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace магазин_игрушек
{
    public partial class окно_гостя : Window
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=MagazinIgrushek;Integrated Security=True;";

        private static readonly string ImagesFolder =
            @"C:\Users\Пользователь\Desktop\учеба\4 курс\курсовая федосеева 2\игрушки";

        private DataTable allProducts = new DataTable();
        private DataRowView selectedRow = null;

        public окно_гостя()
        {
            InitializeComponent();
            Loaded += Окно_гостя_Loaded;
            ProductsList.SelectionChanged += ProductsList_SelectionChanged;
            ProductsList.MouseDoubleClick += ProductsList_MouseDoubleClick;
        }

        private void Окно_гостя_Loaded(object sender, RoutedEventArgs e) => LoadProducts();

        private void ProductsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (selectedRow == null) return;

            int id = Convert.ToInt32(selectedRow["Id_igrushki"]);
            int polka = Convert.ToInt32(selectedRow["PolkaRaw"]);
            int sklad = Convert.ToInt32(selectedRow["SkladRaw"]);

            if (polka + sklad <= 0)
            {
                MessageBox.Show("Товара нет на складе.",
                    "Нет в наличии", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            new карточка_гостя(id).Show();
            this.Close();
        }

        private void ProductsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedRow = ProductsList.SelectedItem as DataRowView;
        }

        private BitmapImage LoadImageByFoto(string foto)
        {
            if (string.IsNullOrWhiteSpace(foto)) return null;

            string path = Path.Combine(ImagesFolder, foto);
            if (!File.Exists(path)) return null;

            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        private void LoadProducts()
        {
            string where = " WHERE 1=1 ";

            if (фильтрация.SelectedMaterialIds != null && фильтрация.SelectedMaterialIds.Count > 0)
                where += " AND i.Id_material IN (" + string.Join(",", фильтрация.SelectedMaterialIds) + ") ";

            if (фильтрация.SelectedFillerIds != null && фильтрация.SelectedFillerIds.Count > 0)
                where += " AND i.Id_napolnitel IN (" + string.Join(",", фильтрация.SelectedFillerIds) + ") ";

            if (фильтрация.SelectedProducerIds != null && фильтрация.SelectedProducerIds.Count > 0)
                where += " AND i.Id_proizvoditel IN (" + string.Join(",", фильтрация.SelectedProducerIds) + ") ";

            if (фильтрация.SelectedPriceFrom.HasValue)
                where += " AND i.Cena >= @from ";

            if (фильтрация.SelectedPriceTo.HasValue)
                where += " AND i.Cena <= @to ";

            string q = @"
                SELECT i.Id_igrushki, i.Nazvanie, i.Foto,
                       i.Kolichestvo_na_polke, i.Kolichestvo_na_sklade, i.Cena,
                       ISNULL(m.Nazvanie, N'—') AS MaterialName,
                       ISNULL(n.Nazvanie, N'—') AS NapolnitelName,
                       ISNULL(p.Nazvanie, N'—') AS ProizvoditelName,
                       ISNULL(a.Procent, 0)     AS Procent
                FROM Igrushki i
                LEFT JOIN Material     m ON i.Id_material     = m.Id_material
                LEFT JOIN Napolnitel   n ON i.Id_napolnitel   = n.Id_napolnitel
                LEFT JOIN Proizvoditel p ON i.Id_proizvoditel = p.Id_proizvoditel
                LEFT JOIN Akciya       a ON i.Id_akciya       = a.Id_akciya
                " + where + " ORDER BY i.Nazvanie;";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(q, conn))
                {
                    if (фильтрация.SelectedPriceFrom.HasValue)
                        cmd.Parameters.AddWithValue("@from", фильтрация.SelectedPriceFrom.Value);
                    if (фильтрация.SelectedPriceTo.HasValue)
                        cmd.Parameters.AddWithValue("@to", фильтрация.SelectedPriceTo.Value);

                    conn.Open();
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        allProducts = new DataTable();
                        adapter.Fill(allProducts);
                    }
                }
                Render();
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки:\n" + ex.Message); }
        }

        private void Render()
        {
            string search = SearchBox?.Text?.Trim().ToLower() ?? "";
            var view = new DataTable();
            view.Columns.Add("Id_igrushki", typeof(int));
            view.Columns.Add("Name");
            view.Columns.Add("Category");
            view.Columns.Add("Quantity");
            view.Columns.Add("Composition");
            view.Columns.Add("Price");
            view.Columns.Add("PriceColor", typeof(Brush));
            view.Columns.Add("FotoPath", typeof(BitmapImage));
            view.Columns.Add("PolkaRaw", typeof(int));
            view.Columns.Add("SkladRaw", typeof(int));

            foreach (DataRow r in allProducts.Rows)
            {
                string name = r["Nazvanie"].ToString();
                if (!string.IsNullOrEmpty(search) && !name.ToLower().Contains(search)) continue;

                int id = Convert.ToInt32(r["Id_igrushki"]);
                int polka = Convert.ToInt32(r["Kolichestvo_na_polke"]);
                int sklad = Convert.ToInt32(r["Kolichestvo_na_sklade"]);
                decimal cena = Convert.ToDecimal(r["Cena"]);
                int procent = Convert.ToInt32(r["Procent"]);

                bool noStock = (polka + sklad) <= 0;

                string priceText;
                Brush priceColor;

                if (noStock)
                {
                    priceText = "0 ₽";
                    priceColor = Brushes.Red;
                }
                else
                {
                    int cenaRub = (int)Math.Round(cena, 0, MidpointRounding.AwayFromZero);
                    priceText = $"{cenaRub} ₽";
                    priceColor = Brushes.Black;

                    if (sklad <= 10 && procent > 0)
                    {
                        int final = (int)Math.Round(cenaRub * (1 - procent / 100.0), 0,
                            MidpointRounding.AwayFromZero);
                        priceText = $"{final} ₽  (-{procent}%)";
                    }
                }

                BitmapImage fotoBmp = null;
                if (!noStock)
                {
                    string foto = r["Foto"] == DBNull.Value ? null : r["Foto"].ToString();
                    fotoBmp = LoadImageByFoto(foto);
                }

                view.Rows.Add(
                    id, name,
                    $"Материал: {r["MaterialName"]}",
                    $"На полке: {polka} | На складе: {sklad}",
                    $"Наполнитель: {r["NapolnitelName"]} | Производитель: {r["ProizvoditelName"]}",
                    priceText,
                    priceColor,
                    fotoBmp,
                    polka,
                    sklad);
            }

            ProductsList.ItemsSource = view.DefaultView;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Render();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }

        private void Фильтрация(object sender, RoutedEventArgs e)
        {
            new фильтрация(0, null).Show();
            this.Close();
        }
    }
}