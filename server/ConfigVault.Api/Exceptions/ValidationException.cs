namespace ConfigVault.Api.Exceptions;

/// <summary>
/// Ошибка валидации входных данных (некорректный запрос).
/// Преобразуется в HTTP 400.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}