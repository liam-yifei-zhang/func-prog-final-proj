module ProcessOrder

open System
open System.Net.Http
open BitfinexAPI
open KrakenAPI
open BitstampAPI
open System.Text.Json
open System.Net.Mail
open MongoDBUtil
open MongoDB.Bson
open MongoDB.Driver

type Currency = string
type Price = float
type OrderType = string
type Quantity = float
type Exchange = string
type OrderID = string
type FulfillmentStatus = string
type testResponse = string

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
    | DomainErrorRaised of string

type InvokeOrderProcessing = {
    Orders: OrderDetails list
    UserEmail: string
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

let sendEmailNotification (recipient: string) (subject: string) (body: string) =
    try
        use smtpClient = new SmtpClient("smtp.gmail.com", 587) 
        smtpClient.EnableSsl <- true
        smtpClient.Credentials <- new System.Net.NetworkCredential("simoniharden@gmail.com", "gxlk gyms skgi bepr") 
        let mailMessage = new MailMessage()
        mailMessage.From <- new MailAddress("simoniharden@gmail.com") 
        mailMessage.To.Add(recipient)
        mailMessage.Subject <- subject
        mailMessage.Body <- body
        smtpClient.Send(mailMessage)
        Ok ()
    with
    | ex ->
        printfn "Exception occurred while sending email: %s" ex.Message
        printfn "Exception StackTrace: %s" ex.StackTrace
        Error ex.Message

let serializeOrderDetails orderID orderDetails =
    let orderDetailsObj = 
        {| OrderID = orderID
           OrderDetails = 
               {| Currency = orderDetails.Currency
                  Price = orderDetails.Price
                  OrderType = orderDetails.OrderType
                  Quantity = orderDetails.Quantity
                  Exchange = orderDetails.Exchange |} |}
    System.Text.Json.JsonSerializer.Serialize(orderDetailsObj)

let serializeOrderUpdate (orderUpdate: OrderUpdate) =
    let orderUpdateObj = 
        {| OrderID = orderUpdate.OrderID
           OrderDetails = 
               {| Currency = orderUpdate.OrderDetails.Currency
                  Price = orderUpdate.OrderDetails.Price
                  OrderType = orderUpdate.OrderDetails.OrderType
                  Quantity = orderUpdate.OrderDetails.Quantity
                  Exchange = orderUpdate.OrderDetails.Exchange |}
           FulfillmentStatus = orderUpdate.FulfillmentStatus
           RemainingQuantity = orderUpdate.RemainingQuantity |}
    JsonSerializer.Serialize(orderUpdateObj)

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
        let jsonDocument = JsonDocument.Parse(content)
        let rootElement = jsonDocument.RootElement
        match rootElement.TryGetProperty("id") with
        | true, property -> Ok (property.GetString())
        | _ -> Error "Failed to parse order response: 'id' property not found"
    with
    | ex -> Error (sprintf "Failed to parse order response: %s" ex.Message)

let submitOrderAsync (orderDetails: OrderDetails) : Async<Result<OrderID, string>> = 
    async {
        match orderDetails.Exchange with
        | "Bitstamp" ->
            let currencyPair = orderDetails.Currency + "USD"  
            let orderFunction = 
                match orderDetails.OrderType with
                | "Buy" -> BitstampAPI.buyMarketOrder
                | "Sell" -> BitstampAPI.sellMarketOrder
                | _ -> failwith "Invalid order type"
            let! result = orderFunction currencyPair orderDetails.Quantity orderDetails.Price
            printfn "Bitstamp order result: %A" result
            match result with
            | Ok (Some responseString) ->
                match parseOrderResponse responseString with
                | Ok orderID ->
                    let orderDetailsJson = serializeOrderDetails orderID orderDetails
                    let orderDetailsDoc = BsonDocument.Parse(orderDetailsJson)
                    MongoDBUtil.insertDocument "orderDetails" orderDetailsDoc |> ignore
                    return Ok orderID
                | Error errorMsg -> return Error errorMsg
            | _ -> return Error "Failed to submit order on Bitstamp"

        | "Kraken" ->
            let pair = orderDetails.Currency + "USD" 
            let! result = KrakenAPI.submitOrder pair orderDetails.OrderType (orderDetails.Quantity.ToString()) (orderDetails.Price.ToString()) "market"
            match result with
            | Some (Ok orderIDs) ->
                let orderID = orderIDs.[0]
                let orderDetailsJson = serializeOrderDetails orderID orderDetails
                let orderDetailsDoc = BsonDocument.Parse(orderDetailsJson)
                MongoDBUtil.insertDocument "orderDetails" orderDetailsDoc |> ignore
                return Ok orderID
            | Some (Error errorMsg) -> return Error errorMsg
            | None -> return Error "Failed to submit order on Kraken"
        
        | "Bitfinex" ->
            let symbol = "t" + orderDetails.Currency.ToUpper() + "USD"
            let! result = BitfinexAPI.submitOrder "MARKET" symbol (orderDetails.Quantity.ToString()) (orderDetails.Price.ToString())
            match result with
            | Some (Ok orderId) ->
                let orderID = string orderId
                let orderDetailsJson = serializeOrderDetails orderID orderDetails
                let orderDetailsDoc = BsonDocument.Parse(orderDetailsJson)
                MongoDBUtil.insertDocument "orderDetails" orderDetailsDoc |> ignore
                return Ok orderID
            | Some (Error errorMsg) -> return Error errorMsg
            | None -> return Error "Failed to submit order on Bitfinex"
        
        | _ -> 
            return Error "Unsupported exchange"
    }

let retrieveAndUpdateOrderStatus (orderID: OrderID) (orderDetails: OrderDetails) : Async<Result<OrderUpdate, string>> =
    async {
        match orderDetails.Exchange with
        | "Bitstamp" -> 
            let! statusResult = BitstampAPI.orderStatus orderID
            match statusResult with
            | Ok (Some responseString) ->
                match BitstampAPI.parseResponseOrderStatus responseString with
                | Ok response ->
                    let remainingQuantity =
                        match Double.TryParse(response.AmountRemaining) with
                        | true, quantity -> quantity
                        | _ -> 0.0
                    let orderUpdate = {
                        OrderID = orderID
                        OrderDetails = orderDetails
                        FulfillmentStatus = 
                            match response.Status with
                            | "Finished" -> "FullyFulfilled"
                            | "Open" -> "PartiallyFulfilled"
                            | _ -> "OneSideFilled"
                        RemainingQuantity = remainingQuantity
                    }
                    
                    match orderUpdate.FulfillmentStatus with
                    | "OneSideFilled" ->
                        let userEmail = "ashishkj@andrew.cmu.edu" 
                        let emailSubject = "Order Partially Filled Notification"
                        let message = sprintf "Your order %s has only one side filled." orderID
                        let emailBody = sprintf "Attention: %s" message
                        match sendEmailNotification userEmail emailSubject emailBody with
                        | Ok () ->
                            let orderUpdateJson = serializeOrderUpdate orderUpdate
                            let transactionHistoryDoc = BsonDocument.Parse(orderUpdateJson)
                            MongoDBUtil.insertDocument "testTransactionHistory" transactionHistoryDoc |> ignore
                            return Ok orderUpdate
                        | Error errorMsg ->
                            return Error errorMsg
                    | _ ->
                        return Ok orderUpdate
                | Error errorMsg -> return Error errorMsg
            | Ok None -> return Error "Empty response received from Bitstamp"
            | Error errorMsg -> return Error errorMsg

        | "Kraken" ->
            let! statusResult = KrakenAPI.queryOrdersInfo orderID true
            match statusResult with
            | Some (Ok response) ->
                let orderInfo = response |> Map.toList |> List.head |> snd
                let orderUpdate = {
                    OrderID = orderID
                    OrderDetails = orderDetails
                    FulfillmentStatus = 
                        match orderInfo.Status with
                        | "closed" -> "FullyFulfilled"
                        | "open" -> "PartiallyFulfilled"
                        | _ -> "OneSideFilled"
                    RemainingQuantity = orderInfo.Vol |> float
                }
                
                match orderUpdate.FulfillmentStatus with
                | "OneSideFilled" ->
                    let userEmail = "ashishkj@andrew.cmu.edu" 
                    let emailSubject = "Order Partially Filled Notification"
                    let message = sprintf "Your order %s has only one side filled." orderID
                    let emailBody = sprintf "Attention: %s" message
                    match sendEmailNotification userEmail emailSubject emailBody with
                    | Ok () ->
                        let orderUpdateJson = serializeOrderUpdate orderUpdate
                        let transactionHistoryDoc = BsonDocument.Parse(orderUpdateJson)
                        MongoDBUtil.insertDocument "testTransactionHistory" transactionHistoryDoc |> ignore
                        return Ok orderUpdate
                    | Error errorMsg ->
                        return Error errorMsg
                | _ ->
                    return Ok orderUpdate
            | Some (Error errorMsg) -> return Error errorMsg
            | None -> return Error "Failed to retrieve order status from Kraken"
                
        | "Bitfinex" ->
            let currencyPair = "t" + orderDetails.Currency.ToUpper() + "USD"
            let! statusResult = BitfinexAPI.retrieveOrderTrades currencyPair (int orderID)
            match statusResult with
            | Some (Ok trades) ->
                let executedQuantity = trades |> List.sumBy (fun trade -> trade.ExecAmount)
                let orderUpdate = {
                    OrderID = orderID
                    OrderDetails = orderDetails
                    FulfillmentStatus = 
                        if executedQuantity = orderDetails.Quantity then "FullyFulfilled"
                        elif executedQuantity > 0.0 then "PartiallyFulfilled"
                        else "OneSideFilled"
                    RemainingQuantity = orderDetails.Quantity - executedQuantity
                }
                
                match orderUpdate.FulfillmentStatus with
                | "OneSideFilled" ->
                    let userEmail = "ashishkj@andrew.cmu.edu" 
                    let emailSubject = "Order Partially Filled Notification"
                    let message = sprintf "Your order %s has only one side filled." orderID
                    let emailBody = sprintf "Attention: %s" message
                    match sendEmailNotification userEmail emailSubject emailBody with
                    | Ok () ->
                        let orderUpdateJson = serializeOrderUpdate orderUpdate
                        let transactionHistoryDoc = BsonDocument.Parse(orderUpdateJson)
                        MongoDBUtil.insertDocument "testTransactionHistory" transactionHistoryDoc |> ignore
                        return Ok orderUpdate
                    | Error errorMsg ->
                        return Error errorMsg
                | _ ->
                    return Ok orderUpdate
            | Some (Error errorMsg) -> return Error errorMsg
            | None -> return Error "Failed to retrieve order status from Bitfinex"

        | _ ->
            return Error "Unsupported exchange"
    }

let emitEvent (event: Event) =
    let getUserEmail () = "simoniharden@gmail.com"  

    match event with
    | OrderFulfillmentUpdated update ->
        printfn "Order Fulfillment Updated: %A" update

    | UserNotificationSent message ->
        let userEmail = getUserEmail ()
        let emailSubject = "Trading Notification"
        let emailBody = sprintf "Attention: %s" message
        match sendEmailNotification userEmail emailSubject emailBody with
        | Ok () -> printfn "User Notification Sent: %s" message
        | Error errorMsg -> printfn "Failed to send user notification: %s" errorMsg

    | OrderInitiated orderID ->
        printfn "Order Initiated: %s" orderID

    | OrderProcessed update ->
        let userEmail = getUserEmail ()
        let emailSubject = "Order Processed Notification"
        let message = "Your order has been fully processed."
        let emailBody = sprintf "Attention: %s" message
        match sendEmailNotification userEmail emailSubject emailBody with
        | Ok () -> printfn "Order Processed Notification Sent: %A" update
        | Error errorMsg -> printfn "Failed to send order processed notification: %s" errorMsg

    | DomainErrorRaised errorMsg ->
        printfn "Domain Error Raised: %s" errorMsg

let workflowProcessOrders (input: InvokeOrderProcessing) (parameters: TradingParameter) =
    let processOrder (acc, results) orderDetails =
        let currentTransactionValue = acc + (decimal orderDetails.Quantity * decimal orderDetails.Price)
        let errorMsg = "Maximal transaction value exceeded. Halting trading."
        let errorEvent = DomainErrorRaised errorMsg
        let notificationEvent = UserNotificationSent errorMsg
        async {
            match currentTransactionValue > parameters.maximalTransactionValue with
            | true ->
                match sendEmailNotification input.UserEmail "Transaction Limit Exceeded" errorMsg with
                | Ok () -> printfn "Notification sent to %s" input.UserEmail
                | Error errMsg -> printfn "Failed to send notification: %s" errMsg
                return (acc, results @ [notificationEvent; errorEvent])
            | false ->
                let result =
                    async {
                        let! orderResult = submitOrderAsync orderDetails
                        match orderResult with
                        | Ok orderID ->
                            printfn "Order Submitted: %s" orderID
                            let! statusResult = retrieveAndUpdateOrderStatus orderID orderDetails
                            match statusResult with
                            | Ok orderUpdate ->
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
                            | Error errMsg ->
                                return UserNotificationSent errMsg
                        | Error errMsg ->
                            return UserNotificationSent errMsg
                    } |> Async.RunSynchronously
                return (currentTransactionValue, results @ [result])
        } |> Async.RunSynchronously
    input.Orders
    |> List.fold processOrder (0m, [])
    |> snd
    |> List.iter (fun event ->
        match event with
        | UserNotificationSent message -> printfn "Notification: %s" message
        | OrderProcessed update -> printfn "Processed Update: %A" update
        | _ -> ())

// Test the process order workflow
// let testNotificationOnMaxTransaction() =
//     // Test parameters
//     let tradingParameters = { maximalTransactionValue = 100m }
//     let userEmail = "ashishkj@andrew.cmu.edu"
//     let orderDetails = 
//         [{ Currency = "FET"; Price = 58.06; OrderType = "Buy"; Quantity = 22.45; Exchange = "Bitstamp" }]
//     let input = { Orders = orderDetails; UserEmail = userEmail }

//     // Run the workflow process
//     workflowProcessOrders input tradingParameters