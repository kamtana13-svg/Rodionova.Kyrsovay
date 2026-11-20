using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace магазин_игрушек
{
    public partial class карточка : Window
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=MagazinIgrushek;Integrated Security=True;";

        private static readonly string ImagesFolder =
            @"C:\Users\Пользователь\Desktop\учеба\4 курс\курсовая федосеева 2\игрушки";

        private readonly int currentId;
        private readonly string currentFio;
        private readonly int toyId;

        private int available = 0;
        private int count = 1;
        private int priceRub = 0;
        private int priceBase = 0;

        private Image toyImage;
        private Label nameLabel;
        private Label countLabel;
        private Label sumLabel;
        private TextBlock descriptionText;
        private TextBlock producerText;
        private TextBlock fillerText;
        private TextBlock materialText;
        private TextBlock quantityText;

        // Лейбл «Наполнитель:» — его тоже надо скрывать
        private Label fillerLabel;

        public карточка(int userId, string fio, int idIgrushki)
        {
            InitializeComponent();
            currentId = userId;
            currentFio = fio;
            toyId = idIgrushki;

            Loaded += Карточка_Loaded;
        }

        public карточка() : this(0, null, 0) { }

        private void Карточка_Loaded(object sender, RoutedEventArgs e)
        {
            FindElements();
            LoadToy();
        }

        private void FindElements()
        {
            if (!(this.Content is Grid grid)) return;

            int tbIndex = 0;
            int labelIndex = 0;

            foreach (var child in grid.Children)
            {
                if (child is TextBlock tb)
                {
                    switch (tbIndex)
                    {
                        case 0: descriptionText = tb; break;
                        case 1: producerText = tb; break;
                        case 2: fillerText = tb; break;
                        case 3: materialText = tb; break;
                        case 4: quantityText = tb; break;
                    }
                    tbIndex++;
                }
                else if (child is Label lbl)
                {
                    switch (labelIndex)
                    {
                        case 2: fillerLabel = lbl; break;  // «Наполнитель:»
                        case 5: nameLabel = lbl; break;
                        case 6: countLabel = lbl; break;
                        case 7: sumLabel = lbl; break;
                    }
                    labelIndex++;
                }
                else if (child is Image img)
                {
                    toyImage = img;
                }
            }
        }

        private void LoadToy()
        {
            if (toyId == 0)
            {
                MessageBox.Show("Товар не выбран.");
                this.Close();
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string q = @"
                        SELECT i.Nazvanie, i.Opisanie, i.Cena, i.Foto,
                               i.Kolichestvo_na_polke, i.Kolichestvo_na_sklade,
                               i.Id_napolnitel,
                               ISNULL(m.Nazvanie, N'—') AS MaterialName,
                               n.Nazvanie AS NapolnitelName,
                               ISNULL(p.Nazvanie, N'—') AS ProizvoditelName,
                               ISNULL(a.Procent, 0)     AS Procent
                        FROM Igrushki i
                        LEFT JOIN Material     m ON i.Id_material     = m.Id_material
                        LEFT JOIN Napolnitel   n ON i.Id_napolnitel   = n.Id_napolnitel
                        LEFT JOIN Proizvoditel p ON i.Id_proizvoditel = p.Id_proizvoditel
                        LEFT JOIN Akciya       a ON i.Id_akciya       = a.Id_akciya
                        WHERE i.Id_igrushki = @i;";

                    using (var cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@i", toyId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Товар не найден.");
                                this.Close();
                                return;
                            }

                            string name = reader["Nazvanie"].ToString();
                            string opis = reader["Opisanie"] == DBNull.Value
                                ? "" : reader["Opisanie"].ToString();
                            string foto = reader["Foto"] == DBNull.Value
                                ? null : reader["Foto"].ToString();

                            decimal cena = Convert.ToDecimal(reader["Cena"]);
                            int polka = Convert.ToInt32(reader["Kolichestvo_na_polke"]);
                            int sklad = Convert.ToInt32(reader["Kolichestvo_na_sklade"]);
                            int procent = Convert.ToInt32(reader["Procent"]);

                            // Наполнитель: NULL или строка
                            bool hasFiller = reader["Id_napolnitel"] != DBNull.Value;
                            string fillerName = hasFiller
                                ? reader["NapolnitelName"].ToString()
                                : null;

                            available = polka + sklad;

                            if (available <= 0)
                            {
                                MessageBox.Show("Товара нет на складе.",
                                    "Нет в наличии",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                this.Close();
                                return;
                            }

                            priceBase = (int)Math.Round(cena, 0, MidpointRounding.AwayFromZero);
                            priceRub = priceBase;

                            if (available <= 10)
                            {
                                int discount = procent > 0 ? procent : 10;
                                priceRub = (int)Math.Round(
                                    priceBase * (1 - discount / 100.0), 0,
                                    MidpointRounding.AwayFromZero);
                            }

                            string priceText = priceRub == priceBase
                                ? $"{priceRub} ₽"
                                : $"{priceRub} ₽ (было {priceBase} ₽)";

                            if (nameLabel != null)
                                nameLabel.Content = $"{name} — {priceText}";

                            if (descriptionText != null)
                                descriptionText.Text = string.IsNullOrEmpty(opis)
                                    ? "Описание отсутствует."
                                    : opis;

                            if (producerText != null)
                                producerText.Text = reader["ProizvoditelName"].ToString();

                            if (materialText != null)
                                materialText.Text = reader["MaterialName"].ToString();

                            if (quantityText != null)
                                quantityText.Text = $"на полке {polka} / на складе {sklad}";

                            // Наполнитель: показываем только если есть
                            if (hasFiller)
                            {
                                if (fillerLabel != null) fillerLabel.Visibility = Visibility.Visible;
                                if (fillerText != null)
                                {
                                    fillerText.Visibility = Visibility.Visible;
                                    fillerText.Text = fillerName;
                                }
                            }
                            else
                            {
                                if (fillerLabel != null) fillerLabel.Visibility = Visibility.Collapsed;
                                if (fillerText != null) fillerText.Visibility = Visibility.Collapsed;
                            }

                            if (toyImage != null)
                            {
                                toyImage.Source = LoadImageByFoto(foto);
                                toyImage.Stretch = System.Windows.Media.Stretch.Uniform;
                            }

                            int already = GetKorzinaCountFromDb(toyId);
                            count = already > 0 ? already : 1;
                            if (count > available) count = available;
                            if (count < 1) count = 1;

                            UpdateCountAndSum();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки товара:\n" + ex.Message);
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

        private int GetKorzinaCountFromDb(int idIgrushka)
        {
            if (currentId == 0) return 0;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string q = @"SELECT Kolichestvo FROM Korzina
                                 WHERE Id_pokupatel = @p AND Id_igrushka = @i;";
                    using (var cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@p", currentId);
                        cmd.Parameters.AddWithValue("@i", idIgrushka);
                        var r = cmd.ExecuteScalar();
                        if (r == null || r == DBNull.Value) return 0;
                        return Convert.ToInt32(r);
                    }
                }
            }
            catch { return 0; }
        }

        private void UpdateCountAndSum()
        {
            if (countLabel != null) countLabel.Content = count.ToString();
            if (sumLabel != null) sumLabel.Content = $"{priceRub * count} ₽";
        }

        private void добавить(object sender, RoutedEventArgs e)
        {
            if (count + 1 > available)
            {
                MessageBox.Show($"Больше {available} шт. купить нельзя.",
                    "Недостаточно товара", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            count++;
            UpdateCountAndSum();
        }

        private void убрать(object sender, RoutedEventArgs e)
        {
            if (count - 1 < 1)
            {
                MessageBox.Show("Минимальное количество — 1 шт.");
                return;
            }
            count--;
            UpdateCountAndSum();
        }

        private void добавить_в_корзину(object sender, RoutedEventArgs e)
        {
            if (currentId == 0)
            {
                MessageBox.Show("Гости не могут добавлять товары в корзину.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (count < 1 || count > available)
            {
                MessageBox.Show($"Доступно {available} шт.");
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string check = @"SELECT COUNT(*) FROM Korzina
                                     WHERE Id_pokupatel = @p AND Id_igrushka = @i;";
                    int exists;
                    using (var cmd = new SqlCommand(check, conn))
                    {
                        cmd.Parameters.AddWithValue("@p", currentId);
                        cmd.Parameters.AddWithValue("@i", toyId);
                        exists = (int)cmd.ExecuteScalar();
                    }

                    if (exists == 0)
                    {
                        string ins = @"INSERT INTO Korzina (Id_pokupatel, Id_igrushka, Kolichestvo)
                                       VALUES (@p, @i, @k);";
                        using (var cmd = new SqlCommand(ins, conn))
                        {
                            cmd.Parameters.AddWithValue("@p", currentId);
                            cmd.Parameters.AddWithValue("@i", toyId);
                            cmd.Parameters.AddWithValue("@k", count);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string upd = @"UPDATE Korzina SET Kolichestvo = @k
                                       WHERE Id_pokupatel = @p AND Id_igrushka = @i;";
                        using (var cmd = new SqlCommand(upd, conn))
                        {
                            cmd.Parameters.AddWithValue("@p", currentId);
                            cmd.Parameters.AddWithValue("@i", toyId);
                            cmd.Parameters.AddWithValue("@k", count);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show($"Добавлено в корзину — {count} шт. на {priceRub * count} ₽");

                new покупатель(currentId, currentFio).Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка корзины:\n" + ex.Message);
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
    }
}