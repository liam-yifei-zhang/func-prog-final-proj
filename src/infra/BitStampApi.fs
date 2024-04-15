module BitstampAPI

open System
open System.Net.Http
open System.Text
open Newtonsoft.Json

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
        match JsonConvert.DeserializeObject<BitstampOrderResponse>(jsonString) with
        | parsedResponse -> Result.Ok parsedResponse
        | exception ex -> Result.Error (sprintf "JSON parsing error: %s" ex.Message)
    with
    | ex: Newtonsoft.Json.JsonException -> 
        Result.Error (sprintf "JSON parsing error: %s" ex.Message)

let postRequest (url: string) (data: (string * string) list) =
    async {
        let content = new FormUrlEncodedContent(data |> Seq.map (fun (k, v) -> new System.Collections.Generic.KeyValuePair<string, string>(k, v)))
        let! response = Async.AwaitTask (httpClient.PostAsync(url, content))
        match response.IsSuccessStatusCode with
        | true -> 
            let! responseString = Async.AwaitTask (response.Content.ReadAsStringAsync())
            Debug.WriteLine($"Response JSON: {responseString}")
            Some responseString |> Result.Ok
        | _ -> 
            Debug.WriteLine($"Error: {response.StatusCode}")
            None |> Result.Error "Failed to execute post request."
    }

let buyMarketOrder (marketSymbol: string) (amount: string) (clientOrderId: Option<string>) =
    let url = sprintf "https://www.bitstamp.net/api/v2/buy/market/%s/" (marketSymbol.ToLower())
    let data = 
        [ "amount", amount ] @ 
        (match clientOrderId with 
         | Some id -> [ "client_order_id", id ] 
         | _ -> [])
    postRequest url data

let sellMarketOrder (marketSymbol: string) (amount: string) (clientOrderId: Option<string>) =
    let url = sprintf "https://www.bitstamp.net/api/v2/sell/market/%s/" (marketSymbol.ToLower())
    let data = 
        [ "amount", amount ] @ 
        (match clientOrderId with 
         | Some id -> [ "client_order_id", id ] 
         | _ -> [])
    postRequest url data

let orderStatus (orderId: string) =
    let url = "https://www.bitstamp.net/api/v2/order_status/"
    let data = [ "id", orderId ]
    postRequest url data
