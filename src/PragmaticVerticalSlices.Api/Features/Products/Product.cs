namespace PragmaticVerticalSlices.Api.Features.Products;

public sealed class Product
{
    private Product()
    {
    }

    public Product(string name, string? description, decimal price)
    {
        Id = Guid.NewGuid();
        Update(name, description, price);
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public void Update(string name, string? description, decimal price)
    {
        Name = name;
        Description = description;
        Price = price;
    }
}
