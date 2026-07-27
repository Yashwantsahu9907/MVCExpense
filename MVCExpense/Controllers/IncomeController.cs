using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCExpense.Data;
using MVCExpense.Models;
using System.Security.Claims;

namespace MVCExpense.Controllers;

[Authorize] 
public class IncomeController(AppDbContext context) : Controller
{
    private readonly AppDbContext _context = context;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // All income record 
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var incomes = await _context.Incomes
            .Where(i => i.UserId == userId && !i.IsDeleted)
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

        income.UserId = GetUserId();
        income.CreatedAt = DateTime.UtcNow;
        income.CreatedBy = income.UserId;
        income.IsDeleted = false;

        _context.Incomes.Add(income);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Income added successfully!";
        return RedirectToAction(nameof(Index));
    }

    // Open the edit page
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId && !i.IsDeleted);
        if (income == null) return NotFound();

        return View(income);
    }

    // Handle the submission of edited income data
    [HttpPost]
    public async Task<IActionResult> Edit(Income income)
    {
        ModelState.Remove("User");    // Remove from validation 
        if (!ModelState.IsValid) return View(income);

        var userId = GetUserId();
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

        income.IsDeleted = true;
        income.UpdatedAt = DateTime.UtcNow;
        income.UpdatedBy = userId;

        _context.Incomes.Update(income);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Income deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}
