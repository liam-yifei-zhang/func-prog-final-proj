module BitstampAPI

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Collections.Generic

type Transaction = {
    Tid: int64
    Price: string
    Fee: string
    Datetime: string
    Type: int
}

type BitstampOrderResponse = {
    Id: int64
    Datetime: string
    OrderType: string
    Status: string
    Market: string
    Transactions: Transaction[]
    AmountRemaining: string
    ClientOrderId: string
}

let private httpClient = new HttpClient()

let parseResponseOrderStatus (jsonString: string) : Result<BitstampOrderResponse, string> =
    try
        let parsedResponse = JsonSerializer.Deserialize<BitstampOrderResponse>(jsonString)
        Result.Ok parsedResponse
    with
    | ex ->
        Result.Error (sprintf "JSON parsing error: %s" ex.Message)

let postRequest (url: string) (data: (string * string) list) =
    async {
        let content = new FormUrlEncodedContent(data |> Seq.map (fun (k, v) -> KeyValuePair(k, v)))
        let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Result.Ok (Some responseString)
        | _ ->
            return Result.Error "Failed to execute post request."
    }

let buyMarketOrder (marketSymbol: string) (amount: string) (clientOrderId: Option<string>) =
    let url = sprintf "https://www.bitstamp.net/api/v2/buy/market/%s/" (marketSymbol.ToLower())
    let data = [ "amount", amount ] @ (match clientOrderId with | Some id -> [ "client_order_id", id ] | _ -> [])
    postRequest url data

let sellMarketOrder (marketSymbol: string) (amount: string) (clientOrderId: Option<string>) =
    let url = sprintf "https://www.bitstamp.net/api/v2/sell/market/%s/" (marketSymbol.ToLower())
    let data = [ "amount", amount ] @ (match clientOrderId with | Some id -> [ "client_order_id", id ] | _ -> [])
    postRequest url data

let orderStatus (orderId: string) =
    let url = "https://www.bitstamp.net/api/v2/order_status/"
    let data = [ "id", orderId ]
    postRequest url data

let fetchBitstampPairs = async {
    let url = "https://www.bitstamp.net/api/v2/trading-pairs-info/"
    try
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
        if response.IsSuccessStatusCode then
            let! jsonResponse = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            let pairs = JsonSerializer.Deserialize<JsonElement[]>(jsonResponse)
            let symbolList = pairs |> Array.map (fun x -> x.GetProperty("url_symbol").GetString())
            return Result.Ok symbolList
        else
            return Result.Error "Failed to fetch Bitstamp pairs due to HTTP error."
    with
    | ex ->
        return Result.Error (sprintf "Failed to fetch Bitstamp pairs: %s" ex.Message)
}