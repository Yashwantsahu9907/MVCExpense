using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCExpense.Models
{
    public class Category :BaseModel
    {
        [Required]
        public string Name { get; set; }
        
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
