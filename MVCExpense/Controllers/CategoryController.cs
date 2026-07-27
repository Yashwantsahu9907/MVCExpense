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

    // Securely extract the UserId from the loggedin  user
    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Display a list of all category records belonging to the logged-in user
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();    // current login user ki id nikal raha hai 
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

    // Create new category record 
    [HttpPost]
    public async Task<IActionResult> Create(Category category)
    {
        ModelState.Remove("User"); // Exclude navigation property from validation
        if (!ModelState.IsValid)
            return View(category);
        
        category.UserId = GetUserId();  // user id assign by serverr  due to security reason 
        category.CreatedAt = DateTime.UtcNow;
        category.CreatedBy = category.UserId;
        category.IsDeleted = false;     // new record is not deleted

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Category created successfully!";
        return RedirectToAction(nameof(Index));
    }

    //  Edit category 
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
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