module PnLUnitTest

open NUnit.Framework
open Services.PnLCalculation
open System

// Test for calculateProfitLoss function
[<Test>]
[<TestCase(50000m, 51000m, 1000m)>] // Case where a profit is expected
[<TestCase(4200m, 4000m, -200m)>] // Case where a loss is expected
[<TestCase(1000m, 1000m, 0m)>] // Case where no profit or loss is expected
let ``CalculateProfitLoss should correctly calculate profit or loss for a transaction`` (buyPrice, sellPrice, expectedPnL) =
    let transaction = { CurrencyPair = ("BTC", "USD"); BuyPrice = buyPrice; SellPrice = sellPrice; TransactionDate = DateTime.Now }
    let actualPnL = calculateProfitLoss transaction

    Assert.AreEqual(expectedPnL, actualPnL, 0.001m, "The calculated PnL does not match the expected value.")

    Assert.IsTrue((sellPrice > buyPrice && actualPnL > 0m) || (sellPrice < buyPrice && actualPnL < 0m) || (sellPrice = buyPrice && actualPnL = 0m), "The PnL sign does not match the transaction outcome.")
