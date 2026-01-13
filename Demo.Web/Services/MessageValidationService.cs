using FluentValidation;
using FluentValidation.Results;

namespace Demo.Web.Services;

public interface IMessageValidationService
{
    Task<ValidationResult> ValidateMessageAsync<T>(T message) where T : class;
    Task<(bool IsValid, List<string> Errors)> ValidateAndLogAsync<T>(T message, string messageType) where T : class;
}

public class MessageValidationService : IMessageValidationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessageValidationService> _logger;

    public MessageValidationService(IServiceProvider serviceProvider, ILogger<MessageValidationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateMessageAsync<T>(T message) where T : class
    {
        var validator = _serviceProvider.GetService<IValidator<T>>();
        
        if (validator == null)
        {
            _logger.LogWarning($"?? Geen validator gevonden voor type {typeof(T).Name}");
            return new ValidationResult(); // Valid by default if no validator
        }

        var result = await validator.ValidateAsync(message);
        return result;
    }

    public async Task<(bool IsValid, List<string> Errors)> ValidateAndLogAsync<T>(T message, string messageType) where T : class
    {
        var result = await ValidateMessageAsync(message);

        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning($"? Validatie gefaald voor {messageType}: {string.Join(", ", errors)}");
            return (false, errors);
        }

        _logger.LogInformation($"? Validatie succesvol voor {messageType}");
        return (true, new List<string>());
    }
}
