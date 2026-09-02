using System.Text.Json;
using NodaMoney.Context;
using NodaMoney.Serialization;

namespace NodaMoney.Tests.Serialization.SystemTextJsonSerializationSpec;

public class DeserializeMoney
{
    [Theory]
    [ClassData(typeof(ValidJsonV1TestData))]
    public void WhenDeserializingV1_ThenThisShouldSucceed(string json, Money expected)
    {
        var clone = JsonSerializer.Deserialize<Money>(json);

        clone.Should().Be(expected);
    }

    [Theory]
    [ClassData(typeof(ValidJsonV2TestData))]
    public void WhenDeserializingV2_ThenThisShouldSucceed(string json, Money expected)
    {
        var clone = JsonSerializer.Deserialize<Money>(json);

        clone.Should().Be(expected);
    }

    [Theory]
    [ClassData(typeof(InvalidJsonV1TestData))]
    public void WhenDeserializingWithInvalidJSONV1_ThenThisShouldFail(string json)
    {
        Action action = () => JsonSerializer.Deserialize<Money>(json);

        action.Should().Throw<JsonException>().WithMessage("*property*");
    }

    [Theory]
    [ClassData(typeof(InvalidJsonV2TestData))]
    public void WhenDeserializingWithInvalidJSONV2_ThenThisShouldFail(string json)
    {
        Action action = () => JsonSerializer.Deserialize<Money>(json);

        action.Should().Throw<JsonException>().WithMessage("*invalid*");
    }

    [Theory]
    [ClassData(typeof(NestedJsonV1TestData))]
    public void WhenDeserializingWithNestedV1_ThenThisShouldSucceed(string json, Order expected)
    {
        JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        var clone = JsonSerializer.Deserialize<Order>(json, options);

        clone.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [ClassData(typeof(NestedJsonV2TestData))]
    public void WhenDeserializingWithNestedV2_ThenThisShouldSucceed(string json, Order expected)
    {
        JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        var clone = JsonSerializer.Deserialize<Order>(json, options);

        clone.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void WhenNullableOrderWithTotalIsNull_ThenThisShouldSucceed()
    {
        // Arrange
        //var order = new NullableOrder { Id = 123, Name = "Foo", Total = null };
        var order = new NullableOrder { Id = 123, Name = "Foo" };
        string json = $$"""{"Id":123,"Total":null,"Name":"Foo"}""";

        // Act
        JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        var deserialized = JsonSerializer.Deserialize<NullableOrder>(json, options);

        // Assert
        deserialized.Should().BeEquivalentTo(order);
        deserialized.Total.Should().BeNull();
    }

    [Fact]
    public void WhenDeserializingV2WithMoreDecimals_ThenAmountShouldBeRoundedByCurrentContext()
    {
        // Arrange
        string json = "\"EUR 123.456\"";

        // Act
        var clone = JsonSerializer.Deserialize<Money>(json);

        // Assert
        clone.Amount.Should().Be(123.46m);
        clone.Context.Should().Be(MoneyContext.CurrentContext);
    }

    [Fact]
    public void WhenAddingDeserializedToConstructedMoney_ThenThisShouldNotThrow()
    {
        // Arrange
        var constructed = new Money(10, "EUR");
        var deserialized = JsonSerializer.Deserialize<Money>("\"EUR 10.00\"");

        // Act
        Action action = () => _ = constructed + deserialized;

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void WhenMultiplyingDeserializedMoney_ThenResultShouldMatchConstructedMoney()
    {
        // Arrange
        var constructed = new Money(10, "EUR");
        var deserialized = JsonSerializer.Deserialize<Money>("\"EUR 10.00\"");

        // Act
        var result = deserialized * 0.1234m;

        // Assert
        result.Should().Be(constructed * 0.1234m);
    }

    [Fact]
    public void WhenConverterHasNoRoundingContext_ThenAmountShouldKeepAllDecimals()
    {
        // Arrange
        var noRounding = MoneyContext.Create(opt => opt.RoundingStrategy = new NoRounding());
        JsonSerializerOptions options = new() { Converters = { new MoneyJsonConverter(noRounding) } };

        // Act
        var clone = JsonSerializer.Deserialize<Money>("\"EUR 123.456\"", options);

        // Assert
        clone.Amount.Should().Be(123.456m);
        clone.Context.Should().Be(noRounding);
    }

    [Fact]
    public void WhenConverterHasNoRoundingContext_ThenSerializingShouldBeUnchanged()
    {
        // Arrange
        var noRounding = MoneyContext.Create(opt => opt.RoundingStrategy = new NoRounding());
        JsonSerializerOptions options = new() { Converters = { new MoneyJsonConverter(noRounding) } };
        var money = new Money(765.4321m, CurrencyInfo.FromCode("EUR"));

        // Act
        var json = JsonSerializer.Serialize(money, options);

        // Assert
        json.Should().Be(JsonSerializer.Serialize(money));
    }

    [Fact]
    public void WhenConverterHasNoContext_ThenAmountShouldBeRoundedByCurrentContext()
    {
        // Arrange
        JsonSerializerOptions options = new() { Converters = { new MoneyJsonConverter() } };

        // Act
        var clone = JsonSerializer.Deserialize<Money>("\"EUR 123.456\"", options);

        // Assert
        clone.Amount.Should().Be(123.46m);
        clone.Context.Should().Be(MoneyContext.CurrentContext);
    }

    [Fact]
    public void WhenDeserializingInNoRoundingScope_ThenAmountShouldKeepAllDecimals()
    {
        // Arrange
        var noRounding = MoneyContext.Create(opt => opt.RoundingStrategy = new NoRounding());

        // Act
        using var scope = MoneyContext.CreateScope(noRounding);
        var clone = JsonSerializer.Deserialize<Money>("\"EUR 123.456\"");

        // Assert
        clone.Amount.Should().Be(123.456m);
        clone.Context.Should().Be(noRounding);
    }

    [Fact]
    public void WhenDeserializingInAwayFromZeroScope_ThenAmountShouldBeRoundedByThatScope()
    {
        // Arrange
        var awayFromZero = MoneyContext.Create(opt => opt.RoundingStrategy = new StandardRounding(MidpointRounding.AwayFromZero));

        // Act
        using var scope = MoneyContext.CreateScope(awayFromZero);
        var clone = JsonSerializer.Deserialize<Money>("\"EUR 123.455\"");

        // Assert
        clone.Amount.Should().Be(123.46m);
        clone.Context.Should().Be(awayFromZero);
    }
}
