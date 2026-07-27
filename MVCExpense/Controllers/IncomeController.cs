using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCExpense.Data;
using MVCExpense.Models;
using System.Security.Claims;

namespace MVCExpense.Controllers;

[Authorize] // Requires a valid JWT token/cookie to access any action in this controller
public class IncomeController(AppDbContext context) : Controller
{
    private readonly AppDbContext _context = context;

    // Securely extract the UserId from the logged-in user's claims
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Display a list of all income records belonging to the logged-in user
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var incomes = await _context.Incomes
            .Where(i => i.UserId == userId && !i.IsDeleted) // Isolation check
            .OrderByDescending(i => i.IncomeDate)
            .ToListAsync();
        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        return View(incomes);
    }

    // Open the page to create a new income
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // Handle the submission of a new income record
    [HttpPost]
    public async Task<IActionResult> Create(Income income)
    {
        ModelState.Remove("User"); // Exclude navigation property from validation
        if (!ModelState.IsValid) return View(income);

        // Enforce the relationship: Attach this income directly to the logged-in user
        income.UserId = GetUserId();
        income.CreatedAt = DateTime.UtcNow;
        income.CreatedBy = income.UserId;
        income.IsDeleted = false;

        _context.Incomes.Add(income);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Income added successfully!";
        return RedirectToAction(nameof(Index));
    }

    // Open the edit page for a specific income, verifying ownership
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        // Fetch only if the income belongs to the active user (Isolation check)
        var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId && !i.IsDeleted);
        if (income == null) return NotFound();

        return View(income);
    }

    // Handle the submission of edited income data
    [HttpPost]
    public async Task<IActionResult> Edit(Income income)
    {
        ModelState.Remove("User");
        if (!ModelState.IsValid) return View(income);

        var userId = GetUserId();
        // Fetch the existing record from DB safely utilizing user isolation
        var existingIncome = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == income.Id && i.UserId == userId && !i.IsDeleted);
        if (existingIncome == null) return NotFound();

        // Update allowable fields
        existingIncome.Title = income.Title;
        existingIncome.Amount = income.Amount;
        existingIncome.IncomeDate = income.IncomeDate;
        existingIncome.UpdatedAt = DateTime.UtcNow;
        existingIncome.UpdatedBy = userId;

        _context.Incomes.Update(existingIncome);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Income updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    // Show the confirmation page to delete an income, verifying ownership
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId && !i.IsDeleted);
        if (income == null) return NotFound();

        return View(income);
    }

    // Perform the soft delete on the confirmed income
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = GetUserId();
        var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId && !i.IsDeleted);
        if (income == null) return NotFound();

        // Soft delete: Flag as deleted instead of fully dropping from the database table
        income.IsDeleted = true;
        income.UpdatedAt = DateTime.UtcNow;
        income.UpdatedBy = userId;

        _context.Incomes.Update(income);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Income deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}
