using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using server.Dtos.Account;
using server.Interfaces;
using server.Models;

namespace server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenService _tokenService;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterDTO dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new User()
            {
                UserName = dto.Username,
                Email = dto.Email,
            };
            
            // Password hashing is handled by userManager
            var createdUser = await _userManager.CreateAsync(user, dto.Password);

            if (createdUser.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, "User");
                if (roleResult.Succeeded)
                {
                    return Ok(
                        new NewUserDTO
                        {
                            UserName = user.UserName,
                            Email = user.Email,
                            Token = _tokenService.CreateToken(user)
                        });
                }
                else
                {
                    return StatusCode(500, roleResult.Errors.Select(x => x.Description).ToArray());
                }
            }
            else
            {
                return StatusCode(500, createdUser.Errors.Select(x => x.Description).ToArray());
            }
        }
        catch (Exception e)
        {
            return StatusCode(500, "Internal Server Error");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var user = await _userManager.FindByNameAsync(dto.Username);
        if(user == null) return Unauthorized("Username or password is incorrect");
        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if(!result.Succeeded) return Unauthorized("Username or password is incorrect");

        return Ok(
            new NewUserDTO
            {
                UserName = user.UserName,
                Email = user.Email,
                Token = _tokenService.CreateToken(user)
            }
        );
    }
}