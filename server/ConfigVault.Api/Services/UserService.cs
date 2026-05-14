using ConfigVault.Api.Data.Repositories;
using ConfigVault.Api.Exceptions;
using ConfigVault.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace ConfigVault.Api.Services;

public interface IUserService
{
    /// <summary>
    /// Зарегистрировать нового пользователя. Возвращает созданного пользователя.
    /// </summary>
    Task<User> RegisterAsync(string login, string password);

    /// <summary>
    /// Проверить логин/пароль и вернуть пользователя, либо null.
    /// </summary>
    Task<User?> LoginAsync(string login, string password);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<User> RegisterAsync(string login, string password)
    {
        // Проверка на занятость логина
        if (await _userRepo.ExistsByLoginAsync(login))
            throw new Exceptions.ValidationException("Пользователь с таким логином уже существует.");

        // Хешируем пароль
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Login = login,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user);
        return user;
    }

    public async Task<User?> LoginAsync(string login, string password)
    {
        var user = await _userRepo.GetByLoginAsync(login);
        if (user == null)
            return null;

        // Проверяем пароль
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return user;
    }
}