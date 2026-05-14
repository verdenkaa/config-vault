using System.ComponentModel.DataAnnotations;

namespace ConfigVault.Api.Models;

public class KeyHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid KeyId { get; set; }      // внешний ключ

    public int Version { get; set; }      // версия до изменения

    [Required, MaxLength(64)]
    public string ValueHash { get; set; } = string.Empty;  // SHA-256 хэш

    [Required, MaxLength(100)]
    public string ChangedBy { get; set; } = string.Empty;  // логин пользователя

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    // Навигационное свойство
    public Key Key { get; set; } = null!;
}
