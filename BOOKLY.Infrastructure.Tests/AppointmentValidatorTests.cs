using BOOKLY.Application.Common.Models;
using BOOKLY.Application.Services.AppointmentAggregate;
using BOOKLY.Application.Services.AppointmentAggregate.DTOs;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Entities;
using BOOKLY.Domain.Aggregates.ServiceTypeAggregate.Enum;

namespace BOOKLY.Infrastructure.Tests;

public sealed class AppointmentValidatorTests
{
    private static readonly DateTime ReferenceNow = new(2026, 3, 15, 10, 0, 0);
    private readonly AppointmentValidator _validator = new();

    [Fact]
    public void Validate_ShouldRejectDuplicateFieldValues()
    {
        var serviceType = CreateServiceType(Field(1, "dni", "DNI", ServiceFieldType.Text, isRequired: false));

        var result = _validator.Validate(
            [Value(1, "123"), Value(1, "456")],
            serviceType);

        AssertValidationFailure(result, "No se puede informar m\u00E1s de un valor por cada campo adicional.");
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenServiceTypeHasNoActiveFieldsAndRequestIsEmpty()
    {
        var serviceType = CreateServiceType(Field(1, "dni", "DNI", ServiceFieldType.Text, isRequired: false, isActive: false));

        var result = _validator.Validate([], serviceType);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_ShouldRejectValues_WhenServiceTypeHasNoActiveFields()
    {
        var serviceType = CreateServiceType(Field(1, "dni", "DNI", ServiceFieldType.Text, isRequired: false, isActive: false));

        var result = _validator.Validate([Value(1, "123")], serviceType);

        AssertValidationFailure(result, "El servicio no admite campos adicionales.");
    }

    [Fact]
    public void Validate_ShouldRejectUnknownField()
    {
        var serviceType = CreateServiceType(Field(1, "dni", "DNI", ServiceFieldType.Text, isRequired: false));

        var result = _validator.Validate([Value(999, "123")], serviceType);

        AssertValidationFailure(result, "Uno o m\u00E1s campos enviados no pertenecen al tipo de servicio.");
    }

    [Fact]
    public void Validate_ShouldRejectMissingRequiredField()
    {
        var serviceType = CreateServiceType(Field(1, "dni", "DNI", ServiceFieldType.Text, isRequired: true));

        var result = _validator.Validate([], serviceType);

        AssertValidationFailure(result, "El campo 'DNI' es obligatorio.");
    }

    [Fact]
    public void Validate_ShouldRejectEmptyFieldValue()
    {
        var serviceType = CreateServiceType(Field(1, "dni", "DNI", ServiceFieldType.Text, isRequired: false));

        var result = _validator.Validate([Value(1, "   ")], serviceType);

        AssertValidationFailure(result, "El campo 'DNI' no puede estar vac\u00EDo.");
    }

    [Fact]
    public void Validate_ShouldAcceptValidNumber()
    {
        var serviceType = CreateServiceType(Field(1, "amount", "Importe", ServiceFieldType.Number, isRequired: false));

        var result = _validator.Validate([Value(1, "123.45")], serviceType);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_ShouldRejectInvalidNumber()
    {
        var serviceType = CreateServiceType(Field(1, "amount", "Importe", ServiceFieldType.Number, isRequired: false));

        var result = _validator.Validate([Value(1, "abc")], serviceType);

        AssertValidationFailure(result, "El campo 'Importe' debe contener un n\u00FAmero v\u00E1lido.");
    }

    [Fact]
    public void Validate_ShouldAcceptValidDate()
    {
        var serviceType = CreateServiceType(Field(1, "date", "Fecha", ServiceFieldType.Date, isRequired: false));

        var result = _validator.Validate([Value(1, "2026-03-15")], serviceType);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_ShouldRejectInvalidDate()
    {
        var serviceType = CreateServiceType(Field(1, "date", "Fecha", ServiceFieldType.Date, isRequired: false));

        var result = _validator.Validate([Value(1, "not-a-date")], serviceType);

        AssertValidationFailure(result, "El campo 'Fecha' debe contener una fecha v\u00E1lida.");
    }

    [Fact]
    public void Validate_ShouldAcceptActiveSelectOption()
    {
        var serviceType = CreateServiceType(Field(
            1,
            "color",
            "Color",
            ServiceFieldType.Select,
            isRequired: false,
            options: [Option(10, "red", isActive: true)]));

        var result = _validator.Validate([Value(1, "RED")], serviceType);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("blue")]
    [InlineData("green")]
    public void Validate_ShouldRejectUnknownOrInactiveSelectOption(string optionValue)
    {
        var serviceType = CreateServiceType(Field(
            1,
            "color",
            "Color",
            ServiceFieldType.Select,
            isRequired: false,
            options:
            [
                Option(10, "red", isActive: true),
                Option(11, "blue", isActive: false)
            ]));

        var result = _validator.Validate([Value(1, optionValue)], serviceType);

        AssertValidationFailure(result, "El campo 'Color' debe seleccionar una opci\u00F3n v\u00E1lida.");
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void Validate_ShouldAcceptValidCheckbox(string value)
    {
        var serviceType = CreateServiceType(Field(1, "accepts", "Acepta", ServiceFieldType.Checkbox, isRequired: false));

        var result = _validator.Validate([Value(1, value)], serviceType);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_ShouldRejectInvalidCheckbox()
    {
        var serviceType = CreateServiceType(Field(1, "accepts", "Acepta", ServiceFieldType.Checkbox, isRequired: false));

        var result = _validator.Validate([Value(1, "yes")], serviceType);

        AssertValidationFailure(result, "El campo 'Acepta' debe indicar true o false.");
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenOptionalFieldsAreMissing()
    {
        var serviceType = CreateServiceType(
            Field(1, "notes", "Notas", ServiceFieldType.MultilineText, isRequired: false),
            Field(2, "date", "Fecha", ServiceFieldType.Date, isRequired: false));

        var result = _validator.Validate([], serviceType);

        Assert.True(result.IsSuccess);
    }

    private static ServiceType CreateServiceType(params FieldSpec[] fields)
    {
        var serviceType = ServiceType.Create("Consulta");
        SetProperty(serviceType, nameof(ServiceType.Id), 100);

        foreach (var fieldSpec in fields)
        {
            var field = serviceType.AddField(
                fieldSpec.Key,
                fieldSpec.Label,
                fieldSpec.Type,
                fieldSpec.IsRequired,
                sortOrder: fieldSpec.Id,
                ReferenceNow);

            SetProperty(field, nameof(ServiceTypeFieldDefinition.Id), fieldSpec.Id);

            foreach (var optionSpec in fieldSpec.Options)
            {
                field.AddOption(optionSpec.Value, optionSpec.Value, optionSpec.Id, ReferenceNow);
                var option = field.Options.Single(current => current.Value == optionSpec.Value);
                SetProperty(option, nameof(ServiceTypeFieldOption.Id), optionSpec.Id);

                if (!optionSpec.IsActive)
                    SetProperty(option, nameof(ServiceTypeFieldOption.IsActive), false);
            }

            if (!fieldSpec.IsActive)
                field.Deactivate();
        }

        return serviceType;
    }

    private static FieldSpec Field(
        int id,
        string key,
        string label,
        ServiceFieldType type,
        bool isRequired,
        bool isActive = true,
        IReadOnlyCollection<OptionSpec>? options = null)
        => new(id, key, label, type, isRequired, isActive, options ?? []);

    private static OptionSpec Option(int id, string value, bool isActive)
        => new(id, value, isActive);

    private static CreateAppointmentFieldValueDto Value(int fieldId, string value)
        => new()
        {
            FieldDefinitionId = fieldId,
            Value = value
        };

    private static void AssertValidationFailure(Result result, string expectedMessage)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(expectedMessage, result.Error.Message);
    }

    private static void SetProperty(object target, string propertyName, object value)
        => target.GetType().GetProperty(propertyName)!.SetValue(target, value);

    private sealed record FieldSpec(
        int Id,
        string Key,
        string Label,
        ServiceFieldType Type,
        bool IsRequired,
        bool IsActive,
        IReadOnlyCollection<OptionSpec> Options);

    private sealed record OptionSpec(int Id, string Value, bool IsActive);
}
