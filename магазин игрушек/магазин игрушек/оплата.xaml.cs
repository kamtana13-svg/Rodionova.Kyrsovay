using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;

namespace магазин_игрушек
{
    public partial class оплата : Window
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=MagazinIgrushek;Integrated Security=True;";

        private readonly int currentId;
        private readonly string currentFio;

        private int baseTotal = 0;
        private int finalTotal = 0;
        private bool bonusApplied = false;

        public оплата(int id, string fio)
        {
            InitializeComponent();
            currentId = id;
            currentFio = fio;
            Loaded += Оплата_Loaded;
        }

        private void Оплата_Loaded(object sender, RoutedEventArgs e) => CalculateTotal();

        private void CalculateTotal()
        {
            baseTotal = 0;
            bonusApplied = false;

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string q = @"
                        SELECT k.Kolichestvo, i.Cena, i.Kolichestvo_na_sklade,
                               ISNULL(a.Procent, 0) AS Procent
                        FROM Korzina k
                        JOIN Igrushki i ON k.Id_igrushka = i.Id_igrushki
                        LEFT JOIN Akciya a ON i.Id_akciya = a.Id_akciya
                        WHERE k.Id_pokupatel = @p;";
                    using (var cmd = new SqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@p", currentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int kolvo = Convert.ToInt32(reader["Kolichestvo"]);
                                decimal cena = Convert.ToDecimal(reader["Cena"]);
                                int sklad = Convert.ToInt32(reader["Kolichestvo_na_sklade"]);
                                int procent = Convert.ToInt32(reader["Procent"]);

                                int cenaRub = (int)Math.Round(cena, 0, MidpointRounding.AwayFromZero);
                                if (sklad <= 10 && procent > 0)
                                    cenaRub = (int)Math.Round(cenaRub * (1 - procent / 100.0), 0,
                                        MidpointRounding.AwayFromZero);

                                baseTotal += cenaRub * kolvo;
                            }
                        }
                    }
                }

                finalTotal = baseTotal;
                TotalBox.Text = $"{finalTotal} ₽";
            }
            catch (Exception ex) { MessageBox.Show("Ошибка подсчёта:\n" + ex.Message); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            new корзина(currentId, currentFio).Show();
            this.Close();
        }

        private void Оплатить(object sender, RoutedEventArgs e)
        {
            if (baseTotal == 0) { MessageBox.Show("Корзина пуста."); return; }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            int itog = 0;
                            var pozicii = new List<Tuple<int, int, int>>();

                            string q = @"
                                SELECT k.Id_igrushka, k.Kolichestvo,
                                       i.Cena, i.Kolichestvo_na_sklade,
                                       ISNULL(a.Procent, 0) AS Procent
                                FROM Korzina k
                                JOIN Igrushki i ON k.Id_igrushka = i.Id_igrushki
                                LEFT JOIN Akciya a ON i.Id_akciya = a.Id_akciya
                                WHERE k.Id_pokupatel = @p;";
                            using (var cmd = new SqlCommand(q, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@p", currentId);
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        int id = (int)reader["Id_igrushka"];
                                        int kolvo = Convert.ToInt32(reader["Kolichestvo"]);
                                        decimal cena = Convert.ToDecimal(reader["Cena"]);
                                        int sklad = Convert.ToInt32(reader["Kolichestvo_na_sklade"]);
                                        int procent = Convert.ToInt32(reader["Procent"]);

                                        int cenaRub = (int)Math.Round(cena, 0, MidpointRounding.AwayFromZero);
                                        if (sklad <= 10 && procent > 0)
                                            cenaRub = (int)Math.Round(cenaRub * (1 - procent / 100.0), 0,
                                                MidpointRounding.AwayFromZero);

                                        itog += cenaRub * kolvo;
                                        pozicii.Add(Tuple.Create(id, kolvo, cenaRub));
                                    }
                                }
                            }

                            int itogFinal = bonusApplied
                                ? (int)Math.Round(itog * 0.9, 0, MidpointRounding.AwayFromZero)
                                : itog;

                            int idChek;
                            string insertChek = @"
                                INSERT INTO Chek (Id_pokupatel, Id_oplata, Data, Itogovaya_stoimost)
                                VALUES (@p, 1, GETDATE(), @s);
                                SELECT CAST(SCOPE_IDENTITY() AS INT);";
                            using (var cmd = new SqlCommand(insertChek, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@p", currentId);
                                cmd.Parameters.AddWithValue("@s", itogFinal);
                                idChek = (int)cmd.ExecuteScalar();
                            }

                            foreach (var p in pozicii)
                            {
                                string ins = @"
                                    INSERT INTO Poziciya_cheka
                                        (Id_chek, Id_igrushka, Kolichestvo, Cena_na_moment)
                                    VALUES (@c, @i, @k, @cen);";
                                using (var cmd = new SqlCommand(ins, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@c", idChek);
                                    cmd.Parameters.AddWithValue("@i", p.Item1);
                                    cmd.Parameters.AddWithValue("@k", p.Item2);
                                    cmd.Parameters.AddWithValue("@cen", p.Item3);
                                    cmd.ExecuteNonQuery();
                                }

                                string upd = @"
                                    UPDATE Igrushki
                                    SET Kolichestvo_na_polke = 
                                            CASE WHEN Kolichestvo_na_polke >= @k
                                                 THEN Kolichestvo_na_polke - @k
                                                 ELSE 0 END,
                                        Kolichestvo_na_sklade = 
                                            CASE WHEN Kolichestvo_na_sklade >= @k
                                                 THEN Kolichestvo_na_sklade - @k
                                                 ELSE 0 END
                                    WHERE Id_igrushki = @i;";
                                using (var cmd = new SqlCommand(upd, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@k", p.Item2);
                                    cmd.Parameters.AddWithValue("@i", p.Item1);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            string clear = @"DELETE FROM Korzina WHERE Id_pokupatel = @p;";
                            using (var cmd = new SqlCommand(clear, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@p", currentId);
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            MessageBox.Show(
                                $"Оплата прошла успешно!\nЧек №{idChek}\nИтого: {itogFinal} ₽",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                            new покупатель(currentId, currentFio).Show();
                            this.Close();
                        }
                        catch { transaction.Rollback(); throw; }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка оплаты:\n" + ex.Message); }
        }
    }
}