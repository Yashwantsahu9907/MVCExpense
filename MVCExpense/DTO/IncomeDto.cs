namespace MVCExpense.DTO
{
    public class IncomeDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Title { get; set; }
        public DateTime IncomeDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
