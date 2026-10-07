using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Dtos
{
        public record OrderItemModelRequest(
            [Required(ErrorMessage ="El codigo de producto es obligatorio.")]
            Guid ProductCode,

            [Required(ErrorMessage = "La cantidad es obligatoria.")]
            [Range(1,int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa o 0.")] 
            int Quantity
            );

        public record OrderItemModelResponse(
            string? Name,
            string? Description,
            int? Quantity,
            decimal? Subtotal
            );
}
