using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace магазин_игрушек
{
    public partial class чеки : Window
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=MagazinIgrushek;Integrated Security=True;";


        private static readonly string ImagesFolder =
            @"C:\Users\Пользователь\Desktop\учеба\4 курс\курсовая федосеева 2\игрушки";

        private readonly int currentId;
        private readonly string currentFio;
        private readonly int chekId;

        private TextBlock dateText;
        private TextBlock timeText;
        private TextBlock totalText;

        public чеки(int id, string fio, int idChek)
        {
            InitializeComponent();

            currentId = id;
            currentFio = fio;
            chekId = idChek;

            Loaded += Чеки_Loaded;
        }

        public чеки() : this(0, null, 0) { }

        private void Чеки_Loaded(object sender, RoutedEventArgs e)
        {
            FindElements();
            LoadChek();
        }

        // TextBlock'и внизу — дата, время, итог. Ищем по порядку в Grid.Children.
        private void FindElements()
        {
            if (!(this.Content is Grid grid)) return;

            int tbIndex = 0;
            foreach (var child in grid.Children)
            {
                if (child is TextBlock tb)
                {
                    // TextBlock'и в XAML:
                    // 0 — дата
                    // 1 — время
                    // 2 — итог
                    if (tbIndex == 0) dateText = tb;
                    else if (tbIndex == 1) timeText = tb;
                    else if (tbIndex == 2) totalText = tb;
                    tbIndex++;
                }
            }
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

        private void LoadChek()
        {
            if (chekId == 0)
            {
                MessageBox.Show("Чек не выбран.");
                this.Close();
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 1) Дата и итог чека
                    DateTime date = DateTime.MinValue;
                    decimal total = 0;

                    string qChek = @"SELECT Data, Itogovaya_stoimost
                                     FROM Chek WHERE Id_chek = @c;";
                    using (var cmd = new SqlCommand(qChek, conn))
                    {
                        cmd.Parameters.AddWithValue("@c", chekId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                date = Convert.ToDateTime(reader["Data"]);
                                total = Convert.ToDecimal(reader["Itogovaya_stoimost"]);
                            }
                        }
                    }

                    if (dateText != null)
                        dateText.Text = date.ToString("dd.MM.yyyy");

                    if (timeText != null)
                        timeText.Text = date.ToString("HH:mm");

                    if (totalText != null)
                        totalText.Text = $"{total:N0} ₽";

                    // 2) Позиции чека
                    var view = new DataTable();
                    view.Columns.Add("Name");
                    view.Columns.Add("Category");
                    view.Columns.Add("Quantity");
                    view.Columns.Add("Composition");
                    view.Columns.Add("Price");
                    view.Columns.Add("FotoPath", typeof(BitmapImage));

                    string qPos = @"
                        SELECT i.Nazvanie, i.Opisanie, i.Foto,
                               ISNULL(m.Nazvanie, N'—') AS MaterialName,
                               ISNULL(n.Nazvanie, N'—') AS NapolnitelName,
                               ISNULL(p.Nazvanie, N'—') AS ProizvoditelName,
                               pc.Kolichestvo, pc.Cena_na_moment
                        FROM Poziciya_cheka pc
                        JOIN Igrushki i ON pc.Id_igrushka = i.Id_igrushki
                        LEFT JOIN Material     m ON i.Id_material     = m.Id_material
                        LEFT JOIN Napolnitel   n ON i.Id_napolnitel   = n.Id_napolnitel
                        LEFT JOIN Proizvoditel p ON i.Id_proizvoditel = p.Id_proizvoditel
                        WHERE pc.Id_chek = @c
                        ORDER BY pc.Id_poziciya_cheka;";

                    using (var cmd = new SqlCommand(qPos, conn))
                    {
                        cmd.Parameters.AddWithValue("@c", chekId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string name = reader["Nazvanie"].ToString();
                                string opis = reader["Opisanie"] == DBNull.Value
                                    ? "" : reader["Opisanie"].ToString();
                                string foto = reader["Foto"] == DBNull.Value
                                    ? null : reader["Foto"].ToString();

                                string material = reader["MaterialName"].ToString();
                                string filler = reader["NapolnitelName"].ToString();
                                string producer = reader["ProizvoditelName"].ToString();

                                int kolvo = Convert.ToInt32(reader["Kolichestvo"]);
                                decimal price = Convert.ToDecimal(reader["Cena_na_moment"]);
                                decimal summa = price * kolvo;

                                // Name, Category, Quantity, Composition, Price, FotoPath
                                view.Rows.Add(
                                    name,
                                    $"Материал: {material}",
                                    $"Кол-во: {kolvo} шт",
                                    $"Наполнитель: {filler} | Производитель: {producer}",
                                    $"{summa:N0} ₽",
                                    LoadImageByFoto(foto));
                            }
                        }
                    }

                    PositionsList.ItemsSource = view.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки чека:\n" + ex.Message);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new список_покупок(currentId, currentFio).Show();
            this.Close();
        }
    }
}