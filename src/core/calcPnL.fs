open System

type HistoricalTransaction = {
    CurrencyPair: CurrencyPair
    BuyPrice: decimal
    SellPrice: decimal
    TransactionDate: DateTime
}

let calculateAllHistoricalPnL (transactions: HistoricalTransaction list) =
    transactions
    |> List.map (fun t -> t.SellPrice - t.BuyPrice)
    |> List.sum

let calculatePnLWithTimeRange (transactions: HistoricalTransaction list) (startDate: DateTime) (endDate: DateTime) =
    transactions
    |> List.filter (fun t -> t.TransactionDate >= startDate && t.TransactionDate <= endDate)
    |> List.map (fun t -> t.SellPrice - t.BuyPrice)
    |> List.sum
