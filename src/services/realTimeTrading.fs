module RealTimeTrading
open System.Text.Json
open System
open MongoDBUtil
open System.Collections.Concurrent
open MongoDB.Driver
open MongoDB.Bson
open ProcessOrder

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
let priceStore = ConcurrentDictionary<string, ConcurrentDictionary<string, PriceQuote>>()
let fetchTradingConfig : TradingConfig option =
    let collection = database.GetCollection<BsonDocument>("TradingStrategies")
    let filter = Builders<BsonDocument>.Filter.Empty
    let document = collection.Find(filter).FirstOrDefault()
    if document <> null then
        // Assuming values are stored as floating-point numbers, not as BsonDecimal128
        let minSpread = document.["MinimalPriceSpread"].AsDouble |> decimal
        let minProfit = document.["MinimalTransactionProfit"].AsDouble |> decimal
        let maxTransactionValue = document.["MaximalTransactionValue"].AsDouble |> decimal
        let maxTradingValue = document.["MaximalTradingValue"].AsDouble |> decimal
        Some { 
            MinSpread = minSpread; 
            MinProfit = minProfit; 
            MaxTransactionAmount = maxTransactionValue; 
            MaxTradingValue = maxTradingValue 
        }
    else
        None



let updatePriceStore (exchange: string) (pair: string) (quote: PriceQuote) =
    let exchangePrices = priceStore.GetOrAdd(exchange, fun _ -> ConcurrentDictionary<string, PriceQuote>())
    exchangePrices.AddOrUpdate(pair, quote, fun _ _ -> quote) |> ignore

let isProfitable (bidPrice: PriceQuote) (askPrice: PriceQuote) =
    match fetchTradingConfig with
    | Some config ->
        printfn "Configuration loaded: %A" config
        // Now you can use 'config' wherever needed in your application
        let spread = bidPrice.Price - askPrice.Price
        let profit = (spread * (min bidPrice.Size askPrice.Size))
        let transactionAmount = (bidPrice.Price * bidPrice.Size + askPrice.Price * askPrice.Size)
        printfn "spread: %M. profit: %M. transactionAmount: %M. tradingValue: %M" spread profit transactionAmount tradingValue
        let b1 = (spread > config.MinSpread)
        let b2 = (profit > config.MinProfit)
        let b3 = (transactionAmount < config.MaxTransactionAmount)
        let b4 = ((transactionAmount + tradingValue) < config.MaxTradingValue)
        printfn "b1: %b. b2: %b. b3: %b. b4: %b." b1 b2 b3 b4
        b1 && b2 && b3 && b4
    | None ->
        false 

let getExchangeNameById (id: string) =
    match id with
    | "1" -> Some "Coinbase"
    | "2" -> Some "Bitfinex"
    | "6" -> Some "Bitstamp"
    | "23" -> Some "Kraken"
    | _ -> None

let executeArbitrageTrade (exchange1: string) (exchange2: string) (pair: string) (bidQuote: PriceQuote) (askQuote: PriceQuote) =
    let transactionAmount = (bidQuote.Price * bidQuote.Size + askQuote.Price * askQuote.Size)
    let profit = (bidQuote.Price - askQuote.Price) * (min bidQuote.Size askQuote.Size)
    tradingValue <- tradingValue + transactionAmount

    printfn "Arbitrage Opportunity: %A. Profit: %M on pair %s" bidQuote.Timestamp profit pair
    printfn "bidQuote: %A. askQuote: %A" bidQuote.Timestamp askQuote.Timestamp

    match isProfitable bidQuote askQuote with
    | true ->
        match getExchangeNameById exchange2, getExchangeNameById exchange1 with
        | Some buyExchangeName, Some sellExchangeName ->
            let buyOrder = {
                Currency = pair;
                Price = float askQuote.Price;
                OrderType = "Buy";
                Quantity = float (min bidQuote.Size askQuote.Size);
                Exchange = buyExchangeName;
            }
            let sellOrder = {
                Currency = pair;
                Price = float bidQuote.Price;
                OrderType = "Sell";
                Quantity = float (min bidQuote.Size askQuote.Size);
                Exchange = sellExchangeName;
            }
            let orders = [buyOrder; sellOrder]
            printfn "\nExecuting orders: %A\n" orders
            let invokeProcessing = {
                Orders = orders;
                UserEmail = "ashishkj@andrew.cmu.edu"; 
            }
            let tradingParameters = {
                maximalTransactionValue = decimal tradingConfig.MaxTransactionAmount
            }
            workflowProcessOrders invokeProcessing tradingParameters
        | _ ->
            printfn "Unsupported exchange identifier provided for either exchange1 (%s) or exchange2 (%s)." exchange1 exchange2
    | false ->
        printfn "No profitable arbitrage found or trade does not meet criteria."



let checkForArbitrage (pair: string) =
    let exchanges = priceStore.Keys |> Seq.toList
    for exchange1 in exchanges do
        for exchange2 in exchanges do
            if exchange1 <> exchange2 then
                let bidPrice = priceStore.[exchange1].TryGetValue(pair)
                let askPrice = priceStore.[exchange2].TryGetValue(pair)
                match bidPrice, askPrice with
                | (true, bidQuote), (true, askQuote) ->
                    let isProfitableBool = isProfitable bidQuote askQuote
                    printfn "isProfitable: %b. bid: %i. ask: %i. bid_bool: %b. ask_bool: %b." isProfitableBool bidQuote.Conditions[0] askQuote.Conditions[0] (bidQuote.Conditions[0] = 1) (askQuote.Conditions[0]=2)
                    if bidQuote.Conditions[0] = 1 && askQuote.Conditions[0] = 2 && isProfitableBool then
                        printf "here"
                        executeArbitrageTrade exchange1 exchange2 pair bidQuote askQuote
                 
                        
                | _ -> ()


let processQuote (quote: PriceQuote) =
    let exchange = quote.ExchangeType.ToString()
    updatePriceStore exchange quote.Pair quote
    checkForArbitrage quote.Pair



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