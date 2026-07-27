using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVCExpense.Data;
using MVCExpense.Models;
using System.Security.Claims;

namespace MVCExpense.Controllers;

[Authorize] // Requires a valid JWT token/cookie to access any action in this controller
public class CategoryController(AppDbContext context) : Controller
{
    private readonly AppDbContext _context = context;

    // Securely extract the UserId from the logged-in user's claims
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Display a list of all category records belonging to the logged-in user
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var categories = await _context.Categories
            .Where(c => c.UserId == userId && !c.IsDeleted) // Isolation check
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        return View(categories);
    }

    // Open the page to create a new category
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // Handle the submission of a new category record
    [HttpPost]
    public async Task<IActionResult> Create(Category category)
    {
        ModelState.Remove("User"); // Exclude navigation property from validation
        if (!ModelState.IsValid)
            return View(category);

        // Enforce the relationship: Attach this category directly to the logged-in user
        category.UserId = GetUserId();
        category.CreatedAt = DateTime.UtcNow;
        category.CreatedBy = category.UserId;
        category.IsDeleted = false;

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Category created successfully!";
        return RedirectToAction(nameof(Index));
    }

    // Open the edit page for a specific category, verifying ownership
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        // Fetch only if the category belongs to the active user (Isolation check)
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted);
        if (category == null) return NotFound();

        return View(category);
    }

    // Handle the submission of edited category data
    [HttpPost]
    public async Task<IActionResult> Edit(Category category)
    {
        ModelState.Remove("User");
        if (!ModelState.IsValid) return View(category);

        var userId = GetUserId();
        // Fetch the existing record from DB safely utilizing user isolation
        var existingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id && c.UserId == userId && !c.IsDeleted);
        if (existingCategory == null) return NotFound();

        // Update allowable fields
        existingCategory.Name = category.Name;
        existingCategory.UpdatedAt = DateTime.UtcNow;
        existingCategory.UpdatedBy = userId;

        _context.Categories.Update(existingCategory);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Category updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    // Show the confirmation page to delete a category, verifying ownership
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted);
        if (category == null) return NotFound();

        return View(category);
    }

    // Perform the soft delete on the confirmed category
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = GetUserId();
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted);
        if (category == null) return NotFound();

        // Soft delete: Flag as deleted instead of fully dropping from the database table
        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedBy = userId;

        _context.Categories.Update(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Category deleted successfully!";
        return RedirectToAction(nameof(Index));
    }
}