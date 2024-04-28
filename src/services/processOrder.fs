module ProcessOrder

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

let submitOrderAsync (orderDetails: OrderDetails) : Async<Result<OrderID, string>> = 
    async {
        match orderDetails.Exchange with
        | "Bitstamp" ->
            let marketSymbol = orderDetails.Currency + "usd"  // Placeholder for actual symbol format
            let orderFunction = 
                match orderDetails.OrderType with
                | "Buy" -> BitstampAPI.buyMarketOrder
                | "Sell" -> BitstampAPI.sellMarketOrder
                | _ -> failwith "Invalid order type"
            let! result = orderFunction marketSymbol (orderDetails.Quantity.ToString()) None
            match result with
            | Ok (Some responseString) ->
                match parseOrderResponse responseString with
                | Ok orderID -> return Ok orderID
                | Error errorMsg -> return Error errorMsg
            | Ok None -> return Error "Empty response received"
            | Error errorMsg -> return Error errorMsg

        | "Kraken" ->
            let pair = orderDetails.Currency + "USD"  // Placeholder for actual pair format
            let! result = KrakenAPI.submitOrder pair orderDetails.OrderType (orderDetails.Quantity.ToString()) (orderDetails.Price.ToString()) "market"
            match result with
            | Ok orderIDs -> return Ok orderIDs.[0]
            | Error errorMsg -> return Error errorMsg

        | "Bitfinex" ->
            let symbol = "t" + orderDetails.Currency.ToUpper() + "USD"
            let! result = BitfinexAPI.submitOrder orderDetails.OrderType symbol (orderDetails.Quantity.ToString()) (orderDetails.Price.ToString())
            match result with
            | Some (Ok orderID) -> return Ok (orderID.ToString())
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
                    let orderUpdate = {
                        OrderID = orderID
                        OrderDetails = orderDetails
                        FulfillmentStatus = response.Status
                        RemainingQuantity = response.AmountRemaining |> float
                    }
                    return Ok orderUpdate
                | Error errorMsg -> return Error errorMsg
            | Ok None -> return Error "Empty response received from Bitstamp"
            | Error errorMsg -> return Error errorMsg

        | "Kraken" ->
            let! statusResult = KrakenAPI.queryOrdersInfo orderID true None
            match statusResult with
            | Ok response ->
                let orderInfo = response |> Map.toList |> List.head |> snd
                let orderUpdate = {
                    OrderID = orderID
                    OrderDetails = orderDetails
                    FulfillmentStatus = orderInfo.Status
                    RemainingQuantity = orderInfo.Vol_exec |> float
                }
                return Ok orderUpdate
            | Error errorMsg -> return Error errorMsg

        | "Bitfinex" ->
            let! statusResult = BitfinexAPI.retrieveOrderTrades orderDetails.Currency (int orderID)
            match statusResult with
            | Some (Ok response) ->
                let executedQuantity = response |> List.sumBy (fun trade -> trade.ExecAmount)
                let orderUpdate = {
                    OrderID = orderID
                    OrderDetails = orderDetails
                    FulfillmentStatus = if executedQuantity = orderDetails.Quantity then "FullyFulfilled" else "PartiallyFulfilled"
                    RemainingQuantity = orderDetails.Quantity - executedQuantity
                }
                return Ok orderUpdate
            | Some (Error errorMsg) -> return Error errorMsg
            | None -> return Error "Failed to retrieve order status from Bitfinex"

        | _ ->
            return Error "Unsupported exchange"
    }

let emitEvent (event: Event) getUserEmail =
    match event with
    | OrderFulfillmentUpdated update ->
        printfn "Order Fulfillment Updated: %A" update

    | UserNotificationSent message ->
        let userEmail = getUserEmail
        let emailSubject = "Trading Notification"
        let emailBody = sprintf "Attention: %s" message
        // Implement notifyUserViaEmail function
        printfn "User Notification Sent: %s to %s" emailBody userEmail

    | OrderInitiated orderID ->
        printfn "Order Initiated: %s" orderID

    | OrderProcessed update ->
        let userEmail = getUserEmail
        let emailSubject = "Order Processed Notification"
        let message = sprintf "Your order has been fully processed."
        let emailBody = sprintf "Attention: %s" message
        // Implement notifyUserViaEmail function
        printfn "Order Processed: %A" update

    | DomainErrorRaised errorMsg ->
        printfn "Domain Error Raised: %s" errorMsg

let workflowProcessOrders (input: InvokeOrderProcessing) (parameters: TradingParameter) =
    let processOrder (acc, results) orderDetails =
        let currentTransactionValue = acc + (decimal orderDetails.Quantity * decimal orderDetails.Price)
        if currentTransactionValue > parameters.maximalTransactionValue then
            let errorEvent = DomainErrorRaised "Maximal transaction value exceeded. Halting trading."
            let notificationEvent = UserNotificationSent "Maximal transaction value exceeded. Halting trading."
            (acc, results @ [notificationEvent; errorEvent])
        else
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
    { Currency = "BTC"; Price = 50000.0; OrderType = "Buy"; Quantity = 0.1; Exchange = "Bitstamp" }
    { Currency = "ETH"; Price = 2000.0; OrderType = "Sell"; Quantity = 1.0; Exchange = "Kraken" }
]
let input = { Orders = orders; UserEmail = "example@example.com" }
let parameters = { maximalTransactionValue = 2000m }
workflowProcessOrders input parameters