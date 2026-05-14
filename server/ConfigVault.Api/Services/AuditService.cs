using ConfigVault.Api.Data.Repositories;
using ConfigVault.Api.Models;

namespace ConfigVault.Api.Services;

public interface IAuditService
{
    /// <summary>
    /// Добавить запись в историю изменений ключа.
    /// </summary>
    Task AddEntryAsync(Guid keyId, int previousVersion, string valueHash, Guid changedByUserId);

    /// <summary>
    /// Получить историю изменений по идентификатору ключа.
    /// </summary>
    Task<List<KeyHistory>> GetHistoryByKeyIdAsync(Guid keyId);
}

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepo;
    private readonly IUserRepository _userRepo;

    public AuditService(IAuditRepository auditRepo, IUserRepository userRepo)
    {
        _auditRepo = auditRepo;
        _userRepo = userRepo;
    }

    public async Task AddEntryAsync(Guid keyId, int previousVersion, string valueHash, Guid changedByUserId)
    {
        // Получаем логин пользователя
        var user = await _userRepo.GetByIdAsync(changedByUserId);
        string changedBy = user?.Login ?? changedByUserId.ToString();

        var entry = new KeyHistory
        {
            KeyId = keyId,
            Version = previousVersion,
            ValueHash = valueHash,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow
        };
        await _auditRepo.AddAsync(entry);
    }

    public async Task<List<KeyHistory>> GetHistoryByKeyIdAsync(Guid keyId)
    {
        return await _auditRepo.GetHistoryByKeyIdAsync(keyId);
    }
}