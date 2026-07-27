using Microsoft.EntityFrameworkCore;
using MVCExpense.Models;

namespace MVCExpense.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Income> Incomes { get; set; }

         // -------------- BETTER OPTION FOR RELATIONSHIP--------------------------

        protected override void OnModelCreating(ModelBuilder modelBuilder)   // Dbcontext ke andar pahle se onmodelcreating() methad hota hai hum usi ko apne requirement ke according modify krte hai isliye override use kiye hai  
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.User)    // Every category has one user
            .WithMany()   // one user has many category
            .HasForeignKey(c => c.UserId)   // category table has column of user foreign key 
            .OnDelete(DeleteBehavior.Restrict);    // Delete rule set fist delete category then delete user

        modelBuilder.Entity<Expense>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Expense>()
            .HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Income>()
            .HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
