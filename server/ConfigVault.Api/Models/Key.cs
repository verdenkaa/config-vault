using System.ComponentModel.DataAnnotations;

namespace ConfigVault.Api.Models;

public class Key
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProjectId { get; set; }   // внешний ключ

    [Required, MaxLength(200)]
    public string KeyName { get; set; } = string.Empty;

    public bool IsSecret { get; set; }

    [Required]
    public string ValueEncrypted { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Навигационные свойства
    public Project Project { get; set; } = null!;
    public ICollection<KeyHistory> KeyHistories { get; set; } = new List<KeyHistory>();
}