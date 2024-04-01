open System

// Todo:
// 1. Move the type to domain.fs
// 2. verify that the functions here are correct. They are auto-generated.

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