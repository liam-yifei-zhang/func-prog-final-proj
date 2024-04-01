module pnlUnitTest

open NUnit.Framework
open Services.PnLCalculation
open Core.Domain
open System

[<Test>]
[<TestCase("2024-06-15", "2024-06-18", 8.0)>] 
let ``Profit And Loss Calculation Workflow - Integration Test with Expanded Dummy Data`` (startDateStr, endDateStr, expectedTotalPnL) =
    let startDate = DateTime.Parse(startDateStr)
    let endDate = DateTime.Parse(endDateStr)

    let loadTransactions _ _ =
        [
            // Fully matched buy and sell orders
            { BuyPrice = 0.8m; BuySize = 30m; SellPrice = 0.85m; SellSize = 30m; TransactionDate = DateTime(2024, 6, 15); ExchangeBuy = 1; ExchangeSell = 2 },
            // Partially matched sell order
            { BuyPrice = 1.2m; BuySize = 30m; SellPrice = 1.25m; SellSize = 25m; TransactionDate = DateTime(2024, 6, 16); ExchangeBuy = 1; ExchangeSell = 2 },
            // Assuming additional order for the remaining buy units gets matched
            { BuyPrice = 1.5m; BuySize = 30m; SellPrice = 1.55m; SellSize = 30m; TransactionDate = DateTime(2024, 6, 17); ExchangeBuy = 1; ExchangeSell = 2 },
            // Larger spread, fully matched
            { BuyPrice = 0.9m; BuySize = 50m; SellPrice = 1.0m; SellSize = 50m; TransactionDate = DateTime(2024, 6, 18); ExchangeBuy = 1; ExchangeSell = 2 }
        ]
        |> List.filter (fun t -> t.TransactionDate >= startDate && t.TransactionDate <= endDate)
        |> List.map (fun t -> { Pair = ""; BidPrice = t.BuyPrice; BidSize = t.BuySize; AskPrice = t.SellPrice; AskSize = t.SellSize; Time = t.TransactionDate; Exchange = t.ExchangeBuy }) 

    let testResult = profitAndLossCalculation 
                         loadTransactions 
                         calculatePnL 
                         transformToPnLReport 
                         persistResults 
                         startDate 
                         endDate

    Assert.AreEqual(expectedTotalPnL, testResult.Report.TotalPnL, "The P&L calculation did not match the expected result.")
    Assert.IsTrue(testResult.Persisted, "The report was expected to be persisted but was not.")
