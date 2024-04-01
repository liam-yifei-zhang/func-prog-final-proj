// Todo:
// 1. CalculatePnL needs to be implemented, but should be put in the core folder.
// 2. Replace calculatePnL with workflowPnLCalculation, but verify the code correctness. It's auto generated.
// 3. Add error handling

module Services.PnLCalculation

open Core.Domain



let retrieveCompletedArbitrageTransactionsFromDatabase () =
    let transactions = [
        { CurrencyPair = ("BTC", "USD"); BuyPrice = 50000m; SellPrice = 51000m; TransactionDate = DateTime(2021, 12, 1) }
        { CurrencyPair = ("ETH", "USD"); BuyPrice = 4000m; SellPrice = 4200m; TransactionDate = DateTime(2021, 12, 2) }
        { CurrencyPair = ("LTC", "USD"); BuyPrice = 200m; SellPrice = 210m; TransactionDate = DateTime(2021, 12, 3) }
    ]
    transactions

let workflowPnLCalculation (input: OrdersProcessed) =
    input
    |> retrieveCompletedArbitrageTransactionsFromDatabase
    |> List.fold (fun acc transaction ->
        match transaction with
        | "buy" -> acc + (calculateProfitLoss transaction)
        | "sell" -> acc + (calculateProfitLoss transaction)
        | _ -> acc
    ) 0m
    |> fun accumulatedPnL ->
        match getUserAlertThreshold() with
        | Some threshold when accumulatedPnL > threshold ->
            notifyUserViaEmail accumulatedPnL
            match getUserAutoStopSetting() with
            | true -> stopTrading ()
            | false -> AlertThresholdUpdated ThresholdReset
        | _ -> PnLReportGenerated (CurrentPnLCalculated accumulatedPnL)

// Events
type PnLReportGenerated = CurrentPnLCalculated

// Domain type
