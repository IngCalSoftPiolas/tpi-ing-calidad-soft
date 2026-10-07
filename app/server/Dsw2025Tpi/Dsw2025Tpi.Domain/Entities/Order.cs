using Dsw2025Tpi.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Domain.Entities
{
    public class Order : EntityBase
    {
        public Order() { }
        public Order(Customer customer, string shippingAddress, string billingAdress, List<OrderItem> orderItems) { 
            Customer = customer;
            CustomerId = customer.Id;
            ShippingAddress = shippingAddress;
            BillingAddress = billingAdress;

            foreach (var item in orderItems) { 
                OrderItems.Add(item);    
            }
            Status = OrderStatus.PENDING;
            Date = DateTime.Now;
        }
        public DateTime Date { get; set; }
        public string ShippingAddress { get; set; }
        public string BillingAddress { get; set; }
        public string? Notes { get; set; }
        public ICollection<OrderItem> OrderItems { get; } = new List<OrderItem>();
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; }
        //Sumamos los subtotales de los items para obtener el total de la orden
        public decimal? TotalAmount => OrderItems.Sum(item => item.Subtotal);
        public OrderStatus Status { get; set; }

        // Indica si el estado actual es final (no permiten transiciones)
        public bool IsFinalState => Status == OrderStatus.DELIVERED || Status == OrderStatus.CANCELLED;

        // Comprueba si es posible transicionar desde el estado actual al estado pedido
        public bool CanTransitionTo(OrderStatus newStatus)
        {
            if (Status == newStatus) return false;

            return Status switch
            {
                OrderStatus.PENDING => newStatus == OrderStatus.PROCESSING || newStatus == OrderStatus.CANCELLED,
                OrderStatus.PROCESSING => newStatus == OrderStatus.SHIPPED || newStatus == OrderStatus.CANCELLED,
                OrderStatus.SHIPPED => newStatus == OrderStatus.DELIVERED,
                _ => false,
            };
        }

        // Cambia el estado validando primero; lanza InvalidOperationException si no está permitido
        public void ChangeStatus(OrderStatus newStatus)
        {
            if (!CanTransitionTo(newStatus))
                throw new InvalidOperationException($"Transición no permitida de {Status} a {newStatus}.");

            Status = newStatus;
        }
    }
}
