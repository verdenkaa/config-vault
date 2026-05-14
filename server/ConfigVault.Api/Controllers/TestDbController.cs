using Microsoft.AspNetCore.Mvc;
using ConfigVault.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfigVault.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestDbController : ControllerBase
{
    private readonly AppDbContext _context;

    public TestDbController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("add-test-user")]
    public async Task<IActionResult> AddTestUser()
    {
        var user = new ConfigVault.Api.Models.User
        {
            Login = "testuser",
            PasswordHash = "hashed_placeholder"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok(new { user.Id, user.Login });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users.Select(u => new { u.Id, u.Login, u.CreatedAt }));
    }
}