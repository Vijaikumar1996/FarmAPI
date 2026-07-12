using FluentValidation;
using static FarmAPI.DTOs.ProductDto;

namespace FarmAPI.Validators
{
    public class ProductValidator
    {
        public class CreateProductValidator : AbstractValidator<CreateProductRequestDto>
        {
            public CreateProductValidator()
            {
                RuleFor(x => x.ProductCode)
                    .NotEmpty()
                    .MaximumLength(20);

                RuleFor(x => x.ProductName)
                    .NotEmpty()
                    .MaximumLength(200);

                RuleFor(x => x.CategoryId)
                    .GreaterThan((short)0);

                RuleFor(x => x.LitresPerUnit)
                    .GreaterThanOrEqualTo(0)
                    .When(x => x.LitresPerUnit.HasValue);

                RuleFor(x => x.DisplayOrder)
                    .GreaterThanOrEqualTo(0);
            }
        }

        public class UpdateProductValidator : AbstractValidator<UpdateProductRequestDto>
        {
            public UpdateProductValidator()
            {
                RuleFor(x => x.ProductCode)
                    .NotEmpty()
                    .MaximumLength(20);

                RuleFor(x => x.ProductName)
                    .NotEmpty()
                    .MaximumLength(200);

                RuleFor(x => x.CategoryId)
                    .GreaterThan((short)0);

                RuleFor(x => x.LitresPerUnit)
                    .GreaterThanOrEqualTo(0)
                    .When(x => x.LitresPerUnit.HasValue);

                RuleFor(x => x.DisplayOrder)
                    .GreaterThanOrEqualTo(0);
            }
        }
    }
}
