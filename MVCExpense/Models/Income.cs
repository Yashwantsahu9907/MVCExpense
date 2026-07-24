using System.ComponentModel.DataAnnotations.Schema;

namespace MVCExpense.Models
{
    public class Income : BaseModel
    {
        public decimal Amount { get; set; }
        public string Title { get; set; }
        public DateTime IncomeDate { get; set; }
        // Foreign key 
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        // navigation property
        public User User { get; set; }
    }
}
