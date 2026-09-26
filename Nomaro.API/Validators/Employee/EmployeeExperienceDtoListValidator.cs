using FluentValidation;
using Nomaro.API.DTO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nomaro.API.Validators.Employee
{
    public class EmployeeExperienceDtoListValidator : AbstractValidator<List<EmployeeExperienceDto>>
    {
        public EmployeeExperienceDtoListValidator()
        {
            RuleFor(list => list)
                .NotNull().WithMessage("Employee experience list cannot be null.")
                .Must(list => list.Any()).WithMessage("Employee experience list cannot be empty.");

            RuleForEach(x => x).ChildRules(exp =>
            {
                exp.RuleFor(e => e.IdEmployee)
                    .NotEmpty().WithMessage("IdEmployee is required.");

                exp.RuleFor(e => e.CompanyName)
                    .NotEmpty().WithMessage("CompanyName is required.")
                    .MaximumLength(150).WithMessage("CompanyName must not exceed 150 characters.");

                exp.RuleFor(e => e.Designation)
                    .NotEmpty().WithMessage("Designation is required.")
                    .MaximumLength(150).WithMessage("Designation must not exceed 150 characters.");

                exp.RuleFor(e => e.ReasonForLeaving)
                    .NotEmpty().WithMessage("ReasonForLeaving is required.")
                    .MaximumLength(300).WithMessage("ReasonForLeaving must not exceed 300 characters.");

                // ✅ EmploymentType validation (matches DB CHECK constraint)
                exp.RuleFor(e => e.EmploymentType)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("EmploymentType is required.")
                    .Must(type =>
                    {
                        if (string.IsNullOrWhiteSpace(type)) return false;
                        var t = type.Trim().ToUpperInvariant();
                        return t == "CONSULTANT" || t == "CONTRACT" || t == "FULLTIME";
                    })
                    .WithMessage("EmploymentType must be one of: Consultant, Contract, FullTime.");

                exp.RuleFor(e => e.FromDate)
                    .NotEmpty().WithMessage("FromDate is required.");

                exp.RuleFor(e => e.ToDate)
                    .NotEmpty().WithMessage("ToDate is required.")
                    .GreaterThanOrEqualTo(e => e.FromDate)
                    .WithMessage("ToDate cannot be earlier than FromDate.");

                exp.RuleFor(e => e.ExperienceInYears)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("ExperienceInYears must be >= 0.");

                exp.RuleFor(e => e.LastDrawnSalary)
                    .GreaterThanOrEqualTo(0)
                    .When(e => e.LastDrawnSalary.HasValue)
                    .WithMessage("LastDrawnSalary must be >= 0.");
                exp.RuleFor(e => e.ExperienceDocument)
    .Must(file =>
    {
        if (file == null) return true; // ✅ file is optional
        var allowedContentTypes = new[]
        {
            "application/pdf",
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp"
        };

        return allowedContentTypes.Contains(file.ContentType.ToLower());
    })
    .WithMessage("ExperienceDocument must be a PDF or an image (jpg, jpeg, png, webp).");

                exp.RuleFor(e => e.ExperienceDocument)
                    .Must(file =>
                    {
                        if (file == null) return true;
                        // ✅ Max 5 MB
                        return file.Length <= 5 * 1024 * 1024;
                    })
                    .WithMessage("ExperienceDocument size must be less than or equal to 5 MB.");

            });
        }
    }
}

