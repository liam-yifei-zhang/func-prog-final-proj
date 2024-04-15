module Services.PnLCalculation
open core.calcPnL
open Core.Domain  // Assuming Core.Domain contains CurrencyPair and other domain-specific types
open infra.shareStates.fs

// Reusing the existing Transaction, OrdersProcessed, and CurrentPnLCalculated definitions from your code

// Assuming that the helper functions and domain events are defined as in your code

// Converting Transactions to TransactionRecords for P&L calculation
let transactionToRecords (transaction: Transaction) =
    let records = [
        { TransactionType = Buy; Quantity = 1m; Price = transaction.BuyPrice; CurrencyPair = transaction.CurrencyPair; TransactionDate = transaction.TransactionDate }
        { TransactionType = Sell; Quantity = 1m; Price = transaction.SellPrice; CurrencyPair = transaction.CurrencyPair; TransactionDate = transaction.TransactionDate }
    ]
    transactionRecordsAgent.Post(AddRecords records)


let workflowPnLCalculation (input: OrdersProcessed) =
    async {
        let! transactionRecords = getTransactionRecordsAsync ()
        let accumulatedPnL = calculatePnL transactionRecords

        match getUserAlertThreshold() with
        | Some threshold when accumulatedPnL > threshold ->
            let messageBody = sprintf "Attention: Your trading P&L has exceeded the threshold. Current P&L: %M" accumulatedPnL
            notifyUserViaEmail getUserEmail messageBody
            match getUserAutoStopSetting() with
            | true -> stopTrading ()
            | false -> return PnLReportGenerated (CurrentPnLCalculated accumulatedPnL)
        | _ -> return PnLReportGenerated (CurrentPnLCalculated accumulatedPnL)
    } |> Async.RunSynchronously

