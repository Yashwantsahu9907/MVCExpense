using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MVCExpense.Models
{
    public class Expense : BaseModel
    {
        [Required]
        public string Title { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }

        // Use Foreign key it inherit the the data from the primary key
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        // navigation propert = it is to allow to navigate from one end of a data relationship to the other
        public Category Category { get; set; }   // access whole category Object 

        // foreign key
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
