// Todo:
// 1. Verify the code here is correct
// 2. Add error handling
// 3. Move the calculatePnL and calculateOnDemandPnL to core folder.


module HistoricalPnLCalculator =
    open System

    // Assuming the existence of a function to retrieve historical transactions from the database
    let fetchHistoricalTransactionsFromDb (startDate: DateTime, endDate: DateTime) =
        // Placeholder: Implement database retrieval logic here
        async {
            // Simulated database response
            let transactions = [|(1000m, DateTime(2021, 01, 01)); (1500m, DateTime(2021, 06, 30))|] // Example values
            return transactions // Example final value
        }

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
