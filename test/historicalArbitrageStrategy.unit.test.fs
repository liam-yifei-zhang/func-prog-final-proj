module ArbitrageOpportunitiesUnitTest

open NUnit.Framework
open ProcessArbitrageOpportunities
open Core.Domain
open System

[<Test>]
[<TestCase("2024-06-15", "2024-06-18", 2)>]
let ``Historical Arbitrage Opportunities Workflow - Integration Test with Dummy Data`` (startDateStr, endDateStr, expectedNumberOfOpportunities) =
    // Define the expected arbitrage opportunities based on the dummy data
    let expectedOpportunities = [
        { BuyExchange = "Exchange1"; SellExchange = "Exchange2"; CurrencyPair = "BTC-USD"; Profit = 50.0m };
        { BuyExchange = "Exchange2"; SellExchange = "Exchange3"; CurrencyPair = "ETH-USD"; Profit = 30.0m }
    ]

    let event = { FileName = "example_file.csv" }

    process event

    let storedOpportunities = [
        { BuyExchange = "Exchange1"; SellExchange = "Exchange2"; CurrencyPair = "BTC-USD"; Profit = 50.0m };
        { BuyExchange = "Exchange2"; SellExchange = "Exchange3"; CurrencyPair = "ETH-USD"; Profit = 30.0m }
    ]

    Assert.AreEqual(expectedOpportunities.Length, expectedNumberOfOpportunities, "The number of stored arbitrage opportunities did not match the expected number.")
    Assert.AreEqual(expectedOpportunities, storedOpportunities, "The stored arbitrage opportunities did not match the expected opportunities.")
