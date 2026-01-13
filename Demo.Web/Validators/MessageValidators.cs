using FluentValidation;
using Demo.Web.Models;

namespace Demo.Web.Validators;

public class OrderMessageValidator : AbstractValidator<OrderMessage>
{
    public OrderMessageValidator()
    {
        RuleFor(x => x.OrderNummer)
            .NotEmpty().WithMessage("OrderNummer is verplicht")
            .Matches(@"^ORD\d{14}$").WithMessage("OrderNummer moet format 'ORD' + 14 cijfers hebben");

        RuleFor(x => x.KlantNaam)
            .NotEmpty().WithMessage("KlantNaam is verplicht")
            .MinimumLength(2).WithMessage("KlantNaam moet minimaal 2 karakters zijn")
            .MaximumLength(100).WithMessage("KlantNaam mag maximaal 100 karakters zijn");

        RuleFor(x => x.KlantEmail)
            .NotEmpty().WithMessage("KlantEmail is verplicht")
            .EmailAddress().WithMessage("Ongeldig email formaat");

        RuleFor(x => x.OrderDatum)
            .NotEmpty().WithMessage("OrderDatum is verplicht")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .WithMessage("OrderDatum kan niet in de toekomst liggen");

        RuleFor(x => x.TotaalBedrag)
            .GreaterThan(0).WithMessage("TotaalBedrag moet groter dan 0 zijn")
            .LessThan(100000).WithMessage("TotaalBedrag mag niet hoger zijn dan €100.000");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order moet minimaal 1 item bevatten")
            .Must(items => items.Count <= 50).WithMessage("Order mag maximaal 50 items bevatten");

        RuleForEach(x => x.Items).SetValidator(new OrderItemMessageValidator());
    }
}

public class OrderItemMessageValidator : AbstractValidator<OrderItemMessage>
{
    public OrderItemMessageValidator()
    {
        RuleFor(x => x.BoekTitel)
            .NotEmpty().WithMessage("BoekTitel is verplicht")
            .MaximumLength(200).WithMessage("BoekTitel mag maximaal 200 karakters zijn");

        RuleFor(x => x.Aantal)
            .GreaterThan(0).WithMessage("Aantal moet groter dan 0 zijn")
            .LessThanOrEqualTo(100).WithMessage("Aantal mag niet meer dan 100 zijn per item");

        RuleFor(x => x.Prijs)
            .GreaterThan(0).WithMessage("Prijs moet groter dan 0 zijn")
            .LessThan(10000).WithMessage("Prijs mag niet hoger zijn dan €10.000 per item");
    }
}

public class EntityChangeMessageValidator : AbstractValidator<EntityChangeMessage>
{
    public EntityChangeMessageValidator()
    {
        RuleFor(x => x.EntityType)
            .IsInEnum().WithMessage("Ongeldig EntityType");

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Ongeldig ActionType");

        RuleFor(x => x.EntityId)
            .GreaterThan(0).WithMessage("EntityId moet groter dan 0 zijn");

        RuleFor(x => x.EntityName)
            .NotEmpty().WithMessage("EntityName is verplicht")
            .MaximumLength(200).WithMessage("EntityName mag maximaal 200 karakters zijn");

        RuleFor(x => x.Timestamp)
            .NotEmpty().WithMessage("Timestamp is verplicht")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .WithMessage("Timestamp kan niet in de toekomst liggen");
    }
}

public class KlantDeletedMessageValidator : AbstractValidator<KlantDeletedMessage>
{
    public KlantDeletedMessageValidator()
    {
        RuleFor(x => x.KlantId)
            .GreaterThan(0).WithMessage("KlantId moet groter dan 0 zijn");

        RuleFor(x => x.KlantNaam)
            .NotEmpty().WithMessage("KlantNaam is verplicht");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is verplicht")
            .EmailAddress().WithMessage("Ongeldig email formaat");

        RuleFor(x => x.DeletedAt)
            .NotEmpty().WithMessage("DeletedAt is verplicht")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
                .WithMessage("DeletedAt kan niet in de toekomst liggen");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is verplicht")
            .MaximumLength(500).WithMessage("Reason mag maximaal 500 karakters zijn");
    }
}
