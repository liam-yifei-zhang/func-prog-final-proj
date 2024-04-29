module BitfinexAPI

open System.Net.Http
open System.Text
open System.Text.Json

let private httpClient = new HttpClient()

type BitfinexSubmitOrderResponse = {
    Mts: int64
    Type: string
    MessageId: int option
    Data: (int64 * int option * int64 * string * int64 * int64 * float * float * string * string option * int option * int option * int * string * float option * float option * float * int * int option * int option * string option * int * int * string option * string option * string option * string * string option * string option * string option)[]
    Status: string
    Text: string
}

type BitfinexOrderTradesResponse = {
    Id: int64
    Symbol: string
    Mts: int64
    OrderId: int64
    ExecAmount: float
    ExecPrice: float
    OrderType: int
    OrderPrice: float
    Maker: int
    Fee: float
    FeeCurrency: string
    Cid: int
}

let parseSubmitOrderResponse (jsonString: string) : Result<int64, string> =
    try
        let response = JsonSerializer.Deserialize<JsonElement>(jsonString)
        let status = response.[6].GetString()
        if status = "SUCCESS" then
            let dataArray = response.[4].EnumerateArray() |> Seq.head
            let orderId = dataArray.[0].GetInt64()
            Result.Ok orderId
        else
            Result.Error "Response status is not SUCCESS"
    with
    | ex -> Result.Error (sprintf "JSON parsing error: %s" ex.Message)




let parseOrderTradesResponse (jsonString: string) : Result<BitfinexOrderTradesResponse list, string> =
    try
        let trades = JsonSerializer.Deserialize<obj[][]>(jsonString)
        let tradesList = trades |> Array.map (fun tradeArray ->
            match tradeArray with
            | [| :? int64 as id; :? string as symbol; :? int64 as mts; :? int64 as orderId; 
                 :? float as execAmount; :? float as execPrice; _; _; 
                 :? int as orderType; :? float as orderPrice; :? string as feeCurrency; :? int as cid |] ->
                {
                    Id = id
                    Symbol = symbol
                    Mts = mts
                    OrderId = orderId
                    ExecAmount = execAmount
                    ExecPrice = execPrice
                    OrderType = orderType
                    OrderPrice = orderPrice
                    Maker = orderType
                    Fee = orderPrice
                    FeeCurrency = feeCurrency
                    Cid = cid
                }
            | _ -> failwith "Unexpected trade format"
        )
        Result.Ok (Array.toList tradesList)
    with
    | ex -> Result.Error (sprintf "Bitfinex JSON parsing error: %s" ex.Message)

let submitOrder (orderType: string) (symbol: string) (amount: string) (price: string) =
    async {
        let url = "https://18656-testing-server.azurewebsites.net/order/place/v2/auth/w/order/submit"
        let payload = sprintf "type=%s&symbol=%s&amount=%s&price=%s" orderType symbol amount price
        let content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded")
        let! response = httpClient.PostAsync(url, content) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseJson = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            match parseSubmitOrderResponse responseJson with
            | Ok result -> return Some (Ok result)
            | Error errorMsg -> return Some (Error errorMsg)
        | false -> return Some (Error "Failed to submit order")
    }

let retrieveOrderTrades (symbol: string) (orderId: int) =
    async {
        let url = sprintf "https://18656-testing-server.azurewebsites.net/order/status/auth/r/order/%s:%d/trades" symbol orderId
        let! response = httpClient.GetAsync(url) |> Async.AwaitTask
        match response.IsSuccessStatusCode with
        | true ->
            let! responseJson = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return Some (parseOrderTradesResponse responseJson)
        | false -> return None
    }