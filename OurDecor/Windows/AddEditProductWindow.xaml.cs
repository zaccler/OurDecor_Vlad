using System;
using System.Linq;
using System.Windows;
using System.Collections.ObjectModel;
using OurDecor.Model;
using System.Data.Entity;
using System.Windows.Controls;
using System.Windows.Media; // ← обязательно для Brushes

namespace OurDecor
{
    public partial class AddEditProductWindow : Window
    {
        private WallpaperProductionEntities1 _db = new WallpaperProductionEntities1();
        private Product _editingProduct = null;
        private ObservableCollection<MaterialViewModel> _materials = new ObservableCollection<MaterialViewModel>();

        public AddEditProductWindow()
        {
            InitializeComponent();
            InitializeForm();
        }

        public AddEditProductWindow(int productId) : this()
        {
            LoadProductForEdit(productId);
        }

        private void InitializeForm()
        {
            try
            {
                ProductTypeComboBox.ItemsSource = _db.ProductType.ToList();
                MaterialComboBox.ItemsSource = _db.Material.Include("MaterialType").ToList();
                MaterialsDataGrid.ItemsSource = _materials;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных.\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void LoadProductForEdit(int productId)
        {
            _editingProduct = _db.Product
                .Include("ProductType")
                .Include("ProductMaterial.Material.MaterialType")
                .FirstOrDefault(p => p.ProductID == productId);

            if (_editingProduct == null)
            {
                MessageBox.Show("Продукт не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
                return;
            }

            ArticleTextBox.Text = _editingProduct.Article;
            NameTextBox.Text = _editingProduct.Name;
            MinCostTextBox.Text = _editingProduct.MinCost.ToString("F2");
            WidthTextBox.Text = _editingProduct.Width.ToString("F2");

            if (_editingProduct.ProductType != null)
                ProductTypeComboBox.SelectedValue = _editingProduct.ProductType.ProductTypeID;

            _materials.Clear();
            foreach (var pm in _editingProduct.ProductMaterial)
            {
                _materials.Add(new MaterialViewModel
                {
                    MaterialID = pm.MaterialID,
                    MaterialName = pm.Material?.Name,
                    Count = pm.Count,
                    Price = pm.Material?.Price ?? 0,
                    MaterialType = pm.Material?.MaterialType?.Name ?? ""
                });
            }
        }

        private void AddMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialComboBox.SelectedItem is Material mat &&
                int.TryParse(MaterialCountTextBox.Text, out int count) &&
                count > 0)
            {
                _materials.Add(new MaterialViewModel
                {
                    MaterialID = mat.MaterialID,
                    MaterialName = mat.Name,
                    Count = count,
                    Price = mat.Price,
                    MaterialType = mat.MaterialType?.Name ?? ""
                });

                MaterialComboBox.SelectedIndex = -1;
                MaterialCountTextBox.Text = "Кол-во";
                MaterialCountTextBox.Foreground = Brushes.Gray;
            }
            else
            {
                MessageBox.Show("Выберите материал и введите количество > 0.",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string article = ArticleTextBox.Text?.Trim() ?? "";
                string name = NameTextBox.Text?.Trim() ?? "";

                // Проверка на заполнение обязательных полей
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Введите наименование продукта.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (ProductTypeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип продукта.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка числовых полей
                if (!double.TryParse(MinCostTextBox.Text, out double minCost) || minCost < 0)
                {
                    MessageBox.Show("Минимальная стоимость должна быть числом ≥ 0.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!double.TryParse(WidthTextBox.Text, out double width) || width < 0)
                {
                    MessageBox.Show("Ширина должна быть числом ≥ 0.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка артикулы: можно сделать необязательным, но если введён – проверяем длину
                if (!string.IsNullOrEmpty(article) && article.Length > 50 || article.Length == 0)
                {
                    MessageBox.Show("Артикул не должен превышать 50 символов или быть пустым.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка наличия хотя бы одного материала
                if (_materials.Count == 0)
                {
                    MessageBox.Show("Добавьте хотя бы один материал с количеством > 0.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                foreach (var mat in _materials)
                {
                    if (mat.Count <= 0)
                    {
                        MessageBox.Show($"Количество материала '{mat.MaterialName}' должно быть > 0.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                int selectedTypeId = (int)ProductTypeComboBox.SelectedValue;
                Product prod;

                if (_editingProduct == null)
                {
                    prod = new Product
                    {
                        Article = article,
                        Name = name,
                        MinCost = minCost,
                        Width = width,
                        MaterialTypeID = selectedTypeId
                    };
                    _db.Product.Add(prod);
                }
                else
                {
                    prod = _db.Product.Find(_editingProduct.ProductID);
                    prod.Article = article;
                    prod.Name = name;
                    prod.MinCost = minCost;
                    prod.Width = width;
                    prod.MaterialTypeID = selectedTypeId;

                    var oldMaterials = _db.ProductMaterial.Where(pm => pm.ProductID == prod.ProductID).ToList();
                    _db.ProductMaterial.RemoveRange(oldMaterials);
                }

                foreach (var m in _materials)
                {
                    _db.ProductMaterial.Add(new ProductMaterial
                    {
                        Product = prod,
                        MaterialID = m.MaterialID,
                        Count = m.Count
                    });
                }

                _db.SaveChanges();
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Обработчики placeholder
        private void MaterialCountTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb.Text == "Кол-во")
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
            }
        }

        private void MaterialCountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = "Кол-во";
                tb.Foreground = Brushes.Gray;
            }
        }
    }

    public class MaterialViewModel
    {
        public int MaterialID { get; set; }
        public string MaterialName { get; set; }
        public double Count { get; set; }
        public double Price { get; set; }
        public string MaterialType { get; set; }
    }
}
