using CommerceSphere.ProductService.Application.DTOs.Requests;
using FluentValidation;

namespace CommerceSphere.ProductService.Application.Validators;

public class CreateBannerRequestValidator : AbstractValidator<CreateBannerRequest>
{
    public CreateBannerRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Banner title is required.").MaximumLength(150);
        RuleFor(x => x.Subtitle).MaximumLength(300);
        RuleFor(x => x.ImageUrl).NotEmpty().WithMessage("Banner image URL is required.").MaximumLength(1000);
        RuleFor(x => x.LinkUrl).MaximumLength(1000);
    }
}

public class UpdateBannerRequestValidator : AbstractValidator<UpdateBannerRequest>
{
    public UpdateBannerRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Banner title is required.").MaximumLength(150);
        RuleFor(x => x.Subtitle).MaximumLength(300);
        RuleFor(x => x.ImageUrl).NotEmpty().WithMessage("Banner image URL is required.").MaximumLength(1000);
        RuleFor(x => x.LinkUrl).MaximumLength(1000);
    }
}
