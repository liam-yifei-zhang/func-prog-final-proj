open core.calcPnL

module HistoricalPnLCalculator =
    let fetchHistoricalTransactionsFromDb (startDate: DateTime, endDate: DateTime) =
        // Placeholder: Implement database retrieval logic here

   let calculateHistoricalPnL (startDate: DateTime, endDate: DateTime, event: UserRequestsPnLCalculation) : decimal =
    match event with
        | UserInvokesPnLCalculation (startDate, endDate) ->
            let transactions = fetchTransactionsFromDb startDate endDate
            let pnl = calculatePnL transactions
            pnl
        | _ -> printfn "Unsupported event"
