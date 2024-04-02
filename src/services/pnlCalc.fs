module Services.PnLCalculation
open core.calcPnL
open Core.Domain  // Assuming Core.Domain contains CurrencyPair and other domain-specific types

// Reusing the existing Transaction, OrdersProcessed, and CurrentPnLCalculated definitions from your code

// Assuming that the helper functions and domain events are defined as in your code

// Converting Transactions to TransactionRecords for P&L calculation
let transactionToRecords (transaction: Transaction) : TransactionRecord list =
    [
        { TransactionType = Buy; Quantity = 1m; Price = transaction.BuyPrice; CurrencyPair = transaction.CurrencyPair; TransactionDate = transaction.TransactionDate }
        { TransactionType = Sell; Quantity = 1m; Price = transaction.SellPrice; CurrencyPair = transaction.CurrencyPair; TransactionDate = transaction.TransactionDate }
    ]

let workflowPnLCalculation (input: OrdersProcessed) =
    let transactionRecords = input |> List.collect transactionToRecords
    let accumulatedPnL = calculatePnL transactionRecords

    match getUserAlertThreshold() with
    | Some threshold when accumulatedPnL > threshold ->
        notifyUserViaEmail accumulatedPnL
        match getUserAutoStopSetting() with
        | true -> stopTrading ()
        | false -> PnLReportGenerated (CurrentPnLCalculated accumulatedPnL)
    | _ -> PnLReportGenerated (CurrentPnLCalculated accumulatedPnL)


