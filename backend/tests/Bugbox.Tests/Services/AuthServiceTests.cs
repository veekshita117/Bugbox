using Bugbox.Core.DTOs.Auth;
using Bugbox.Core.Exceptions;
using Bugbox.Infrastructure.Services;
using Bugbox.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Bugbox.Tests.Services;

public class AuthServiceTests
{
    private readonly AuthService _sut;
    private readonly BcryptPasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public AuthServiceTests()
    {
        var dbContext = TestDbContextFactory.Create();
        _passwordHasher = new BcryptPasswordHasher();

        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = "super_secret_test_key_for_unit_tests_that_is_long_enough",
            Issuer = "BugboxTestIssuer",
            Audience = "BugboxTestAudience",
            ExpiryMinutes = 60
        });

        _jwtTokenGenerator = new JwtTokenGenerator(jwtOptions);
        _sut = new AuthService(dbContext, _passwordHasher, _jwtTokenGenerator);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ReturnsAuthResponseAndPersistsUser()
    {
        // Arrange
        var request = new RegisterRequest("Alice Hacker", "alice@example.com", "Password123!");

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.User.Should().NotBeNull();
        result.User.Name.Should().Be("Alice Hacker");
        result.User.Email.Should().Be("alice@example.com");
        result.User.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var request1 = new RegisterRequest("Alice Hacker", "alice@example.com", "Password123!");
        await _sut.RegisterAsync(request1);

        var request2 = new RegisterRequest("Alice Clone", "ALICE@EXAMPLE.COM", "AnotherPassword123!");

        // Act
        var act = () => _sut.RegisterAsync(request2);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*alice@example.com*already exists*");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var registerRequest = new RegisterRequest("Bob Dev", "bob@example.com", "SecurePassword123!");
        await _sut.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest("bob@example.com", "SecurePassword123!");

        // Act
        var result = await _sut.LoginAsync(loginRequest);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("bob@example.com");
        result.User.Name.Should().Be("Bob Dev");
    }

    [Fact]
    public async Task LoginAsync_WithIncorrectPassword_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var registerRequest = new RegisterRequest("Charlie Tester", "charlie@example.com", "CorrectPassword123!");
        await _sut.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest("charlie@example.com", "WrongPassword!");

        // Act
        var act = () => _sut.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var loginRequest = new LoginRequest("unknown@example.com", "AnyPassword123!");

        // Act
        var act = () => _sut.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithExistingUser_ReturnsUserDto()
    {
        // Arrange
        var registered = await _sut.RegisterAsync(new RegisterRequest("Dana Admin", "dana@example.com", "Password123!"));

        // Act
        var result = await _sut.GetCurrentUserAsync(registered.User.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(registered.User.Id);
        result.Email.Should().Be("dana@example.com");
        result.Name.Should().Be("Dana Admin");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithNonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => _sut.GetCurrentUserAsync(nonExistentId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
