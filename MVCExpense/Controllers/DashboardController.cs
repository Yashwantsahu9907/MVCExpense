using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCExpense.Data;
using MVCExpense.DTO;
using System.Security.Claims;

namespace MVCExpense.Controllers;

[Authorize]
public class DashboardController(AppDbContext context) : Controller
{
    private readonly AppDbContext _context = context;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index()
    {
        var userId = GetUserId(); // Get the active user's ID

        // Calculate total income by sum of valid  income of specific user
        var totalIncome = await _context.Incomes
            .Where(i => i.UserId == userId && !i.IsDeleted)
            .SumAsync(i => i.Amount);
        // calulate expense 
        var totalExpense = await _context.Expenses
            .Where(e => e.UserId == userId && !e.IsDeleted)
            .SumAsync(e => e.Amount);

        // Calculate current balance
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