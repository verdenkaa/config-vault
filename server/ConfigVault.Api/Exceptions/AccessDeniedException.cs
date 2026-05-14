namespace ConfigVault.Api.Exceptions;

/// <summary>
/// Доступ запрещён (пользователь не состоит в проекте или не имеет нужной роли).
/// Преобразуется в HTTP 403.
/// </summary>
public class AccessDeniedException : Exception
{
    public AccessDeniedException(string message) : base(message) { }
}