using System;
using System.Linq;
using OurDecor.Model;

namespace OurDecor
{
    public static class MaterialCalculator
    {
        /// <summary>
        /// Рассчитывает количество материала, необходимого для производства продукции с учётом брака и наличия на складе.
        /// </summary>
        /// <param name="productTypeId">ID типа продукта</param>
        /// <param name="materialTypeId">ID типа материала</param>
        /// <param name="productQuantity">Количество продукции (целое число)</param>
        /// <param name="param1">Параметр продукции 1 (вещественное число, >0)</param>
        /// <param name="param2">Параметр продукции 2 (вещественное число, >0)</param>
        /// <param name="stockAmount">Количество материала на складе (вещественное число, ≥0)</param>
        /// <returns>Необходимое количество материала (целое), -1 при ошибке</returns>
        public static int CalculateRequiredMaterial(int productTypeId, int materialTypeId, int productQuantity, double param1, double param2, double stockAmount)
        {
            if (productQuantity <= 0 || param1 <= 0 || param2 <= 0 || stockAmount < 0)
                return -1;

            using (var db = new WallpaperProductionEntities1())
            {
                var productType = db.ProductType.FirstOrDefault(pt => pt.ProductTypeID == productTypeId);
                var materialType = db.MaterialType.FirstOrDefault(mt => mt.MaterialTypeID == materialTypeId);

                if (productType == null || materialType == null)
                    return -1;

                double coef = productType.Coefficient;
                double defectPercent = materialType.DefectPercent; // в процентах

                double materialPerUnit = param1 * param2 * coef;
                double totalRequired = materialPerUnit * productQuantity;

                // Учитываем брак
                totalRequired *= (1.0 + defectPercent / 100.0);

                // Вычитаем уже имеющееся на складе
                double toBuy = totalRequired - stockAmount;

                return toBuy > 0 ? (int)Math.Ceiling(toBuy) : 0;
            }
        }
    }
}
