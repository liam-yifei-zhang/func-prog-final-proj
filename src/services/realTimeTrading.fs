module Services.RealTimeTrading
open Core.Domain

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

type UpdateResult =
    | OpportunityFound of TradingOpportunity
    | NoOpportunity of DomainError

let evaluateMarketData (data: MarketData) (parameters: TradingParameter): UpdateResult =
    match data.bidPrice > 0m, data.askPrice > 0m with
    | true, false ->
        let quantity = min (parameters.maximalTransactionValue / data.bidPrice) (parameters.maximalTradingValue / data.bidPrice)
        match quantity * data.bidPrice <= parameters.maximalTradingValue with
        | true -> OpportunityFound { currencyPair = data.currencyPair; buyPrice = data.bidPrice; sellPrice = 0m; buyExchange = data.exchangeName; sellExchange = ""; quantity = quantity }
        | false -> NoOpportunity ExceedsMaximalTradingValue
    | false, true | _, _ -> NoOpportunity InvalidMarketData

let updateOpportunity (data: MarketData) (opp: TradingOpportunity): UpdateResult =
    match data.exchangeName <> opp.buyExchange && data.askPrice > opp.buyPrice with
    | true -> 
        let profitPerUnit = data.askPrice - opp.buyPrice
        let totalProfit = profitPerUnit * opp.quantity
        match totalProfit > parameters.minimalProfit with
        | true -> OpportunityFound { opp with sellPrice = data.askPrice; sellExchange = data.exchangeName }
        | false -> NoOpportunity BelowMinimalProfit
    | false -> NoOpportunity NoOpportunityFound


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
        | OpportunityFound opp -> (Some opp, accOrders @ generateTradingOrders opp)
        | NoOpportunity _ -> (accOpportunity, accOrders)
    ) (None, []) dataFeed