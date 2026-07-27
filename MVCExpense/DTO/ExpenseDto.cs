namespace MVCExpense.DTO
{
    public class ExpenseDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
    }
}
