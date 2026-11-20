using Microsoft.Xrm.Sdk;
using Xunit;
using XrmMockup.MetadataGenerator.Core.Readers;
using XrmMockup.MetadataGenerator.Tool.Context;
using XrmMockup.MetadataGenerator.Tool.Tests.Fixtures;

namespace XrmMockup.MetadataGenerator.Tool.Tests.Readers;

/// <summary>
/// Tests for CurrencyReader.
/// Tests verify the reader correctly retrieves currencies from the organization.
/// </summary>
public class CurrencyReaderTests : ReaderTestBase
{
    private readonly CurrencyReader _reader;

    public CurrencyReaderTests()
    {
        var logger = CreateLogger<CurrencyReader>();
        _reader = new CurrencyReader(ServiceProvider, logger);
    }

    [Fact]
    public async Task GetCurrenciesAsync_CreatedCurrencyIsRetrieved()
    {
        var currency = new TransactionCurrency(Guid.NewGuid())
        {
            CurrencyName = "Test Currency",
            ISOCurrencyCode = "TST",
            CurrencySymbol = "T",
            CurrencyPrecision = 2,
            ExchangeRate = 1.0m
        };
        Service.Create(currency);

        var result = await _reader.GetCurrenciesAsync();

        var currencies = result.ConvertAll(e => e.ToEntity<TransactionCurrency>());
        var testCurrency = currencies.FirstOrDefault(c => c.ISOCurrencyCode == "TST");
        Assert.NotNull(testCurrency);
        Assert.Equal("Test Currency", testCurrency.CurrencyName);
    }
}
