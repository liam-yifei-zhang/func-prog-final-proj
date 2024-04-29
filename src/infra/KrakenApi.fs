module KrakenAPI

open System
open System.Net.Http
open System.Text
open System.Text.Json

type SubmitDescription = {
    order: string
}

type SubmitResult = {
    descr: SubmitDescription
    txid: string[]
}

type KrakenSubmitResponse = {
    error: string[]
    result: SubmitResult option
}

type OrderDescription = {
    Pair: string
    Type: string
    Ordertype: string
    Price: string
    Price2: string
    Leverage: string
    Order: string
    Close: string
}

type OrderInfo = {
    Refid: string
    Userref: int
    Status: string
    Reason: string option
    Opentm: float
    Closetm: float
    Starttm: int
    Expiretm: int
    Descr: OrderDescription
    Vol: string
    Vol_exec: string
    Cost: string
    Fee: string
    Price: string
    Stopprice: string
    Limitprice: string
    Misc: string
    Oflags: string
    Trigger: string
    Trades: string[]
}

type KrakenOrderResponse = {
    Error: string[]
    Result: Map<string, OrderInfo>
}

let httpClient = new HttpClient()

let generateNonce () =
    let timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    let random = Random().Next(1000, 9999)
    timestamp * 10000L + int64 random

let parseKrakenSubmitResponse (jsonString: string) : Result<string[], string> =
    try
        let parsedResponse = JsonSerializer.Deserialize<KrakenSubmitResponse>(jsonString)
        match parsedResponse.error with
        | error when error.Length > 0 -> Result.Error (String.concat "; " error)
        | _ ->
            match parsedResponse.result with
            | Some result ->
                match result.txid with
                | txid when txid.Length > 0 -> Result.Ok txid
                | _ -> Result.Error "No transaction IDs found in the response"
            | None -> Result.Error "The 'result' field is missing in the response"
    with
    | ex -> Result.Error (sprintf "Kraken API JSON parsing error: %s" ex.Message)

let parseKrakenOrderResponse (jsonString: string) : Result<Map<string, OrderInfo>, string> =
    try
        let parsedResponse = JsonSerializer.Deserialize<KrakenOrderResponse>(jsonString)
        match parsedResponse.Error with
        | error when error.Length > 0 -> Result.Error (String.concat "; " error)
        | _ ->
            match parsedResponse.Result with
            | result when result.Count > 0 -> Result.Ok result
            | _ -> Result.Error "No order information found in the response"
    with
    | ex -> Result.Error (sprintf "Kraken API JSON parsing error: %s" ex.Message)

let submitOrder (pair: string) (orderType: string) (volume: string) (price: string) (orderTypeSpecific: string) =
    async {
        let url = "https://18656-testing-server.azurewebsites.net/order/place/0/private/AddOrder"
        let nonce = generateNonce()
        let payload = sprintf "nonce=%i&ordertype=%s&type=%s&volume=%s&pair=%s&price=%s" nonce orderTypeSpecific orderType volume pair price
        let content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded")
        let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseJson = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Some (parseKrakenSubmitResponse responseJson)
        | false -> return Some (Error "Failed to submit order")
    }

let queryOrdersInfo (transactionId: string) (includeTrades: bool) =
    async {
        let url = "https://18656-testing-server.azurewebsites.net/order/status/0/private/QueryOrders"
        let nonce = generateNonce()
        let payload = sprintf "nonce=%i&txid=%s&trades=%b" nonce transactionId includeTrades
        let content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded")

        httpClient.DefaultRequestHeaders.Clear()
        httpClient.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded")

        let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseJson = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            match parseKrakenOrderResponse responseJson with
            | Ok result -> return Some (Ok result)
            | Error errorMsg -> return Some (Error errorMsg)
        | false -> return Some (Error "Failed to query order info")
    }