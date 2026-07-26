using System.ComponentModel.DataAnnotations;

namespace MVCExpense.DTO;

public class RegisterDto
{
    public string Name { get; set; }
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
}
