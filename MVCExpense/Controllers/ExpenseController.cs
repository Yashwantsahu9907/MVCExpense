using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVCExpense.Data;
using MVCExpense.Models;
using System.Security.Claims;

namespace MVCExpense.Controllers;

[Authorize]
public class ExpenseController(AppDbContext context) : Controller
{
    private readonly AppDbContext _context = context;

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task PopulateCategoriesAsync(int? selectedId = null)   // PopulateCategoriesAsync ye method expense ke dropdown me category load karata hai 
    {
        var userId = GetUserId();
        var categories = await _context.Categories
            .Where(c => c.UserId == userId && !c.IsDeleted) 
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedId);    // create dropdown  value = id, display = name
    }

    // Display a list of all expenses belonging to the logged-in user
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var expenses = await _context.Expenses
            .Include(e => e.Category) // y the using of include it help to show the category name instead of id category ke obj load karta hai 
            .Where(e => e.UserId == userId && !e.IsDeleted) 
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        return View(expenses);
    }

    // Open the page 
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync(); // Load the category dropdown
        return View();
    }

    // Create new expense 
    [HttpPost]
    public async Task<IActionResult> Create(Expense expense)
    {
        ModelState.Remove("User");   // naviguation property remove category id aati hai validation fail na ho isliye remove 
        ModelState.Remove("Category");
        
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(expense.CategoryId); // reload dropdown when validation is fail if it is not assign dropdown become null
            return View(expense);
        }

        expense.UserId = GetUserId();
        expense.CreatedAt = DateTime.UtcNow;
        expense.CreatedBy = expense.UserId;
        expense.IsDeleted = false;

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Expense added successfully!";
        return RedirectToAction(nameof(Index));
    }

    // Edit expense
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted);
        if (expense == null) return NotFound();

        await PopulateCategoriesAsync(expense.CategoryId);
        return View(expense);
    }

    // Handle the submission of edited expense data
    [HttpPost]
    public async Task<IActionResult> Edit(Expense expense)
    {
        ModelState.Remove("User");
        ModelState.Remove("Category");
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(expense.CategoryId);
            return View(expense);
        }

        var userId = GetUserId();
        var existingExpense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == expense.Id && e.UserId == userId && !e.IsDeleted);
        if (existingExpense == null) return NotFound();

        // Update allowable fields
        existingExpense.Title = expense.Title;
        existingExpense.Amount = expense.Amount;
        existingExpense.Description = expense.Description;
        existingExpense.CategoryId = expense.CategoryId;
        existingExpense.UpdatedAt = DateTime.UtcNow;
        existingExpense.UpdatedBy = userId;

        _context.Expenses.Update(existingExpense);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Expense updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var expense = await _context.Expenses
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted);
        if (expense == null) return NotFound();

        return View(expense);
    }

    // Perform the soft delete on the confirmed expense
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = GetUserId();
        var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId && !e.IsDeleted);
        if (expense == null) return NotFound();

        // Soft delete: Flag as deleted instead of fully dropping from the database table
        expense.IsDeleted = true;
        expense.UpdatedAt = DateTime.UtcNow;
        expense.UpdatedBy = userId;

        _context.Expenses.Update(expense);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Expense deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}
