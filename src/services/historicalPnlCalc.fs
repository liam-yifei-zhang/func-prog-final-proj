let transactions = [
    { TransactionType = Buy; Quantity = 100m; Price = 10m; TransactionDateTime = DateTime(2024, 4, 1, 9, 0, 0, 500) }
    { TransactionType = Buy; Quantity = 50m; Price = 12m; TransactionDateTime = DateTime(2024, 4, 1, 10, 30, 0, 250) }
    { TransactionType = Sell; Quantity = 75m; Price = 15m; TransactionDateTime = DateTime(2024, 4, 1, 11, 0, 0, 125) }
    { TransactionType = Sell; Quantity = 50m; Price = 20m; TransactionDateTime = DateTime(2024, 4, 2, 15, 45, 0, 625) }
]

module HistoricalPnLCalculator =
    open System

    // Assuming the existence of a function to retrieve historical transactions from the database
    let fetchHistoricalTransactionsFromDb (startDate: DateTime, endDate: DateTime) =
        // Placeholder: Implement database retrieval logic here

    let calculatePnL (transactions: (decimal * DateTime)[]) =
        let pnl = transactions |> Array.fold (fun acc (amount, _) -> acc + amount) 0m
        pnl

    let emitHistoricalPnLReportGeneratedEvent (pnl: decimal) =
        // Placeholder: Implement event emission logic here
        printfn "Historical P&L Calculated: %A" pnl

    let calculateOnDemandPnL (startDate: DateTime, endDate: DateTime) =
        async {
            let! transactions = fetchHistoricalTransactionsFromDb startDate endDate
            let pnl = calculatePnL transactions
            emitHistoricalPnLReportGeneratedEvent pnl
        }

    let workflowOnDemandHistoricPnLCalculation (event: UserRequestsPnLCalculation) =
        match event with
        | UserInvokesPnLCalculation (startDate, endDate) ->
            calculateOnDemandPnL startDate endDate
            |> Async.RunSynchronously
        | _ -> printfn "Unsupported event"
