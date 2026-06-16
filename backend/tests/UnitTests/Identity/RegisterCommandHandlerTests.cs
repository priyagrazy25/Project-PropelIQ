using Identity.Application.Abstractions;
using Identity.Application.Commands.Register;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UnitTests.Identity;

public sealed class RegisterCommandHandlerTests
{
    private static RegisterCommand ValidCommand() => new(
        FirstName: "Jane",
        LastName: "Smith",
        Email: "jane.smith@example.com",
        Phone: "(555) 999-0000",
        DateOfBirth: "1985-06-20",
        Gender: "Female",
        Password: "Str0ng!Pass");

    private static (RegisterCommandHandler handler, Mock<IIdentityDbContext> dbMock, Mock<IPasswordHasher> hasherMock)
        CreateHandler(List<User>? existingUsers = null)
    {
        existingUsers ??= [];

        // Set up an in-memory DbSet<User>
        var usersQueryable = existingUsers.AsQueryable();
        var mockUserSet = new Mock<DbSet<User>>();
        mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<User>(usersQueryable.Provider));
        mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(usersQueryable.Expression);
        mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(usersQueryable.ElementType);
        mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(usersQueryable.GetEnumerator());
        mockUserSet.As<IAsyncEnumerable<User>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<User>(usersQueryable.GetEnumerator()));

        var mockPatientSet = new Mock<DbSet<Patient>>();

        var dbMock = new Mock<IIdentityDbContext>();
        dbMock.Setup(d => d.Users).Returns(mockUserSet.Object);
        dbMock.Setup(d => d.Patients).Returns(mockPatientSet.Object);
        dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed_password");

        var validator = new RegisterCommandValidator();
        var handler = new RegisterCommandHandler(dbMock.Object, hasherMock.Object, validator);

        return (handler, dbMock, hasherMock);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_ReturnsSuccess()
    {
        var (handler, dbMock, hasherMock) = CreateHandler();

        var result = await handler.HandleAsync(ValidCommand());

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value!.UserId);
        hasherMock.Verify(h => h.Hash("Str0ng!Pass"), Times.Once);
        dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ReturnsFailure()
    {
        var existingUser = new User { Email = "jane.smith@example.com" };
        var (handler, _, _) = CreateHandler([existingUser]);

        var result = await handler.HandleAsync(ValidCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("DUPLICATE_EMAIL", result.Error);
    }

    [Fact]
    public async Task HandleAsync_InvalidCommand_ReturnsValidationErrors()
    {
        var (handler, _, _) = CreateHandler();
        var invalidCommand = ValidCommand() with { Email = "", Password = "" };

        var result = await handler.HandleAsync(invalidCommand);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task HandleAsync_SetsPatientRoleAndActiveStatus()
    {
        var (handler, dbMock, _) = CreateHandler();
        User? capturedUser = null;
        dbMock.Setup(d => d.Users).Returns(() =>
        {
            var empty = new List<User>().AsQueryable();
            var mockSet = new Mock<DbSet<User>>();
            mockSet.As<IQueryable<User>>().Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<User>(empty.Provider));
            mockSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(empty.Expression);
            mockSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(empty.ElementType);
            mockSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(empty.GetEnumerator());
            mockSet.As<IAsyncEnumerable<User>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<User>(empty.GetEnumerator()));
            mockSet.Setup(m => m.Add(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u);
            return mockSet.Object;
        });

        var result = await handler.HandleAsync(ValidCommand());

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedUser);
        Assert.Equal(global::Identity.Domain.Enums.UserRole.Patient, capturedUser!.Role);
        Assert.Equal(global::Identity.Domain.Enums.UserStatus.Active, capturedUser.Status);
    }

    [Fact]
    public async Task HandleAsync_EmailIsNormalized()
    {
        var (handler, dbMock, _) = CreateHandler();
        User? capturedUser = null;
        dbMock.Setup(d => d.Users).Returns(() =>
        {
            var empty = new List<User>().AsQueryable();
            var mockSet = new Mock<DbSet<User>>();
            mockSet.As<IQueryable<User>>().Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<User>(empty.Provider));
            mockSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(empty.Expression);
            mockSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(empty.ElementType);
            mockSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(empty.GetEnumerator());
            mockSet.As<IAsyncEnumerable<User>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<User>(empty.GetEnumerator()));
            mockSet.Setup(m => m.Add(It.IsAny<User>()))
                .Callback<User>(u => capturedUser = u);
            return mockSet.Object;
        });

        var cmd = ValidCommand() with { Email = "  Jane.Smith@Example.COM  " };
        await handler.HandleAsync(cmd);

        Assert.NotNull(capturedUser);
        Assert.Equal("jane.smith@example.com", capturedUser!.Email);
    }
}

// Async query infrastructure for mocking EF Core DbSet
internal sealed class TestAsyncQueryProvider<T> : Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression) =>
        new TestAsyncEnumerable<T>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression) =>
        new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(System.Linq.Expressions.Expression expression) =>
        _inner.Execute(expression);

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression) =>
        _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression,
        CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(System.Linq.Expressions.Expression)])!
            .MakeGenericMethod(expectedResultType)
            .Invoke(_inner, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, [executionResult])!;
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());

    public T Current => _inner.Current;
}
