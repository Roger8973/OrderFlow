using OrderFlow.Domain;
using OrderFlow.Domain.Customers;

namespace OrderFlow.UnitTests.Customers;

public class CustomerTests
{
    [Fact]
    public void Create_ReturnsCustomer_ForValidData()
    {
        var customer = Customer.Create("Maria Silva", "maria@example.com", "+55 11 99999-0000");

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Maria Silva", customer.Name);
        Assert.Equal("maria@example.com", customer.Email);
        Assert.Equal("+55 11 99999-0000", customer.Phone);
    }

    [Fact]
    public void Create_GeneratesDistinctIds()
    {
        var first = Customer.Create("Maria", "maria@example.com", null);
        var second = Customer.Create("Maria", "maria@example.com", null);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Create_TrimsSurroundingWhitespace()
    {
        var customer = Customer.Create("  Maria Silva  ", " maria@example.com ", "  123  ");

        Assert.Equal("Maria Silva", customer.Name);
        Assert.Equal("maria@example.com", customer.Email);
        Assert.Equal("123", customer.Phone);
    }

    [Fact]
    public void Create_LowerCasesEmail()
    {
        var customer = Customer.Create("Maria", "Maria@Example.COM", null);

        Assert.Equal("maria@example.com", customer.Email);
    }

    [Fact]
    public void Create_IsAlwaysActive()
    {
        var customer = Customer.Create("Maria", "maria@example.com", null);

        Assert.True(customer.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_TreatsBlankPhoneAsNull(string? phone)
    {
        var customer = Customer.Create("Maria", "maria@example.com", phone);

        Assert.Null(customer.Phone);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Throws_ForMissingOrBlankName(string? name)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Customer.Create(name, "maria@example.com", null));

        Assert.Equal(["name"], exception.Errors.Keys);
    }

    [Fact]
    public void Create_Throws_ForNameLongerThanMaximum()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Customer.Create(new string('a', Customer.NameMaxLength + 1), "maria@example.com", null));

        Assert.Equal(["name"], exception.Errors.Keys);
    }

    [Fact]
    public void Create_Accepts_NameAtMaximumLengthAfterTrimming()
    {
        var name = new string('a', Customer.NameMaxLength);

        var customer = Customer.Create($" {name} ", "maria@example.com", null);

        Assert.Equal(name, customer.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("maria@")]
    [InlineData("maria@@example.com")]
    [InlineData("ma@ria@example.com")]
    [InlineData("ma ria@example.com")]
    public void Create_Throws_ForMissingOrMalformedEmail(string? email)
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Customer.Create("Maria", email, null));

        Assert.Equal(["email"], exception.Errors.Keys);
    }

    [Fact]
    public void Create_Throws_ForEmailLongerThanMaximum()
    {
        var email = new string('a', Customer.EmailMaxLength) + "@example.com";

        var exception = Assert.Throws<DomainValidationException>(
            () => Customer.Create("Maria", email, null));

        Assert.Equal(["email"], exception.Errors.Keys);
    }

    [Fact]
    public void Create_Throws_ForPhoneLongerThanMaximum()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Customer.Create("Maria", "maria@example.com", new string('1', Customer.PhoneMaxLength + 1)));

        Assert.Equal(["phone"], exception.Errors.Keys);
    }

    [Fact]
    public void Create_ReportsAllInvalidFieldsAtOnce()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Customer.Create("   ", "not-an-email", new string('1', Customer.PhoneMaxLength + 1)));

        Assert.Equal(3, exception.Errors.Count);
        Assert.Contains("name", exception.Errors.Keys);
        Assert.Contains("email", exception.Errors.Keys);
        Assert.Contains("phone", exception.Errors.Keys);
    }
}
