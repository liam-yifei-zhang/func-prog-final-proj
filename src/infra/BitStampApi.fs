module BitstampAPI

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Collections.Generic

type Transaction = {
    Tid: int64
    Price: string
    Currency1: string
    Currency2: string
    Fee: string
    Datetime: string
    Type: int
}

type BitstampOrderResponse = {
    Id: int64
    Datetime: string
    Type: string
    Status: string
    Market: string
    Transactions: Transaction[]
    AmountRemaining: string
    ClientOrderId: string
}

let private httpClient = new HttpClient()

let parseResponseOrderStatus (jsonString: string) : Result<BitstampOrderResponse, string> =
    try
        let options = JsonSerializerOptions()
        options.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
        let parsedResponse = JsonSerializer.Deserialize<BitstampOrderResponse>(jsonString, options)
        Result.Ok parsedResponse
    with
    | ex -> Result.Error (sprintf "JSON parsing error: %s" ex.Message)

let postRequest (url: string) (data: (string * string) list) =
    async {
        printfn "Posting request to %A" data
        let content = new FormUrlEncodedContent(data |> Seq.map (fun (k, v) -> KeyValuePair(k, v)))
        let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Result.Ok (Some responseString)
        | _ -> return Result.Error "Failed to execute post request."
    }

let buyMarketOrder (currencyPair: string) (amount: float) (price: float) =
    let url = sprintf "https://18656-testing-server.azurewebsites.net/order/place/api/v2/buy/market/%s/" (currencyPair.ToLower())
    let data = [ "amount", amount.ToString(); "price", price.ToString() ]
    postRequest url data

let sellMarketOrder (currencyPair: string) (amount: float) (price: float) =
    let url = sprintf "https://18656-testing-server.azurewebsites.net/order/place/api/v2/sell/market/%s/" (currencyPair.ToLower())
    let requestBody = sprintf "amount=%f&price=%f" 22.45 58.06
    printfn "Request body: %s" requestBody
    let data = [ "amount", amount.ToString(); "price", price.ToString() ]
    postRequest url data

let orderStatus (orderId: string) =
    let url = "https://18656-testing-server.azurewebsites.net/order/status/api/v2/order_status/"
    let data = [ "id", orderId ]
    postRequest url data