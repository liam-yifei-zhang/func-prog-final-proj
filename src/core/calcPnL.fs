open System

type HistoricalTransaction = {
    CurrencyPair: CurrencyPair
    BuyPrice: decimal
    SellPrice: decimal
    TransactionDate: DateTime
}

let calculatePnL (transactions: TransactionRecord list) : decimal =
    transactions 
    |> List.fold (fun acc t ->
        match t.TransactionType with
        | Buy -> acc - (t.Quantity * t.Price) // Subtracting cost of buys from accumulated value
        | Sell -> acc + (t.Quantity * t.Price) // Adding revenue from sells to accumulated value
    ) 0m