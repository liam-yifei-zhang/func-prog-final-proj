module RealTimeTrading
open System.Text.Json
open System
open MongoDBUtil

open System.Collections.Concurrent

type PriceQuote = {
    Event: string
    Pair: string
    Price: decimal
    Timestamp: int64
    Size: decimal
    Conditions: int list
    Id: string
    ExchangeType: int
    ResponseTime: int64
}

type TradingConfig = {
    MinSpread: decimal
    MinProfit: decimal
    MaxTransactionAmount: decimal
    MaxTradingValue: decimal
}

let tradingConfig = {
    MinSpread = 0.05M
    MinProfit = 5.00M
    MaxTransactionAmount = 2000.00M
    MaxTradingValue = 5000.00M
}

let mutable tradingValue = 0M
let priceStore = ConcurrentDictionary<string, (decimal * decimal)>()

let processQuote (quote: PriceQuote) =
    let currencyPairCollectionName = "currencyPairs"
    let documents = fetchAllDocuments(currencyPairCollectionName)
    for doc in documents do
        printfn "%A" doc

    let updatePrices (bidAsk: (decimal * decimal) option) =
        match bidAsk with
        | Some (bid, ask) ->
            if quote.Conditions[0] = 1 then // Bid
                let newBid = min bid quote.Price
                priceStore.AddOrUpdate(quote.Pair, (newBid, ask), fun _ _ -> (newBid, ask)) |> ignore
            elif quote.Conditions[0] = 2 then // Ask
                let newAsk = max ask quote.Price
                priceStore.AddOrUpdate(quote.Pair, (bid, newAsk), fun _ _ -> (bid, newAsk)) |> ignore
        | None ->
            if quote.Conditions[0] = 1 then
                priceStore.AddOrUpdate(quote.Pair, (quote.Price, Decimal.Zero), fun _ _ -> (quote.Price, Decimal.Zero)) |> ignore
            elif quote.Conditions[0] = 2 then
                priceStore.AddOrUpdate(quote.Pair, (Decimal.MaxValue, quote.Price), fun _ _ -> (Decimal.Zero, quote.Price)) |> ignore

    let (exists, currentPrices) = priceStore.TryGetValue(quote.Pair)
    updatePrices (if exists then Some currentPrices else None)

    printfn "containsKey: %b" (priceStore.ContainsKey(quote.Pair))
    if priceStore.ContainsKey(quote.Pair) then
        let (bid, ask) = priceStore.[quote.Pair]
        
        printfn "bid: %M. ask: %M" bid ask
        // Check if both bid and ask prices are available
        if bid <> Decimal.MaxValue && ask <> Decimal.Zero then
            let spread = ask - bid
            printfn "spread: %M. MinSpread: %M" spread tradingConfig.MinSpread
            if spread >= tradingConfig.MinSpread then
                let transactionAmount = min tradingConfig.MaxTransactionAmount (tradingConfig.MaxTradingValue - tradingValue)
                let possibleProfit = (spread - tradingConfig.MinSpread) * transactionAmount
                printfn "possibleProfit: %M. MinProfit: %M" possibleProfit tradingConfig.MinProfit
                if possibleProfit >= tradingConfig.MinProfit then
                    tradingValue <- tradingValue + transactionAmount
                    printfn "Arbitrage Opportunity: %A. Possible profit: %M on pair %s" quote.Timestamp possibleProfit quote.Pair
                    
                    let bidQuote = {
                        Event = quote.Event
                        Pair = quote.Pair
                        Price = bid
                        Timestamp = quote.Timestamp
                        Size = quote.Size
                        Conditions = quote.Conditions
                        Id = quote.Id
                        ExchangeType = 1
                        ResponseTime = quote.ResponseTime
                    }
                    
                    let askQuote = {
                        Event = quote.Event
                        Pair = quote.Pair
                        Price = ask
                        Timestamp = quote.Timestamp
                        Size = quote.Size
                        Conditions = quote.Conditions
                        Id = quote.Id
                        ExchangeType = 2
                        ResponseTime = quote.ResponseTime
                    }

                    printfn "bidQuote: %s. askQuote: %s" bidQuote.Pair askQuote.Pair
                    Some (bidQuote, askQuote)
                else
                    None
            else
                None
        else
            None
    else
        None

let parseQuoteFromMessage (message: string) =
    try
        let jsonOptions = JsonSerializerOptions()
        jsonOptions.PropertyNameCaseInsensitive <- true
        
        let quoteJson = JsonSerializer.Deserialize<JsonElement>(message, jsonOptions)
        
        let event = quoteJson.[0].GetProperty("ev").GetString()
        let pair = quoteJson.[0].GetProperty("pair").GetString()
        let price = quoteJson.[0].GetProperty("p").GetDecimal()
        let timestamp = quoteJson.[0].GetProperty("t").GetInt64()
        let size = quoteJson.[0].GetProperty("s").GetDecimal()
        let conditions = quoteJson.[0].GetProperty("c").EnumerateArray() |> Seq.map (fun (x: JsonElement) -> x.GetInt32()) |> Seq.toList
        let id = quoteJson.[0].GetProperty("i").GetString()
        let exchangeType = quoteJson.[0].GetProperty("x").GetInt32()
        let responseTime = quoteJson.[0].GetProperty("r").GetInt64()
        
        let quote = {
            Event = event
            Pair = pair
            Price = price
            Timestamp = timestamp
            Size = size
            Conditions = conditions
            Id = id
            ExchangeType = exchangeType
            ResponseTime = responseTime
        }
        
        Ok quote
    with
    | ex -> Error ex.Message