using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Сontact_Angle_Meter
{
    /// <summary>
    /// Логика взаимодействия для DataBaseWindow.xaml
    /// </summary>
    public partial class DataBaseWindow : Window
    {
        private string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=NaPoLiBon;Database=contactAngles_db";
        private List<ContactAngleInfo> contactAngleList;

        public DataBaseWindow()
        {
            InitializeComponent();
            LoadLiquids();
            LoadMaterials();
            contactAngleList = new List<ContactAngleInfo>();
        }

        //Подключение к БД для получения доступа к жидкостям
        private void LoadLiquids()
        {
            List<string> liquids = new List<string>();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT name FROM liquids";

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            liquids.Add(reader.GetString(0));
                        }
                    }
                }
            }

            cbLiquids.ItemsSource = liquids;
        }

        //Подключение к БД для получения доступа к материалам
        private void LoadMaterials()
        {
            List<string> materials = new List<string>();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT name FROM materials";

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            materials.Add(reader.GetString(0));
                        }
                    }
                }
            }

            cbMaterials.ItemsSource = materials;
        }

        private void btnShowAngle_Click(object sender, RoutedEventArgs e)
        {
            if (cbLiquids.SelectedItem == null || cbMaterials.SelectedItem == null)
            {
                MessageBox.Show("Выберите жидкость и материал.","Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            string selectedLiquid = cbLiquids.SelectedItem.ToString();
            string selectedMaterial = cbMaterials.SelectedItem.ToString();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                // Запрос с фильтрацией по выбранным значениям жидкости и материала
                string query = @"
                            SELECT wetting_angles.id, liquids.name AS Liquid, materials.name AS Material, wetting_angles.angle
                            FROM wetting_angles
                            JOIN liquids ON wetting_angles.liquid_id = liquids.id
                            JOIN materials ON wetting_angles.material_id = materials.id
                            WHERE liquids.name = @liquid AND materials.name = @material";

                using (var cmd = new NpgsqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("liquid", selectedLiquid);
                    cmd.Parameters.AddWithValue("material", selectedMaterial);

                    using (var reader = cmd.ExecuteReader())
                    {
                        contactAngleList.Clear();  // Очищаем список перед обновлением
                                                   // List<ContactAngleInfo> contactAngleList = new List<ContactAngleInfo>();
                        while (reader.Read())
                        {
                            contactAngleList.Add(new ContactAngleInfo
                            {
                                Id = reader.GetInt32(0),              
                                Liquid = reader.GetString(1),          
                                Material = reader.GetString(2),        
                                Angle = reader.GetDouble(3)
                            });
                        }

                        ContactAngleDBGrid.ItemsSource = null;
                        ContactAngleDBGrid.ItemsSource = contactAngleList;
                    }
                }
            }
        }

        // Обработчик кнопки удаления
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedAngle = (ContactAngleInfo)ContactAngleDBGrid.SelectedItem;

            if (selectedAngle == null)
            {
                MessageBox.Show("Выберите строку для удаления.");
                return;
            }

            // Подтверждение удаления
            var result = MessageBox.Show($"Вы уверены, что хотите удалить запись '{selectedAngle.Liquid} {selectedAngle.Material} {selectedAngle.Angle}'?",
                                          "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

            if (result == MessageBoxResult.Yes)
            {
                // Удаление из базы данных
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    string deleteQuery = "DELETE FROM wetting_angles WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(deleteQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("id", selectedAngle.Id);
                        //cmd.ExecuteNonQuery();

                        // Выполняем запрос
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Запись успешно удалена из базы данных.", "Удаление успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Запись не была найдена в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        }
                    }
                }

                // Удаление из списка и обновление DataGrid
                contactAngleList.Remove(selectedAngle);
                ContactAngleDBGrid.ItemsSource = null;  // Сброс источника данных
                ContactAngleDBGrid.ItemsSource = contactAngleList;  // Обновление источника данных
            }
        }
    }

    //Класс для хранения информации о жидкости, материале и угле смачивания
    public class ContactAngleInfo
    {
        public string Liquid { get; set; }
        public string Material { get; set; }
        public double Angle { get; set; }
        public int Id { get; set; }
    }

}

