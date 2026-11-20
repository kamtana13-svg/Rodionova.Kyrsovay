using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace магазин_игрушек
{
    public partial class список_покупок : Window
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=MagazinIgrushek;Integrated Security=True;";

        private readonly int currentId;
        private readonly string currentFio;

        private DataRowView selectedRow = null;

        public список_покупок(int id, string fio)
        {
            InitializeComponent();

            currentId = id;
            currentFio = fio;

            FioLabel.Content = fio;

            Loaded += Список_покупок_Loaded;
            ChecksGrid.SelectionChanged += ChecksGrid_SelectionChanged;
            ChecksGrid.MouseDoubleClick += ChecksGrid_MouseDoubleClick;
        }

        private void Список_покупок_Loaded(object sender, RoutedEventArgs e)
        {
            LoadChecks();
        }

        private void ChecksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedRow = ChecksGrid.SelectedItem as DataRowView;
        }

        private void ChecksGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (selectedRow == null) return;

            int idChek = Convert.ToInt32(selectedRow["Id_chek"]);
            new чеки(currentId, currentFio, idChek).Show();
            this.Close();
        }

        private void LoadChecks()
        {
            if (currentId == 0)
            {
                MessageBox.Show("Войдите в аккаунт, чтобы увидеть историю покупок.",
                    "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string query = @"
                SELECT c.Id_chek, c.Data, c.Itogovaya_stoimost
                FROM Chek c
                WHERE c.Id_pokupatel = @p
                ORDER BY c.Data DESC;";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@p", currentId);

                    conn.Open();
                    using (var adapter = new SqlDataAdapter(cmd))
                    {
                        var table = new DataTable();
                        adapter.Fill(table);
                        ChecksGrid.ItemsSource = table.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки покупок:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new покупатель(currentId, currentFio).Show();
            this.Close();
        }
    }
}