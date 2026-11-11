using System.ComponentModel.DataAnnotations;

namespace PragmaticVerticalSlices.Api.Features.Products;

public static class ProductRequestValidation
{
    public static Dictionary<string, string[]> Validate(IReadOnlyList<ProductRequest> requests)
    {
        var errors = new Dictionary<string, string[]>();
        if (requests.Count == 0)
        {
            errors["products"] = ["At least one product is required."];
            return errors;
        }

        for (var index = 0; index < requests.Count; index++)
        {
            ProductRequest? request = requests[index];
            if (request is null)
            {
                errors[$"products[{index}]"] = ["A product is required."];
                continue;
            }

            var validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(
                request,
                new ValidationContext(request),
                validationResults,
                validateAllProperties: true);

            foreach (var result in validationResults)
            {
                foreach (var memberName in result.MemberNames.DefaultIfEmpty(string.Empty))
                {
                    var key = string.IsNullOrEmpty(memberName)
                        ? $"products[{index}]"
                        : $"products[{index}].{memberName}";
                    errors[key] = [result.ErrorMessage ?? "The value is invalid."];
                }
            }
        }

        return errors;
    }
}
