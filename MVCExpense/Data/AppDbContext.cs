using Microsoft.EntityFrameworkCore;
using MVCExpense.Models;

namespace MVCExpense.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Income> Incomes { get; set; }
}
