using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Domain.Entities
{
    public class OrderItem : EntityBase
    {
        public OrderItem() { }
        public OrderItem(Product product, int quantity) {
            this.UnitPrice = product.CurrentUnitPrice;
            this.Quantity = quantity;
            this.ProductId = product.Id;
        }
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; private set; }
        public Guid ProductId { get; set; }
        public Product Product { get; set; }
        public Guid OrderId { get; set; }
        public Order Order { get; set; }
        public decimal? Subtotal => UnitPrice * Quantity;
    }
}
