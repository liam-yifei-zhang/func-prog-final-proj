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

let emitEvent (event: Event) =
    match event with
    | OrderFulfillmentUpdated update -> printfn "Order Fulfillment Updated: %A" update
    | UserNotificationSent message -> printfn "User Notification Sent: %s" message
    | OrderInitiated orderID -> printfn "Order Initiated: %s" orderID
    | OrderProcessed update -> printfn "Order Processed: %A" update

let workflowProcessOrders (input: InvokeOrderProcessing) (parameters: TradingParameter) =
    let processOrder (cumulativeTransactionValue, results) order =
        let orderDetails = { Currency = order.Currency; Price = order.Price; OrderType = order.OrderType; Quantity = order.Quantity; Exchange = order.Exchange }
        let result =
            async {
                emitEvent (OrderInitiated "Order ID placeholder")  // Placeholder for actual order ID
                let! orderResult = submitOrderAsync orderDetails
                let! statusResult = retrieveAndUpdateOrderStatus orderID orderDetails
                let event =
                    match orderResult, statusResult with
                    | Result.Ok orderID, Result.Ok orderUpdate ->
                        emitEvent (OrderInitiated orderID)
                        match orderUpdate.FulfillmentStatus with
                        | "FullyFulfilled" -> OrderProcessed orderUpdate
                        | _ -> OrderFulfillmentUpdated orderUpdate
                    | Result.Error errMsg, _ ->
                        emitEvent (UserNotificationSent errMsg)
                        DomainErrorRaised errMsg
                    | _, Result.Error errMsg ->
                        emitEvent (UserNotificationSent errMsg)
                        DomainErrorRaised errMsg
                emitEvent event
                match event with
                | OrderProcessed update -> FullTransactionStored update
                | OrderFulfillmentUpdated update -> PartialTransactionStored update
                | DomainErrorRaised error -> DomainErrorRaised error
            }
            |> Async.RunSynchronously
        (cumulativeTransactionValue + order.TransactionValue, results @ [result])

    let _, orderResults = input.Orders |> List.fold processOrder (0m, [])
    
    orderResults |> List.iter (function
        | OrdersProcessed result -> emitEvent result)

// Example usage:
// let orders = [
//     { Currency = "BTC"; Price = 50000.0; OrderType = "Buy"; Quantity = 0.1; TransactionValue = 5000.0m; TotalQuantity = 0.1m; FilledQuantity = 0.0m }
//     { Currency = "ETH"; Price = 2000.0; OrderType = "Sell"; Quantity = 1.0; TransactionValue = 2000.0m; TotalQuantity = 1.0m; FilledQuantity = 0.0m }
// ]

// let input = { Orders = orders; UserEmail = "user@gmail.com" }
// let parameters = { maximalTransactionValue = 10000.0m }

// workflowProcessOrders input parameters

let storeCompletedTransactionInDatabase (order: Order) =
    let document = BsonDocument([
        ("Type", BsonString("Complete"))
        ("TotalQuantity", BsonDouble(order.TotalQuantity))
        ("FilledQuantity", BsonDouble(order.FilledQuantity))
        ("TransactionValue", BsonDouble(order.TransactionValue))
    ])
    insertDocument document |> ignore

let storeCompletedAndAdditionalOrdersInDatabase (originalOrder: Order) (additionalOrder: Order) =
    let documents = [
        BsonDocument([
            ("Type", BsonString("Original"))
            ("TotalQuantity", BsonDouble(originalOrder.TotalQuantity))
            ("FilledQuantity", BsonDouble(originalOrder.FilledQuantity))
            ("TransactionValue", BsonDouble(originalOrder.TransactionValue))
        ])
        BsonDocument([
            ("Type", BsonString("Additional"))
            ("TotalQuantity", BsonDouble(additionalOrder.TotalQuantity))
            ("FilledQuantity", BsonDouble(additionalOrder.FilledQuantity))
            ("TransactionValue", BsonDouble(additionalOrder.TransactionValue))
        ])
    ]
    insertManyDocuments documents |> ignore

let storeTransactionAttemptInDatabase (order: Order) =
    let document = BsonDocument([
        ("Type", BsonString("Attempt"))
        ("TotalQuantity", BsonDouble(order.TotalQuantity))
        ("FilledQuantity", BsonDouble(order.FilledQuantity))
        ("TransactionValue", BsonDouble(order.TransactionValue))
    ])
    insertDocument document |> ignore
