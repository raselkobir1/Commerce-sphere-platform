using CommerceSphere.ProductService.Application.DTOs.Requests;
using FluentValidation;

namespace CommerceSphere.ProductService.Application.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Category name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Category name is required.").MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
