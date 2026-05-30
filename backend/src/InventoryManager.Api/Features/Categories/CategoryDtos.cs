using System.ComponentModel.DataAnnotations;

namespace InventoryManager.Api.Features.Categories;

public record CategoryDto(int Id, string Name, string? Description, int ProductCount);

public record SaveCategoryRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description);
