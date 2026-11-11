namespace PragmaticVerticalSlices.Api.Features.Products;

public static class ProductPagination
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;

    public static Dictionary<string, string[]> Validate(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            errors["page"] = ["Page must be greater than or equal to 1."];
        }

        if (pageSize is < 1 or > MaximumPageSize)
        {
            errors["pageSize"] = [$"Page size must be between 1 and {MaximumPageSize}."];
        }

        return errors;
    }
}
