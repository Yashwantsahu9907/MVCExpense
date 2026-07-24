using System.ComponentModel.DataAnnotations;

namespace MVCExpense.Models
{
    public class Category :BaseModel
    {
        [Required]
        public string Name { get; set; }
    }
}
