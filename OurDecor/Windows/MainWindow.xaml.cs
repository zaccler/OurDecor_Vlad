using System;
using System.Linq;
using System.Windows;
using System.Data.Entity;
using OurDecor.Model;

namespace OurDecor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                using (var db = new WallpaperProductionEntities1())
                {
                    var productsFromDb = db.Product
                        .Include("ProductType")
                        .Include("ProductMaterial.Material.MaterialType")
                        .Include("ProductMaterial")
                        .ToList();

                    var productViewModels = productsFromDb
                        .Select(p =>
                        {
                            double materialsCost = 0.0;
                            foreach (var pm in p.ProductMaterial)
                            {
                                var material = pm.Material;
                                if (material == null) continue;
                                double itemCost = material.Price * pm.Count *
                                                  (1.0 + (material.MaterialType?.DefectPercent ?? 0) / 100.0);
                                materialsCost += itemCost;
                            }

                            double coef = p.ProductType?.Coefficient ?? 1.0;
                            double totalCost = Math.Round(materialsCost * coef, 2);
                            if (totalCost < 0) totalCost = 0;

                            return new ProductViewModel
                            {
                                ProductID = p.ProductID,
                                ProductTypeName = p.ProductType?.Name ?? "",
                                Name = p.Name,
                                Article = p.Article,
                                MinCost = Math.Round(Math.Max(0, p.MinCost), 2),
                                Width = Math.Round(Math.Max(0, p.Width), 2),
                                TotalCost = totalCost
                            };
                        })
                        .ToList();

                    ProductsItemsControl.ItemsSource = productViewModels;
                    StatusText.Text = "Загрузка завершена успешно";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddEditProductWindow();
            addWindow.Owner = this;
            if (addWindow.ShowDialog() == true)
                LoadProducts();
        }

        private void ProductCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            if (border?.DataContext is ProductViewModel vm)
            {
                var editWindow = new AddEditProductWindow(vm.ProductID);
                editWindow.Owner = this;
                if (editWindow.ShowDialog() == true)
                    LoadProducts();
            }
        }

        private void CalculateMaterialsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ProductViewModel vm)
            {
                var inputWindow = new InputParametersWindow();
                inputWindow.Owner = this;
                if (inputWindow.ShowDialog() == true)
                {
                    int productQty = inputWindow.ProductQuantity;
                    double param1 = inputWindow.Param1;
                    double param2 = inputWindow.Param2;

                    ShowRequiredMaterialsForProduct(vm.ProductID, productQty, param1, param2);
                }
            }
        }

        private void ShowRequiredMaterialsForProduct(int productId, int productQty, double param1, double param2)
        {
            using (var db = new WallpaperProductionEntities1())
            {
                var product = db.Product
                                .Include("ProductType")
                                .Include("ProductMaterial.Material.MaterialType")
                                .FirstOrDefault(p => p.ProductID == productId);
                if (product == null) return;

                foreach (var pm in product.ProductMaterial)
                {
                    int toBuy = MaterialCalculator.CalculateRequiredMaterial(
                        product.MaterialTypeID,
                        pm.Material.MaterialTypeID,
                        productQty,
                        param1,
                        param2,
                        pm.Count
                    );

                    MessageBox.Show($"Материал: {pm.Material.Name}\nНеобходимо закупить: {toBuy}",
                                    "Расчет закупки", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }

    public class ProductViewModel
    {
        public int ProductID { get; set; }
        public string ProductTypeName { get; set; }
        public string Name { get; set; }
        public string Article { get; set; }
        public double MinCost { get; set; }
        public double Width { get; set; }
        public double TotalCost { get; set; }
    }
}
