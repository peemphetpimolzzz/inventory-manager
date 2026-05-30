using InventoryManager.Api.Common;
using InventoryManager.Api.Data;
using InventoryManager.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Api.Features.Categories;

[ApiController]
[Route("api/categories")]
public class CategoriesController(InventoryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll()
    {
        var items = await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.Products.Count))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        var category = await db.Categories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.Products.Count))
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException($"Category {id} not found.");
        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(SaveCategoryRequest request)
    {
        if (await db.Categories.AnyAsync(c => c.Name == request.Name))
        {
            throw new BusinessRuleException($"A category named '{request.Name}' already exists.");
        }

        var category = new Category { Name = request.Name, Description = request.Description };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.Id },
            new CategoryDto(category.Id, category.Name, category.Description, 0));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> Update(int id, SaveCategoryRequest request)
    {
        var category = await db.Categories.FindAsync(id)
            ?? throw new NotFoundException($"Category {id} not found.");
        if (await db.Categories.AnyAsync(c => c.Name == request.Name && c.Id != id))
        {
            throw new BusinessRuleException($"A category named '{request.Name}' already exists.");
        }

        category.Name = request.Name;
        category.Description = request.Description;
        await db.SaveChangesAsync();
        var count = await db.Products.CountAsync(p => p.CategoryId == id);
        return Ok(new CategoryDto(category.Id, category.Name, category.Description, count));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories.FindAsync(id)
            ?? throw new NotFoundException($"Category {id} not found.");
        if (await db.Products.AnyAsync(p => p.CategoryId == id))
        {
            throw new BusinessRuleException("Cannot delete a category that still has products.");
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
