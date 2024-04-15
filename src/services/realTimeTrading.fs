module Services.RealTimeTrading
open Core.Domain
open Infra.RealTimeTrading

type TradingParameter = {
    minimalPriceSpread: decimal
    minimalProfit: decimal
    maximalTransactionValue: decimal
    maximalTradingValue: decimal
}

type MarketData = {
    currencyPair: string
    bidPrice: decimal
    askPrice: decimal
    exchangeName: string
}

type TradingOpportunity = {
    currencyPair: string
    buyPrice: decimal
    sellPrice: decimal
    buyExchange: string
    sellExchange: string
    quantity: decimal
}

type TradingOrder = {
    currencyPair: string
    orderType: string
    quantity: decimal
    price: decimal
    exchangeName: string
}

type DomainError =
    | BelowMinimalProfit
    | ExceedsMaximalTransactionValue
    | ExceedsMaximalTradingValue
    | NoOpportunityFound
    | InvalidMarketData

type Result<'a, 'b> =
    | Success of 'a
    | Failure of 'b

type UpdateResult =
    | OpportunityFound of TradingOpportunity
    | NoOpportunity of DomainError


// Define a function to start the WebSocket client
// Sample subscripton parameters: "XT.BTC-USD"
// See https://polygon.io/docs/crypto/ws_getting-started
let start(uri: Uri, apiKey: string, subscriptionParameters: string) =
            async {
            //Establish websockets connectivity
            //Run underlying async workflow and await the result
            let! wsClient = connectToWebSocket uri
            //Authenticate with Polygon
            sendJsonMessage wsClient { action = "auth"; params = apiKey }
            //Subscribe to market data
            sendJsonMessage wsClient { action = "subscribe" ; params = subscriptionParameters }
            //Process market data
            do! receiveData wsClient
            }
          
         


let evaluateMarketData (data: MarketData) (parameters: TradingParameter): UpdateResult =
    match data.bidPrice > 0m, data.askPrice > 0m with
    | true, false ->
        let quantity = min (parameters.maximalTransactionValue / data.bidPrice) (parameters.maximalTradingValue / data.bidPrice)
        match quantity * data.bidPrice <= parameters.maximalTradingValue with
        | true -> Success { currencyPair = data.currencyPair; buyPrice = data.bidPrice; sellPrice = 0m; buyExchange = data.exchangeName; sellExchange = ""; quantity = quantity }
        | false -> Failure ExceedsMaximalTradingValue
    | false, true | _, _ -> Failure InvalidMarketData

let updateOpportunity (data: MarketData) (opp: TradingOpportunity): UpdateResult =
    match data.exchangeName <> opp.buyExchange && data.askPrice > opp.buyPrice with
    | true -> 
        let profitPerUnit = data.askPrice - opp.buyPrice
        let totalProfit = profitPerUnit * opp.quantity
        match totalProfit > parameters.minimalProfit with
        | true -> Success { opp with sellPrice = data.askPrice; sellExchange = data.exchangeName }
        | false -> Failure BelowMinimalProfit
    | false -> Failure NoOpportunityFound


let processMarketDataPoint (data: MarketData) (parameters: TradingParameter) (existingOpportunity: Option<TradingOpportunity>) : UpdateResult =
    match existingOpportunity with
    | Some opp -> updateOpportunity data opp
    | None -> evaluateMarketData data parameters

let generateTradingOrders (opp: TradingOpportunity) : List<TradingOrder> =
    match opp.sellPrice with
    | price when price > 0m ->
        [{ currencyPair = opp.currencyPair; orderType = "buy"; quantity = opp.quantity; price = opp.buyPrice; exchangeName = opp.buyExchange };
         { currencyPair = opp.currencyPair; orderType = "sell"; quantity = opp.quantity; price = opp.sellPrice; exchangeName = opp.sellExchange }]
    | _ -> []

let processRealTimeDataFeed (dataFeed: seq<MarketData>) (parameters: TradingParameter) =
    Seq.fold (fun (accOpportunity, accOrders) data ->
        let result = processMarketDataPoint data parameters accOpportunity
        match result with
        | Success opp -> Success (Some opp, accOrders @ generateTradingOrders opp)
        | Failure NoOpportunityFound -> Success (accOpportunity, accOrders)
        | Failure err -> Failure err
    ) (None, []) dataFeed


    [<EntryPoint>]
let main args =
    let uri = Uri("wss://socket.polygon.io/crypto")
    let apiKey = "phN6Q_809zxfkeZesjta_phpgQCMB2Dw"
    let subscriptionParameters = "XT.BTC-USD"
    start (uri, apiKey, subscriptionParameters) |> Async.RunSynchronously
    0   