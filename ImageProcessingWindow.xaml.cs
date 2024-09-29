using Npgsql;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace Сontact_Angle_Meter
{
    /// <summary>
    /// Логика взаимодействия для ImageProcessingWindow.xaml
    /// </summary>
    public partial class ImageProcessingWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=NaPoLiBon;Database=contactAngles_db";
        private double angle;

        public ImageProcessingWindow(BitmapSource image, double angle)
        {
            InitializeComponent();
            this.angle = angle;
            LoadWindow(image);
        }

        private void LoadWindow(BitmapSource image)
        {
            textInsertLiquid.Visibility = Visibility.Hidden;
            textInsertMaterial.Visibility = Visibility.Hidden;
            tbLiquidName.Visibility = Visibility.Hidden;
            tbMaterialName.Visibility = Visibility.Hidden;
            btnWriteToDB.Visibility = Visibility.Hidden;

            processedImageBox.Source = image;
            ContactAngleValueLabel.Content = "Краевой угол смачивания = " + angle + "°";
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            textInsertLiquid.Visibility = Visibility.Visible;
            textInsertMaterial.Visibility = Visibility.Visible;
            tbLiquidName.Visibility = Visibility.Visible;
            tbMaterialName.Visibility = Visibility.Visible;
            btnWriteToDB.Visibility = Visibility.Visible;
        }

        private void btnWriteToDB_Click(object sender, RoutedEventArgs e)
        {
            string liquid = tbLiquidName.Text.Trim();
            string material = tbMaterialName.Text.Trim();

            if (string.IsNullOrEmpty(liquid) || string.IsNullOrEmpty(material))
            {
                MessageBox.Show("Пожалуйста, введите корректные значения для всех полей.", "Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // Вставляем новую жидкость, если её нет
                string selectLiquidQuery = "SELECT id FROM liquids WHERE name = @name";
                string insertLiquidQuery = "INSERT INTO liquids (name) VALUES (@name)";
                long liquidId = InsertOrGetId(connection, selectLiquidQuery, insertLiquidQuery, liquid);

                // Вставляем новый материал, если его нет
                string selectMaterialQuery = "SELECT id FROM materials WHERE name = @name";
                string insertMaterialQuery = "INSERT INTO materials (name) VALUES (@name)";
                long materialId = InsertOrGetId(connection, selectMaterialQuery, insertMaterialQuery, material);

                // Вставляем запись угла смачивания
                string insertAngleQuery = "INSERT INTO wetting_angles (liquid_id, material_id, angle) VALUES (@liquidId, @materialId, @angle)";
                using (var cmd = new NpgsqlCommand(insertAngleQuery, connection))
                {
                    cmd.Parameters.AddWithValue("liquidId", liquidId);
                    cmd.Parameters.AddWithValue("materialId", materialId);
                    cmd.Parameters.AddWithValue("angle", angle);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Данные успешно записаны!","Запись успешна", MessageBoxButton.OK,MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка записи данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

            }
        }

        private int InsertOrGetId(NpgsqlConnection connection, string selectQuery, string insertQuery, string name)
        {
            // Проверка, существует ли запись
            using (var selectCmd = new NpgsqlCommand(selectQuery, connection))
            {
                selectCmd.Parameters.AddWithValue("@name", name);
                var result = selectCmd.ExecuteScalar();

                if (result != null && int.TryParse(result.ToString(), out int id))
                {
                    return id; // Возвращаем найденный ID
                }
            }

            // Если запись не найдена, вставляем новую
            using (var insertCmd = new NpgsqlCommand(insertQuery, connection))
            {
                insertCmd.Parameters.AddWithValue("@name", name);
                insertCmd.ExecuteNonQuery();
            }

            // Повторная проверка для получения ID после вставки
            using (var selectCmd = new NpgsqlCommand(selectQuery, connection))
            {
                selectCmd.Parameters.AddWithValue("@name", name);
                return (int)selectCmd.ExecuteScalar(); // Возвращаем ID новой записи
            }
        }

    }
}
