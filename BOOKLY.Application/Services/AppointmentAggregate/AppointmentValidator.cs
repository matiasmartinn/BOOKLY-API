using System.Globalization;
using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Enum;

namespace BOOKLY.Application.Services.AppointmentAggregate;

public interface IAppointmentValidator
{
    Result Validate(
        IReadOnlyCollection<CreateAppointmentFieldValueDto> fieldsValues,
        ServiceType serviceType);
}
public sealed class AppointmentValidator : IAppointmentValidator
{
    public Result Validate(
           IReadOnlyCollection<CreateAppointmentFieldValueDto> fieldValues,
           ServiceType serviceType)
    {
        var activeFields = serviceType.FieldDefinitions
            .Where(field => field.IsActive)
            .ToList();

        if (fieldValues.GroupBy(field => field.FieldDefinitionId).Any(group => group.Count() > 1))
            return Result.Failure(Error.Validation("No se puede informar más de un valor por cada campo adicional."));

        if (activeFields.Count == 0)
        {
            if (fieldValues.Count > 0)
                return Result.Failure(Error.Validation("El servicio no admite campos adicionales."));

            return Result.Success();
        }

        foreach (var fieldValue in fieldValues)
        {
            var field = activeFields.FirstOrDefault(definition => definition.Id == fieldValue.FieldDefinitionId);
            if (field == null)
                return Result.Failure(Error.Validation("Uno o más campos enviados no pertenecen al tipo de servicio."));

            if (string.IsNullOrWhiteSpace(fieldValue.Value))
                return Result.Failure(Error.Validation($"El campo '{field.Label.Value}' no puede estar vacío."));

            if (!IsFieldValueValid(field, fieldValue.Value))
                return Result.Failure(Error.Validation(BuildInvalidFieldValueMessage(field)));
        }

        foreach (var requiredField in activeFields.Where(field => field.IsRequired))
        {
            var hasValue = fieldValues.Any(fieldValue =>
                fieldValue.FieldDefinitionId == requiredField.Id &&
                !string.IsNullOrWhiteSpace(fieldValue.Value));

            if (!hasValue)
                return Result.Failure(Error.Validation($"El campo '{requiredField.Label.Value}' es obligatorio."));
        }

        return Result.Success();
    }

    private static bool IsFieldValueValid(ServiceTypeFieldDefinition field, string value)
    {
        var trimmedValue = value.Trim();

        return field.FieldType switch
        {
            ServiceFieldType.Text => true,
            ServiceFieldType.MultilineText => true,
            ServiceFieldType.Number => decimal.TryParse(
                trimmedValue,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out _) || decimal.TryParse(trimmedValue, out _),
            ServiceFieldType.Date => DateOnly.TryParse(trimmedValue, out _) || DateTime.TryParse(trimmedValue, out _),
            ServiceFieldType.Select => field.Options.Any(option =>
                option.IsActive &&
                string.Equals(option.Value, trimmedValue, StringComparison.OrdinalIgnoreCase)),
            ServiceFieldType.Checkbox => bool.TryParse(trimmedValue, out _),
            _ => false
        };
    }

    private static string BuildInvalidFieldValueMessage(ServiceTypeFieldDefinition field)
    {
        return field.FieldType switch
        {
            ServiceFieldType.Number => $"El campo '{field.Label.Value}' debe contener un número válido.",
            ServiceFieldType.Date => $"El campo '{field.Label.Value}' debe contener una fecha válida.",
            ServiceFieldType.Select => $"El campo '{field.Label.Value}' debe seleccionar una opción válida.",
            ServiceFieldType.Checkbox => $"El campo '{field.Label.Value}' debe indicar true o false.",
            _ => $"El campo '{field.Label.Value}' contiene un valor inválido."
        };
    }
}
