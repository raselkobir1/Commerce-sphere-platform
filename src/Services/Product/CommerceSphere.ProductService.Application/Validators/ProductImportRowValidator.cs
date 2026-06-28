using CommerceSphere.ProductService.Application.DTOs.Requests;
using FluentValidation;

namespace CommerceSphere.ProductService.Application.Validators;

// Per-row validation for the bulk importer. Mirrors CreateProductRequestValidator and the
// Product.Create invariants — the COPY path bypasses the domain factory, so this is the only
// gate that protects those invariants for imported rows.
public class ProductImportRowValidator : AbstractValidator<ProductImportRow>
{
    public ProductImportRowValidator()
    {
        // A cell-level conversion failure (e.g. a non-numeric Price) short-circuits everything
        // else — report it verbatim and stop, since the typed fields can't be trusted.
        RuleFor(x => x.ParseError)
            .Empty().WithMessage(x => x.ParseError);

        When(x => string.IsNullOrEmpty(x.ParseError), () =>
        {
            ConfigureFieldRules();
        });
    }

    private void ConfigureFieldRules()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(100).WithMessage("SKU must not exceed 100 characters.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("SKU can only contain letters, digits, hyphens, and underscores.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL must not exceed 500 characters.")
            .Must(url => string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Image URL must be a valid absolute URI.");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Initial stock must be non-negative.");
    }
}
