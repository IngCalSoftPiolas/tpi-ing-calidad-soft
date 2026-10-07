using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2025Tpi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        [Authorize(Roles = "Master, Client")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderModelRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var order = await _orderService.CreateOrder(request);
                return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, order);
            }
            catch (NotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InsufficientStockException ex) { 
                return BadRequest(ex.Message);
            }
            catch(InactiveProductException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        

        [HttpGet("{id}")]
        [Authorize(Roles = "Master, Admin, Client")]
        public async Task<ActionResult<OrderModelResponse>> GetOrderById(Guid id)
        {
            var order = await _orderService.GetOrderById(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpGet]
        [Authorize(Roles = "Master, Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var order = await _orderService.GetAllOrders();
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Master, Admin")]

        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] NewOrderStatusModel nw)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var order = await _orderService.UpdateOrderStatus(id, nw);
                return Ok(order);
            }
            catch(NotFoundException ex) { 
                return NotFound(ex.Message);
            }
            catch(ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

        }

    }
}
