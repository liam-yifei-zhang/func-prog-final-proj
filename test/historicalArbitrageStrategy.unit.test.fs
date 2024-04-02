module ArbitrageStrategiesTests

open NUnit.Framework
open Core.Strategy
open Core.Domain
open System

[<Test>]
let ``findArbitrageOpportunities correctly identifies opportunities when bid-ask difference is more than 0.01`` () =

    let quotesByPair = [
        ("CHZ-USD", [
            { Exchange = "ExchangeA"; CurrencyPair = "CHZ-USD"; Bid = 1.05f; Ask = 1.07f; Timestamp = DateTime(2023, 4, 1) }; 
            { Exchange = "ExchangeB"; CurrencyPair = "CHZ-USD"; Bid = 1.03f; Ask = 1.03f; Timestamp = DateTime(2023, 4, 1) }; 
            { Exchange = "ExchangeC"; CurrencyPair = "CHZ-USD"; Bid = 1.02f; Ask = 1.06f; Timestamp = DateTime(2023, 4, 1) }; 
        ])
    ]

    let opportunities = quotesByPair |> List.collect (fun (_, qs) -> findArbitrageOpportunities (qs.Head.CurrencyPair, qs))

    Assert.AreEqual(1, opportunities.Length, "Should identify exactly one arbitrage opportunity.")
    let opportunity = opportunities.Head
    Assert.AreEqual("ExchangeB", opportunity.BuyExchange, "Buy Exchange should be ExchangeB with the lower ask price.")
    Assert.AreEqual("ExchangeA", opportunity.SellExchange, "Sell Exchange should be ExchangeA with the highest bid price.")
    Assert.IsTrue(opportunity.SellPrice - opportunity.BuyPrice > 0.01f, "The difference between sell and buy price should be more than $0.01.")

[<Test>]
let ``summarizeOpportunities should correctly summarize the number of arbitrage opportunities by currency pair`` () =
    
    let opportunities = [
        { BuyExchange = "ExchangeA"; SellExchange = "ExchangeB"; CurrencyPair = "CHZ-USD"; BuyPrice = 0.99f; SellPrice = 1.01f; Timestamp = DateTime.Now }
        { BuyExchange = "ExchangeC"; SellExchange = "ExchangeD"; CurrencyPair = "CHZ-USD"; BuyPrice = 1.01f; SellPrice = 1.03f; Timestamp = DateTime.Now }
        { BuyExchange = "ExchangeE"; SellExchange = "ExchangeF"; CurrencyPair = "KNC-USD"; BuyPrice = 0.72f; SellPrice = 0.74f; Timestamp = DateTime.Now }
    ]

    let summary = summarizeOpportunities opportunities

    Assert.Contains("CHZ-USD; 2", summary, "Summary should report exactly 2 opportunities for CHZ-USD.")
    Assert.Contains("KNC-USD; 1", summary, "Summary should report exactly 1 opportunity for KNC-USD.")
    Assert.AreEqual(2, summary.Length, "Summary should include exactly 2 currency pairs.")
