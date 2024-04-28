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
    result: SubmitResult
}

type OrderDescription = {
    Pair: string
    OrderType: string
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

let private httpClient = new HttpClient()

let generateNonce () =
    let timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    let random = Random().Next(1000, 9999)
    timestamp * 10000L + int64 random

let parseKrakenSubmitResponse (jsonString: string) : Result<string[], string> =
    try
        let parsedResponse = JsonSerializer.Deserialize<KrakenSubmitResponse>(jsonString)
        match parsedResponse.error with
        | error when error.Length > 0 -> Result.Error (String.concat "; " error)
        | _ -> Result.Ok parsedResponse.result.txid
    with
    | ex -> 
        Result.Error (sprintf "JSON parsing error: %s" ex.Message)

let parseKrakenOrderResponse (jsonString: string) : Result<Map<string, OrderInfo>, string> =
    try
        let parsedResponse = JsonSerializer.Deserialize<KrakenOrderResponse>(jsonString)
        match parsedResponse.Error with
        | error when error.Length > 0 -> Result.Error (String.concat "; " error)
        | _ -> Result.Ok parsedResponse.Result
    with
    | ex -> 
        Result.Error (sprintf "JSON parsing error: %s" ex.Message)

let submitOrder (pair: string) (orderType: string) (volume: string) (price: string) (orderTypeSpecific: string) =
    async {
        let url = "https://api.kraken.com/0/private/AddOrder"
        let nonce = generateNonce ()
        let payload = sprintf "nonce=%i&pair=%s&type=%s&ordertype=%s&price=%s&volume=%s" nonce pair orderType orderTypeSpecific price volume
        let content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded")
        let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return parseKrakenSubmitResponse responseString
        | _ -> return Result.Error "Failed to submit order due to HTTP error"
    }

let queryOrdersInfo (transactionIds: string) (includeTrades: bool) (userRef: int option) =
    async {
        let url = "https://api.kraken.com/0/private/QueryOrders"
        let nonce = generateNonce ()
        let basePayload = sprintf "nonce=%i&txid=%s" nonce transactionIds
        let tradePayload = match includeTrades with
                           | true -> "&trades=true"
                           | false -> ""
        let userRefPayload = match userRef with
                             | Some ref -> sprintf "&userref=%d" ref
                             | None -> ""
        let payload = basePayload + tradePayload + userRefPayload
        let content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded")
        
        httpClient.DefaultRequestHeaders.Clear()
        
        let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return parseKrakenOrderResponse responseString
        | _ -> return Result.Error "Failed to query order info due to HTTP error"
    }

let fetchKrakenPairs = async {
    let url = "https://api.kraken.com/0/public/AssetPairs"
    try
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
        if response.IsSuccessStatusCode then
            let! jsonResponse = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            let parsedJson = JsonDocument.Parse(jsonResponse)
            let pairs = parsedJson.RootElement.GetProperty("result")
                        |> fun result -> result.EnumerateObject()
                        |> Seq.map (fun p -> p.Name)
                        |> Seq.toList
            return Result.Ok pairs
        else
            return Result.Error "Failed to fetch Kraken pairs due to HTTP error."
    with
    | ex ->
        return Result.Error (sprintf "Failed to fetch Kraken pairs: %s" ex.Message)
}