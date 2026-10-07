using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Domain.Entities
{
    public class Product : EntityBase
    {
        public Product() { } 
        public Product(string sku, string? name = null, decimal? price = null, int? stock = 0) {
            Sku = sku;
            Name = name;
            CurrentUnitPrice = price;
            StockQuantity = stock;
            /*if (string.IsNullOrEmpty(name) || price.HasValue || StockQuantity.HasValue)
            {
                IsActive = false;
            }*/
            //else {
                IsActive = true;
            //}
                
                
        }
        public string Sku { get; set; }
        public string? InternalCode { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? CurrentUnitPrice { get; set; }
        public int? StockQuantity { get; set; }
        public bool IsActive { get; set; } 
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
