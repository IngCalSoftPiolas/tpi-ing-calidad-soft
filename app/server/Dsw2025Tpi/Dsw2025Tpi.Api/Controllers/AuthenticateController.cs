using Azure.Core;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Services;
using Dsw2025Tpi.Data.Repositories;
using Dsw2025Tpi.Domain.Enums;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Dsw2025Tpi.Data.Repositories.Interfaces;

namespace Dsw2025Ej15.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticateController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IRepository _repository;

    public AuthenticateController(UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        JwtTokenService jwtTokenService,
        IRepository repository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _repository = repository;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginModelRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            return Unauthorized(new { 
                error = "Usuario o contraseña incorrectos" 
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                error = "Usuario o contraseña incorrectos"
            });
        }
        var roles = await _userManager.GetRolesAsync(user);
        var userRole = roles.FirstOrDefault();
        var token = await _jwtTokenService.GenerateToken(user);
        var customer = await _repository.First<Customer>(c => c.EMail == user.Email);
        return Ok(new {
            token,
            role = userRole,
            customer?.Id
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {

        if (!Enum.TryParse<ValidRoles>(model.Role, true, out var parsedRole))
        {
            return BadRequest("Rol invalido");
        }

        var user = new IdentityUser { UserName = model.Username, Email = model.Email };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var createdUser = await _userManager.FindByNameAsync(model.Username);

        var assignRoleResult = await _userManager.AddToRoleAsync(createdUser, parsedRole.ToString());

        if(parsedRole == ValidRoles.CLIENT)
        {
            var clientCustomer = new Customer(model.Email, model.Nombre, model.PhoneNumber);
            await _repository.Add<Customer>(clientCustomer);
        }

        if (!assignRoleResult.Succeeded)
            return BadRequest(assignRoleResult.Errors);

        return Ok(new
        {
            message = "Usuario registrado correctamente",
            role = parsedRole
        });
    }
}
