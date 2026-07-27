using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVCExpense.Data;
using MVCExpense.Models;
using System.Security.Claims;

namespace MVCExpense.Controllers;

[Authorize] // Requires a valid JWT token/cookie to access any action in this controller
public class ExpenseController(AppDbContext context) : Controller
{
    private readonly AppDbContext _context = context;

    // securely extract the UserId from the logged-in user's claims
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Helper method to populate the Categories dropdown exclusively with categories created by the logged-in user
    private async Task PopulateCategoriesAsync(int? selectedId = null)
    {
        var userId = GetUserId();
        var categories = await _context.Categories
            .Where(c => c.UserId == userId && !c.IsDeleted) // Isolation: Only the current user's categories
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedId);
    }

    // Display a list of all expenses belonging to the logged-in user
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var expenses = await _context.Expenses
            .Include(e => e.Category) // Include related Category data to display Category Name
            .Where(e => e.UserId == userId && !e.IsDeleted) // Isolation check
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        return View(expenses);
    }

    // Open the page to create a new expense
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync(); // Load the category dropdown
        return View();
    }

    // Handle the submission of a new expense
    [HttpPost]
    public async Task<IActionResult> Create(Expense expense)
    {
        // Remove related objects from validation since they are not bound in the form
        ModelState.Remove("User");
        ModelState.Remove("Category");
        
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(expense.CategoryId); // Reload dropdown on error
            return View(expense);
        }

        // Enforce the relationship: Attach this expense directly to the logged-in user
        expense.UserId = GetUserId();
        expense.CreatedAt = DateTime.UtcNow;
        expense.CreatedBy = expense.UserId;
        expense.IsDeleted = false;

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Expense added successfully!";
        return RedirectToAction(nameof(Index));
    }

    // Open the edit page for a specific expense, verifying ownership
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        // Fetch only if the expense belongs to the active user (Isolation check)
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
        // Fetch the existing record from DB safely utilizing user isolation
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

    // Show the confirmation page to delete an expense, verifying ownership
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
