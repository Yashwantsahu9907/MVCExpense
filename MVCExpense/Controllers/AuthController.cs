using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MVCExpense.Data;
using MVCExpense.Models;
using MVCExpense.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MVCExpense.Controllers;

public class AuthController(AppDbContext context) : Controller
{
    private readonly AppDbContext _context = context;
    public IActionResult Login()
    {
        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        return View();
    }   
    public IActionResult Register()
    {
        return View();
    }


    //Register
    public async Task<IActionResult> CreateUser(RegisterDto dto)
    {
        if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Name) || string.IsNullOrEmpty(dto.Password))
        {
            ViewBag.ErrorMessage = "Kindly please fill all the details";
            return View("Login");   // stay in login page
        }
        var isUserExist = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
        if(isUserExist == null)
        {
            var user = new User
            {
                Email = dto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        else
        {
            ViewBag.ErrorMessage = "User with this email is already exist";
            return View("Register");
        }
        TempData["SuccessMessage"] = " User Created Successfully";
        return RedirectToAction("Login");
    }

    // Login 
    [HttpPost]
    public async Task<IActionResult> LoginUser(LoginDto dto)
    {
        // when enter null filling is mandotory
        if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
        {
            ViewBag.ErrorMessage = "Kindly please fill all the details";
            return View("Login");   // stay in login page
        }
        // when user does not exist
        var isUserExist = await _context.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
        if (isUserExist == null)
        {
            ViewBag.ErrorMessage = "User doesn't exist with this email";
            return View("Login");
        }

        // Verify Password
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, isUserExist.Password);
        if (!isPasswordValid)
        {
            ViewBag.ErrorMessage = "Invalid Password";
            return View("Login");
        }

        // Generate JWT Token
        string token = GetJwtToken(isUserExist);
        // Store token in Cookie
        Response.Cookies.Append("JwtToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(30)
        });
        return RedirectToAction("Index", "Dashboard");
    }


    [HttpGet]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("JwtToken");
        TempData["SuccessMessage"] = "Logged out successfully";
        return RedirectToAction("Login");
    }

    // JWT AUTHENTICATION
    private string GetJwtToken(User user)
    {
        var claims = new[]   // create claim
        {
             new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  //password will not pass here due to security risk
             new Claim(ClaimTypes.Email, user.Email),
             new Claim(ClaimTypes.Name, user.Name)
        };
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("9khIuQ1ANQnfq2lhRZRFG4wpIGPIdN7w1AeOO9MltDXYnRhY2XhCt5di62hsC8cv")
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: "MVCExpenseApi",
            audience: "MVCExpenseUser",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}