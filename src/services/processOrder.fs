module MongoDBUtil =

    open MongoDB.Driver
    open MongoDB.Bson
    open System

    let connectionString = "your_connection_string_here"
    let databaseName = "cryptoDatabase"
    let collectionName = "transactions"

    let client = MongoClient(connectionString)
    let database = client.GetDatabase(databaseName)
    let collection = database.GetCollection<BsonDocument>(collectionName)

    let insertDocument (document: BsonDocument) =
        try
            collection.InsertOne(document)
            true
        with
        | ex: Exception ->
            printfn "Error inserting document: %s" ex.Message
            false

    let insertManyDocuments (documents: BsonDocument list) =
        try
            collection.InsertMany(documents)
            true
        with
        | ex: Exception ->
            printfn "Error inserting documents: %s" ex.Message
            false



module Services.ProcessOrder

open Core.Domain
open MongoDBUtil
open MongoDB.Bson
open System
open System.Net.Http
open BitfinexAPI
open KrakenAPI
open BitstampAPI

type Currency = string
type Price = float
type OrderType = string
type Quantity = float
type Exchange = string
type OrderID = string
type FulfillmentStatus = string

type OrderDetails = {
    Currency: Currency
    Price: Price
    OrderType: OrderType
    Quantity: Quantity
    Exchange: Exchange
}

type OrderUpdate = {
    OrderID: OrderID
    OrderDetails: OrderDetails
    FulfillmentStatus: FulfillmentStatus
    RemainingQuantity: float
}

type Event =
    | OrderFulfillmentUpdated of OrderUpdate
    | UserNotificationSent of string
    | OrderInitiated of OrderID
    | OrderProcessed of OrderUpdate

type InvokeOrderProcessing = {
    Orders: Order list
    UserEmail: string
}

    
type OrdersProcessed =
    | FullTransactionStored of OrderUpdate
    | PartialTransactionStored of OrderUpdate
    | DomainErrorRaised of string

type Order = {
    Currency: Currency
    Price: Price
    OrderType: OrderType
    Quantity: Quantity
    TransactionValue: decimal
    TotalQuantity: decimal
    FilledQuantity: decimal
}

type TradingParameter = {
    maximalTransactionValue: decimal
}

module Http =
    let httpClient = new HttpClient()

    let asyncPost (url: string) (payload: string) : Async<HttpResponseMessage> =
        async {
            let content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json")
            let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
            return response
        }

    let asyncGet (url: string) : Async<HttpResponseMessage> =
        async {
            let! response = httpClient.GetAsync(url) |> Async.AwaitTask
            return response
        }

let processApiResponse (response: HttpResponseMessage) : Async<Result<string, string>> =
    async {
        match response.IsSuccessStatusCode with
        | true ->
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Ok content
        | false ->
            return Error ("API call failed with status: " + response.StatusCode.ToString())
    }

let parseOrderResponse (content: string) : Result<OrderID, string> =
    try
        Ok "Extracted Order ID from response"  // Placeholder for actual JSON parsing logic
    with
    | _ -> Error "Failed to parse order response"

let parseOrderStatus (content: string) : Result<(FulfillmentStatus, float), string> =
    try
        // Placeholder for actual JSON parsing logic
        Ok ("PartiallyFulfilled", 1.5)
    with
    | _ -> Error "Failed to parse order status"

let submitOrderAsync (orderDetails: OrderDetails) : Async<Result<OrderID, string>> = 
    async {
        match orderDetails.Exchange with
        | "Bitstamp" ->
            let marketSymbol = orderDetails.Currency + "usd"  // Placeholder for actual symbol format
            let orderFunction = 
                match orderDetails.OrderType with
                | "Buy" -> buyMarketOrder
                | "Sell" -> sellMarketOrder
                | _ -> failwith "Invalid order type"
            let! result = orderFunction marketSymbol (orderDetails.Quantity.ToString()) None
            match result with
            | Some responseString -> return parseJsonResponseOrderID responseString |> Async.Result
            | None -> return Result.Error "Failed to submit order on Bitstamp" |> Async.Result

        | "Kraken" ->
            let pair = orderDetails.Currency + "USD"  // Placeholder for actual pair format
            let! result = KrakenAPI.submitOrder pair orderDetails.OrderType (orderDetails.Quantity.ToString()) (orderDetails.Price.ToString())
            match result with
            | Some responseString -> return KrakenAPI.parseKrakenSubmitResponse responseString |> Async.Result
            | None -> return Result.Error "Failed to submit order on Kraken" |> Async.Result

        | "Bitfinex" ->
            let symbol = "t" + orderDetails.Currency.ToUpper() + "USD"
            let! result = BitfinexAPI.submitOrder "market" symbol (orderDetails.Quantity.ToString()) (orderDetails.Price.ToString())
            match result with
            | Some responseString -> return BitfinexAPI.parseBitfinexResponse responseString |> Async.Result
            | None -> return Result.Error "Failed to submit order on Bitfinex" |> Async.Result

        | _ -> 
            return Result.Error "Unsupported exchange" |> Async.Result
    }

let retrieveAndUpdateOrderStatus (orderID: OrderID) (orderDetails: OrderDetails) : Async<Result<OrderUpdate, string>> =
    async {
        match orderDetails.Exchange with
        | "Bitstamp" -> 
            let! statusResult = BitstampAPI.retrieveOrderTrades "BTCUSD" orderID //  First argument is a placeholder for symbol
            match statusResult with
            | Some response -> return BitstampAPI.parseResponseOrderStatus response |> Async.Result
            | None -> return Result.Error "Failed to retrieve order status from Bitstamp" |> Async.Result

        | "Kraken" ->
            let! statusResult = KrakenAPI.queryOrdersInfo orderID true None
            match statusResult with
            | Some response -> return KrakenAPI.parseKrakenOrderResponse response |> Async.Result
            | None -> return Result.Error "Failed to retrieve order status from Kraken" |> Async.Result

        | "Bitfinex" ->
            let! statusResult = BitfinexAPI.retrieveOrderTrades orderDetails.Currency orderID
            match statusResult with
            | Some response -> return BitfinexAPI.parseBitfinexOrderStatusResponse response |> Async.Result
            | None -> return Result.Error "Failed to retrieve order status from Bitfinex" |> Async.Result

        | _ ->
            return Result.Error "Unsupported exchange" |> Async.Result
    }

let storeOrderInDatabase order orderType =
    let document = [
        ("Type", orderType)
        ("TotalQuantity", order.TotalQuantity)
        ("FilledQuantity", order.FilledQuantity)
        ("TransactionValue", order.TransactionValue)
    ] |> BsonDocument
    insertDocument document |> ignore

let storeOrderUpdateInDatabase order updateType additionalOrderOption =
    let documents = 
        [
            [
                ("Type", "Original - " + updateType)
                ("TotalQuantity", order.TotalQuantity)
                ("FilledQuantity", order.FilledQuantity)
                ("TransactionValue", order.TransactionValue)
            ]
        ] @
        additionalOrderOption |> List.map (fun ao ->
            [
                ("Type", "Additional - " + updateType)
                ("TotalQuantity", ao.TotalQuantity)
                ("FilledQuantity", ao.FilledQuantity)
                ("TransactionValue", ao.TransactionValue)
            ]
        ) |> List.map BsonDocument
    insertManyDocuments documents |> ignore

let emitEvent (event: Event) =
    match event with
    | OrderFulfillmentUpdated update -> printfn "Order Fulfillment Updated: %A" update
    | UserNotificationSent message -> printfn "User Notification Sent: %s" message
    | OrderInitiated orderID -> printfn "Order Initiated: %s" orderID
    | OrderProcessed update -> printfn "Order Processed: %A" update

let workflowProcessOrders (input: InvokeOrderProcessing) (parameters: TradingParameter) =
    input.Orders
    |> List.fold (fun (acc, results) order ->
        let currentTransactionValue = acc + order.TransactionValue
        if currentTransactionValue > parameters.maximalTransactionValue then
            let errorEvent = DomainErrorRaised "Maximal transaction value exceeded. Halting trading."
            (acc, results @ [errorEvent])
        else
            let orderDetails = { Currency = order.Currency; Price = order.Price; OrderType = order.OrderType; Quantity = order.Quantity; Exchange = order.Exchange }
            let result = 
                async {
                    let! orderResult = submitOrderAsync orderDetails
                    match orderResult with
                    | Result.Ok orderID ->
                        storeOrderInDatabase order "Attempt"
                        let! statusResult = retrieveAndUpdateOrderStatus orderID orderDetails
                        match statusResult with
                        | Result.Ok orderUpdate ->
                            match orderUpdate.FulfillmentStatus with
                            | "FullyFulfilled" ->
                                storeOrderUpdateInDatabase order "Complete" None
                                return FullTransactionStored orderUpdate
                            | _ ->
                                storeOrderUpdateInDatabase order "Partial" (Some order)
                                return PartialTransactionStored orderUpdate
                        | Result.Error errMsg ->
                            return DomainErrorRaised errMsg
                    | Result.Error errMsg ->
                        return DomainErrorRaised errMsg
                } |> Async.RunSynchronously
            (currentTransactionValue, results @ [result])
    ) (0m, [])
    |> snd
    |> List.iter emitEvent

// Example usage:
// let invokeProcessing = { Orders = [{ Currency = "BTC"; Price = 10000.0; OrderType = "Buy"; Quantity = 1.0; Exchange = "Bitfinex"; TransactionValue = 10000m }]; UserEmail = "user@example.com" }
// let tradingParams = { maximalTransactionValue = 50000m }
// workflowProcessOrders invokeProcessing tradingParams
