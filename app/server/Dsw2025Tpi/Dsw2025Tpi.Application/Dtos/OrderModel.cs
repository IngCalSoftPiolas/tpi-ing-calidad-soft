using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Dtos
{
        public record OrderModelRequest(
            [Required(ErrorMessage ="Debe especificar un código de cliente.")]
            Guid CustomerId,

            [Required(ErrorMessage ="Debe especificar un domicilio de envio.")]
            [MinLength(1,ErrorMessage ="El domicilio de envio no puede estar vacio.")]
            string ShippingAddress,

            [Required(ErrorMessage ="Debe especificar un domicilio de cobro.")]
            [MinLength(1,ErrorMessage ="El domicilio de cobro no puede estar vacio.")]
            string BillingAdress,

            [Required(ErrorMessage ="La orden debe tener items de compra.")] 
            OrderItemModelRequest[] OrderItems,

            [MaxLength(60,ErrorMessage ="La nota no puede superar los 60 caracteres.")]
            string? Notes
            );
        public record OrderModelResponse(
            Guid OrderId,
            decimal? TotalAmount,
            Guid CustomerId,
            string? CustomerName,
            string? ShippingAddress,
            string? BillingAddress,
            string? Notes,
            string? Status,
            OrderItemModelResponse[] OrderItems
            );
}
