module BitfinexAPI

open System.Net.Http
open System.Text
open Newtonsoft.Json
open System.Threading.Tasks
open FSharp.Data
let private httpClient = new HttpClient()

type BitfinexSubmitOrderResponse = {
    Mts: int64
    Type: string
    MessageId: int option
    Data: (int64 * int option * int option * string * int64 * int64 * float * float * string * string option * int option * int option * int * string * float option * float option * float option * float option * int option * int option) array
    Status: string
    Text: string
}

type BitfinexOrderTradesResponse = {
    Id: int
    Symbol: string
    Mts: int
    OrderId: int
    ExecAmount: float
    ExecPrice: float
    Maker: int
    Fee: float
    FeeCurrency: string
}

let parseSubmitOrderResponse (jsonString: string) : Result<int64, string> =
    try
        let response = JsonConvert.DeserializeObject<BitfinexSubmitOrderResponse>(jsonString)
        match response.Data with
        | [|(orderId, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _)|] ->
            Result.Ok orderId
        | _ -> 
            Result.Error "Invalid response format or multiple orders found."
    with
    | :? Newtonsoft.Json.JsonException as ex -> 
        Result.Error (sprintf "JSON parsing error: %s" ex.Message)

let parseOrderTradesResponse (jsonString: string) : Result<BitfinexOrderTradesResponse list, string> =
    try
        let trades = JsonConvert.DeserializeObject<BitfinexOrderTradesResponse[]>(jsonString)
        Result.Ok (trades |> Array.toList)
    with
    | :? Newtonsoft.Json.JsonException as ex -> 
        Result.Error (sprintf "JSON parsing error: %s" ex.Message)

let submitOrder (orderType: string) (symbol: string) (amount: string) (price: string) =
    async {
        let url = "https://api.bitfinex.com/v2/auth/w/order/submit"
        let payload = sprintf "{\"type\": \"%s\", \"symbol\": \"%s\", \"amount\": \"%s\", \"price\": \"%s\"}" orderType symbol amount price
        let content = new StringContent(payload, Encoding.UTF8, "application/json")
        let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Some (parseSubmitOrderResponse responseString)
        | false ->
            return None
    }
    
let retrieveOrderTrades (symbol: string) (orderId: int) =
    async {
        let url = sprintf "https://api.bitfinex.com/v2/auth/r/order/%s:%d/trades" symbol orderId
        let requestContent = new StringContent("", Encoding.UTF8, "application/json") 
        let! response = httpClient.PostAsync(url, requestContent) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Some (parseOrderTradesResponse responseString)
        | false ->
            return None
    }