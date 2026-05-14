namespace ConfigVault.Api.Exceptions;

/// <summary>
/// Сущность не найдена (проект, ключ, пользователь).
/// Преобразуется в HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
