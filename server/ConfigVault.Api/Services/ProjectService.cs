using ConfigVault.Api.Data.Repositories;
using ConfigVault.Api.Exceptions;
using ConfigVault.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace ConfigVault.Api.Services;

public interface IProjectService
{
    /// <summary>
    /// Создать новый проект и сделать пользователя владельцем.
    /// </summary>
    Task<Project> CreateAsync(string name, Guid ownerUserId);

    /// <summary>
    /// Получить все проекты, где состоит пользователь.
    /// </summary>
    Task<List<Project>> GetUserProjectsAsync(Guid userId);

    /// <summary>
    /// Получить проект по ID с проверкой доступа (viewer+).
    /// </summary>
    Task<Project> GetByIdAsync(Guid projectId, Guid currentUserId);

    /// <summary>
    /// Удалить проект (только владелец).
    /// </summary>
    Task DeleteAsync(Guid projectId, Guid currentUserId);

    /// <summary>
    /// Добавить участника (только владелец).
    /// </summary>
    Task AddMemberAsync(Guid projectId, Guid newUserId, string role, Guid currentUserId);

    /// <summary>
    /// Удалить участника (только владелец).
    /// </summary>
    Task RemoveMemberAsync(Guid projectId, Guid userIdToRemove, Guid currentUserId);

    /// <summary>
    /// Получить список участников проекта (viewer+).
    /// </summary>
    Task<List<UserProject>> GetMembersAsync(Guid projectId, Guid currentUserId);

    /// <summary>
    /// Проверить, что пользователь имеет хотя бы одну из допустимых ролей в проекте.
    /// </summary>
    Task EnsureUserHasRoleAsync(Guid projectId, Guid userId, params string[] allowedRoles);
}

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepo;
    private readonly IUserRepository _userRepo;

    public ProjectService(IProjectRepository projectRepo, IUserRepository userRepo)
    {
        _projectRepo = projectRepo;
        _userRepo = userRepo;
    }

    public async Task<Project> CreateAsync(string name, Guid ownerUserId)
    {
        var project = new Project { Name = name };
        await _projectRepo.AddAsync(project);

        // Назначаем создателя владельцем
        await _projectRepo.AddMemberAsync(project.Id, ownerUserId, "owner");
        return project;
    }

    public async Task<List<Project>> GetUserProjectsAsync(Guid userId)
    {
        return await _projectRepo.GetAllByUserIdAsync(userId);
    }

    public async Task<Project> GetByIdAsync(Guid projectId, Guid currentUserId)
    {
        var project = await _projectRepo.GetByIdAsync(projectId)
                      ?? throw new NotFoundException("Проект не найден.");

        await EnsureUserHasRoleAsync(projectId, currentUserId, "owner", "editor", "viewer");
        return project;
    }

    public async Task DeleteAsync(Guid projectId, Guid currentUserId)
    {
        await EnsureUserHasRoleAsync(projectId, currentUserId, "owner");
        await _projectRepo.DeleteAsync(projectId);
    }

    public async Task AddMemberAsync(Guid projectId, Guid newUserId, string role, Guid currentUserId)
    {
        await EnsureUserHasRoleAsync(projectId, currentUserId, "owner");

        // Проверяем существование пользователя
        var user = await _userRepo.GetByIdAsync(newUserId)
                   ?? throw new NotFoundException("Пользователь не найден.");

        // Проверяем, что он ещё не участник
        var existingRole = await _projectRepo.GetUserRoleAsync(projectId, newUserId);
        if (existingRole != null)
            throw new Exceptions.ValidationException("Пользователь уже состоит в проекте.");

        await _projectRepo.AddMemberAsync(projectId, newUserId, role);
    }

    public async Task RemoveMemberAsync(Guid projectId, Guid userIdToRemove, Guid currentUserId)
    {
        await EnsureUserHasRoleAsync(projectId, currentUserId, "owner");
        await _projectRepo.RemoveMemberAsync(projectId, userIdToRemove);
    }

    public async Task<List<UserProject>> GetMembersAsync(Guid projectId, Guid currentUserId)
    {
        await EnsureUserHasRoleAsync(projectId, currentUserId, "owner", "editor", "viewer");
        return await _projectRepo.GetMembersAsync(projectId);
    }

    public async Task EnsureUserHasRoleAsync(Guid projectId, Guid userId, params string[] allowedRoles)
    {
        var role = await _projectRepo.GetUserRoleAsync(projectId, userId)
            ?? throw new AccessDeniedException("У вас нет доступа к этому проекту.");

        if (!allowedRoles.Contains(role))
            throw new AccessDeniedException("Недостаточно прав для выполнения операции.");
    }
}