using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace ConfigVault.Api.Models;

public class Project
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int Version { get; set; } = 1;  // для отслеживания изменений

    // Навигационные свойства
    public ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
    public ICollection<Key> Keys { get; set; } = new List<Key>();
}