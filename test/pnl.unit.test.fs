module PnLUnitTest

open NUnit.Framework
open Core.Domain
open System

[<Test>]
let ``calculatePnL should accurately calculate net profit or loss across complex transaction sets`` () =

    let transactions = [
        { TransactionType = Buy; Quantity = 5m; Price = 100m; CurrencyPair = "ETH-USD"; TransactionDate = DateTime(2023, 1, 1) };  
        { TransactionType = Buy; Quantity = 10m; Price = 90m; CurrencyPair = "ETH-USD"; TransactionDate = DateTime(2023, 1, 2) };  
        { TransactionType = Sell; Quantity = 8m; Price = 95m; CurrencyPair = "ETH-USD"; TransactionDate = DateTime(2023, 1, 3) };  
        { TransactionType = Sell; Quantity = 5m; Price = 110m; CurrencyPair = "ETH-USD"; TransactionDate = DateTime(2023, 1, 4) }; 
        { TransactionType = Sell; Quantity = 2m; Price = 105m; CurrencyPair = "ETH-USD"; TransactionDate = DateTime(2023, 1, 5) }; 
    ]

    let expectedNetPnL = 880m 
    let actualNetPnL = calculatePnL transactions

    Assert.AreEqual(expectedNetPnL, actualNetPnL, "The calculated net PnL from complex transaction sets does not match the expected value.")
    Assert.IsTrue(actualNetPnL >= 0m, "Net PnL should be non-negative")
