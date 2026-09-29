namespace OrderFlow.Domain.Customers;

public sealed class Customer
{
    public const int NameMaxLength = 200;
    public const int EmailMaxLength = 254;
    public const int PhoneMaxLength = 30;

    private Customer(Guid id, string name, string email, string? phone)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
        IsActive = true;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Email { get; }

    public string? Phone { get; }

    public bool IsActive { get; }

    public static Customer Create(string? name, string? email, string? phone)
    {
        var trimmedName = name?.Trim();
        var normalizedEmail = email?.Trim().ToLowerInvariant();
        var trimmedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(trimmedName))
        {
            errors["name"] = ["Name is required."];
        }
        else if (trimmedName.Length > NameMaxLength)
        {
            errors["name"] = [$"Name must be at most {NameMaxLength} characters."];
        }

        if (string.IsNullOrEmpty(normalizedEmail))
        {
            errors["email"] = ["Email is required."];
        }
        else if (normalizedEmail.Length > EmailMaxLength)
        {
            errors["email"] = [$"Email must be at most {EmailMaxLength} characters."];
        }
        else if (!IsWellFormedEmail(normalizedEmail))
        {
            errors["email"] = ["Email is not a well-formed email address."];
        }

        if (trimmedPhone is { Length: > PhoneMaxLength })
        {
            errors["phone"] = [$"Phone must be at most {PhoneMaxLength} characters."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        return new Customer(Guid.NewGuid(), trimmedName!, normalizedEmail!, trimmedPhone);
    }

    // Deliberately simple: a single '@', non-empty local and domain parts, no whitespace.
    private static bool IsWellFormedEmail(string email)
    {
        if (email.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var at = email.IndexOf('@');
        return at > 0 && at == email.LastIndexOf('@') && at < email.Length - 1;
    }
}
