using System.ComponentModel.DataAnnotations;

namespace ConfigVault.Api.Models;

public class UserProject
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid ProjectId { get; set; }

    [Required, MaxLength(50)]
    public string Role { get; set; } = "viewer";  // owner, editor, viewer

    // Навигационные свойства
    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}