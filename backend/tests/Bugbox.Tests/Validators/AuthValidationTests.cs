using Bugbox.Api.Validators;
using Bugbox.Core.DTOs.Auth;
using FluentAssertions;

namespace Bugbox.Tests.Validators;

public class AuthValidationTests
{
    private readonly RegisterRequestValidator _registerValidator = new();
    private readonly LoginRequestValidator _loginValidator = new();

    [Fact]
    public void RegisterValidator_WithValidData_PassesValidation()
    {
        var request = new RegisterRequest("Valid Name", "valid@example.com", "Password123!");
        var result = _registerValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    [InlineData("@nodomain.com")]
    [InlineData("")]
    public void RegisterValidator_WithInvalidEmail_FailsValidation(string invalidEmail)
    {
        var request = new RegisterRequest("Valid Name", invalidEmail, "Password123!");
        var result = _registerValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")] // 7 characters, min is 8
    public void RegisterValidator_WithPasswordUnder8Characters_FailsValidation(string shortPassword)
    {
        var request = new RegisterRequest("Valid Name", "user@example.com", shortPassword);
        var result = _registerValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterValidator_WithEmptyName_FailsValidation(string invalidName)
    {
        var request = new RegisterRequest(invalidName, "user@example.com", "Password123!");
        var result = _registerValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Name));
    }

    [Fact]
    public void LoginValidator_WithValidData_PassesValidation()
    {
        var request = new LoginRequest("user@example.com", "Password123!");
        var result = _loginValidator.Validate(request);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginValidator_WithEmptyFields_FailsValidation()
    {
        var request = new LoginRequest("", "");
        var result = _loginValidator.Validate(request);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterOrEqualTo(2);
    }
}
