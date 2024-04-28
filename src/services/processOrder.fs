module ProcessOrder

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
    
    let parseOrderStatus (content: string) : Result<(FulfillmentStatus * float), string> =
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
    
    // let storeOrderInDatabase order orderType =
    //     let document = [
    //         ("Type", orderType)
    //         ("TotalQuantity", order.TotalQuantity)
    //         ("FilledQuantity", order.FilledQuantity)
    //         ("TransactionValue", order.TransactionValue)
    //     ] |> BsonDocument
    //     insertDocument document |> ignore
    
    // let storeOrderUpdateInDatabase order updateType additionalOrderOption =
    //     let documents = 
    //         [
    //             [
    //                 ("Type", "Original - " + updateType)
    //                 ("TotalQuantity", order.TotalQuantity)
    //                 ("FilledQuantity", order.FilledQuantity)
    //                 ("TransactionValue", order.TransactionValue)
    //             ]
    //         ] @
    //         additionalOrderOption |> List.map (fun ao ->
    //             [
    //                 ("Type", "Additional - " + updateType)
    //                 ("TotalQuantity", ao.TotalQuantity)
    //                 ("FilledQuantity", ao.FilledQuantity)
    //                 ("TransactionValue", ao.TransactionValue)
    //             ]
    //         ) |> List.map BsonDocument
    //     insertManyDocuments documents |> ignore
    
    let emitEvent (event: Event) getUserEmail =
        match event with
        | OrderFulfillmentUpdated update ->
            printfn "Order Fulfillment Updated: %A" update
    
        | UserNotificationSent message ->
            let userEmail = getUserEmail
            let emailSubject = "Trading Notification"
            let emailBody = sprintf "Attention: %s" message
            notifyUserViaEmail userEmail emailSubject emailBody
            printfn "User Notification Sent: %s to %s" emailBody userEmail
    
        | OrderInitiated orderID ->
            printfn "Order Initiated: %s" orderID
    
        | OrderProcessed update ->
            let userEmail = getUserEmail
            let emailSubject = "Order Processed Notification"
            let message = sprintf "Your order has been fully processed."
            let emailBody = sprintf "Attention: %s" message
            notifyUserViaEmail userEmail emailSubject emailBody
            printfn "Order Processed: %A" update
    
    let workflowProcessOrders (input: InvokeOrderProcessing) (parameters: TradingParameter) =
        let processOrder (acc, results) order =
            let currentTransactionValue = acc + order.TransactionValue
            if currentTransactionValue > parameters.maximalTransactionValue then
                let errorEvent = DomainErrorRaised "Maximal transaction value exceeded. Halting trading."
                let notificationEvent = UserNotificationSent "Maximal transaction value exceeded. Halting trading."
                (acc, results @ [notificationEvent; errorEvent])
            else
                let orderDetails = { Currency = order.Currency; Price = order.Price; OrderType = order.OrderType; Quantity = order.Quantity; Exchange = order.Exchange }
                let result =
                    async {
                        let! orderResult = submitOrderAsync orderDetails
                        match orderResult with
                        | Result.Ok orderID ->
                            printfn "Order Submitted: %s" orderID
                            let! statusResult = retrieveAndUpdateOrderStatus orderID orderDetails
                            match statusResult with
                            | Result.Ok orderUpdate ->
                                printfn "Order Update: %A" orderUpdate
                                let updateEvent =
                                    match orderUpdate.FulfillmentStatus with
                                    | "FullyFulfilled" ->
                                        printfn "Order Fully Fulfilled"
                                        OrderProcessed orderUpdate
                                    | _ ->
                                        printfn "Order Partially Fulfilled"
                                        OrderFulfillmentUpdated orderUpdate
                                return updateEvent
                            | Result.Error errMsg ->
                                return UserNotificationSent errMsg
                        | Result.Error errMsg ->
                            return UserNotificationSent errMsg
                    } |> Async.RunSynchronously
                (currentTransactionValue, results @ [result])
        input.Orders
        |> List.fold processOrder (0m, [])
        |> snd
        |> List.iter (fun event ->
            match event with
            | UserNotificationSent message -> printfn "Notification: %s" message
            | OrderProcessed update -> printfn "Processed Update: %A" update
            | _ -> ())  // Handle other events as needed
    
    
    // Example usage:
    let orders = [
        { Currency = "BTC"; Price = 50000.0; OrderType = "Buy"; Quantity = 0.1; TransactionValue = 5000.0; TotalQuantity = 0.1; FilledQuantity = 0.0; Exchange = "Bitstamp" }
        { Currency = "ETH"; Price = 2000.0; OrderType = "Sell"; Quantity = 1.0; TransactionValue = 2000.0; TotalQuantity = 1.0; FilledQuantity = 0.0; Exchange = "Kraken" }
    ]
    let input = { Orders = orders; UserEmail = "example@example.com" }
    let parameters = { maximalTransactionValue = 10000.0 }
    workflowProcessOrders input parameters
    
    