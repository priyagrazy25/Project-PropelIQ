using SharedKernel.Domain;

namespace UnitTests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Success_ShouldReturnIsSuccessTrue()
    {
        var result = Result<string>.Success("value");

        Assert.True(result.IsSuccess);
        Assert.Equal("value", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ShouldReturnIsSuccessFalse()
    {
        var result = Result<string>.Failure("Something went wrong");

        Assert.False(result.IsSuccess);
        Assert.Equal("Something went wrong", result.Error);
        Assert.Null(result.Value);
    }

    [Fact]
    public void GenericSuccess_ShouldReturnValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GenericFailure_ShouldReturnDefaultValue()
    {
        var result = Result<int>.Failure("Error");

        Assert.False(result.IsSuccess);
        Assert.Equal(default, result.Value);
    }
}
