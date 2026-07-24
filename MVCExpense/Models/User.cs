using System.ComponentModel.DataAnnotations;

namespace MVCExpense.Models
{
    public class User : BaseModel
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
