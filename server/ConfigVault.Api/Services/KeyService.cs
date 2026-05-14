using ConfigVault.Api.Data.Repositories;
using ConfigVault.Api.Exceptions;
using ConfigVault.Api.Models;
using System.Security.Cryptography;
using System.Text;

namespace ConfigVault.Api.Services;

public interface IKeyService
{
    /// <summary>
    /// Создать или обновить ключ. Возвращает актуальный ключ.
    /// </summary>
    Task<Key> SetAsync(Guid projectId, string keyName, string value, bool isSecret, Guid userId);

    /// <summary>
    /// Получить значение ключа (расшифрованное).
    /// </summary>
    Task<Key> GetAsync(Guid projectId, string keyName, Guid userId);

    /// <summary>
    /// Получить все ключи проекта (значения остаются зашифрованными, расшифровка в контроллере при необходимости).
    /// </summary>
    Task<List<Key>> GetAllByProjectAsync(Guid projectId, Guid userId);

    /// <summary>
    /// Удалить ключ.
    /// </summary>
    Task DeleteAsync(Guid projectId, string keyName, Guid userId);

    /// <summary>
    /// Получить историю изменений ключа.
    /// </summary>
    Task<List<KeyHistory>> GetHistoryAsync(Guid projectId, string keyName, Guid userId);
}

public class KeyService : IKeyService
{
    private readonly IKeyRepository _keyRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IEncryptionService _encryption;
    private readonly IAuditService _audit;
    private readonly IProjectService _projectService;

    public KeyService(
        IKeyRepository keyRepo,
        IProjectRepository projectRepo,
        IEncryptionService encryption,
        IAuditService audit,
        IProjectService projectService)
    {
        _keyRepo = keyRepo;
        _projectRepo = projectRepo;
        _encryption = encryption;
        _audit = audit;
        _projectService = projectService;
    }

    public async Task<Key> SetAsync(Guid projectId, string keyName, string value, bool isSecret, Guid userId)
    {
        // Проверяем права (editor+)
        await _projectService.EnsureUserHasRoleAsync(projectId, userId, "owner", "editor");

        string encryptedValue = _encryption.Encrypt(value);

        var existingKey = await _keyRepo.GetByProjectAndNameAsync(projectId, keyName);

        if (existingKey != null)
        {
            // Обновление: сохраняем предыдущий хэш в аудит
            string oldHash = ComputeHash(existingKey.ValueEncrypted);
            await _audit.AddEntryAsync(existingKey.Id, existingKey.Version, oldHash, userId);

            // Обновляем поля
            existingKey.ValueEncrypted = encryptedValue;
            existingKey.IsSecret = isSecret;
            existingKey.Version++;
            existingKey.UpdatedAt = DateTime.UtcNow;

            _keyRepo.Update(existingKey);
            await _projectRepo.SaveChangesAsync(); // Сохраняем изменения ключа и версию проекта

            await IncrementProjectVersionAsync(projectId);
            return existingKey;
        }
        else
        {
            // Создание нового ключа
            var newKey = new Key
            {
                ProjectId = projectId,
                KeyName = keyName,
                IsSecret = isSecret,
                ValueEncrypted = encryptedValue,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _keyRepo.AddAsync(newKey);
            // Инкрементируем версию проекта (после добавления ключа)
            await IncrementProjectVersionAsync(projectId);
            return newKey;
        }
    }

    public async Task<Key> GetAsync(Guid projectId, string keyName, Guid userId)
    {
        await _projectService.EnsureUserHasRoleAsync(projectId, userId, "owner", "editor", "viewer");

        var key = await _keyRepo.GetByProjectAndNameAsync(projectId, keyName)
                  ?? throw new NotFoundException("Ключ не найден.");
        return key;
    }

    public async Task<List<Key>> GetAllByProjectAsync(Guid projectId, Guid userId)
    {
        await _projectService.EnsureUserHasRoleAsync(projectId, userId, "owner", "editor", "viewer");
        return await _keyRepo.GetAllByProjectIdAsync(projectId);
    }

    public async Task DeleteAsync(Guid projectId, string keyName, Guid userId)
    {
        await _projectService.EnsureUserHasRoleAsync(projectId, userId, "owner", "editor");

        var key = await _keyRepo.GetByProjectAndNameAsync(projectId, keyName)
                  ?? throw new NotFoundException("Ключ не найден.");
        await _keyRepo.DeleteAsync(key.Id);
        await IncrementProjectVersionAsync(projectId);
    }

    public async Task<List<KeyHistory>> GetHistoryAsync(Guid projectId, string keyName, Guid userId)
    {
        await _projectService.EnsureUserHasRoleAsync(projectId, userId, "owner", "editor", "viewer");
        var key = await _keyRepo.GetByProjectAndNameAsync(projectId, keyName)
                  ?? throw new NotFoundException("Ключ не найден.");
        return await _audit.GetHistoryByKeyIdAsync(key.Id);
    }

    private string ComputeHash(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private async Task IncrementProjectVersionAsync(Guid projectId)
    {
        var project = await _projectRepo.GetByIdAsync(projectId);
        if (project != null)
        {
            project.Version++;
            _projectRepo.Update(project);
            await _projectRepo.SaveChangesAsync();
        }
    }
}