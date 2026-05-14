using System.ComponentModel.DataAnnotations;

namespace ConfigVault.Api.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Навигационное свойство для связи многие-ко-многим
    public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
}