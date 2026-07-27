using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCExpense.Data;
using MVCExpense.DTO;
using System.Security.Claims;

namespace MVCExpense.Controllers;

[Authorize] // Ensures only logged-in users can access the dashboard
public class DashboardController(AppDbContext context) : Controller
{
    private readonly AppDbContext _context = context;

    // Helper method to retrieve the currently logged-in user's ID securely from their JWT claims
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var userId = GetUserId(); // Get the active user's ID

        // Calculate total income by summing up all valid income records for this specific user
        var totalIncome = await _context.Incomes
            .Where(i => i.UserId == userId && !i.IsDeleted)
            .SumAsync(i => i.Amount);

        // Calculate total expense by summing up all valid expense records for this specific user
        var totalExpense = await _context.Expenses
            .Where(e => e.UserId == userId && !e.IsDeleted)
            .SumAsync(e => e.Amount);

        // Calculate current balance by subtracting total expenses from total income
        var balance = totalIncome - totalExpense;

        // Fetch the 5 most recent income transactions, selecting only necessary fields to optimize performance
        var recentIncomes = await _context.Incomes
            .Where(i => i.UserId == userId && !i.IsDeleted)
            .Select(i => new { Type = "Income", Title = i.Title, Amount = i.Amount, Date = i.IncomeDate })
            .OrderByDescending(x => x.Date)
            .Take(5)
            .ToListAsync();

        // Fetch the 5 most recent expense transactions
        var recentExpenses = await _context.Expenses
            .Where(e => e.UserId == userId && !e.IsDeleted)
            .Select(e => new { Type = "Expense", Title = e.Title, Amount = e.Amount, Date = e.CreatedAt })
            .OrderByDescending(x => x.Date)
            .Take(5)
            .ToListAsync();

        // Combine both recent incomes and expenses, sort them by date (newest first), and limit to 10 overall transactions
        var recentHistory = recentIncomes.Concat(recentExpenses)
            .OrderByDescending(x => x.Date)
            .Take(10)
            .ToList();

        // Pass calculated data to the View using ViewBag
        ViewBag.TotalIncome = totalIncome;
        ViewBag.TotalExpense = totalExpense;
        ViewBag.Balance = balance;
        ViewBag.RecentHistory = recentHistory;
        ViewBag.SuccessMessage = TempData["SuccessMessage"];

        return View();
    }
}