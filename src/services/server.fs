// Server.fs
module Server

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

open Newtonsoft.Json
open MongoDB.Bson
open MongoDB.Driver
open MongoDBUtil

type TradingStrategy = {
    Currencys: int
    MinimalPriceSpread: float
    MinimalTransactionProfit: float
    MaximalTransactionValue: float
    MaximalTradingValue: float
    Email: string
}

let tradingStrategyRoute =
    path "/strategies" >=> request (fun r ->
        let Currencys = match r.queryParam "Currencys" with
            | Choice1Of2 s -> int s
            | _ -> 1
        let MinimalPriceSpread = match r.queryParam "MinimalPriceSpread" with
            | Choice1Of2 s -> float s
            | _ -> 1.0
        let MinimalTransactionProfit = match r.queryParam "MinimalTransactionProfit" with
            | Choice1Of2 s -> float s
            | _ -> 1.0
        let MaximalTransactionValue = match r.queryParam "MaximalTransactionValue" with
            | Choice1Of2 s -> float s
            | _ -> 1.0
        let MaximalTradingValue = match r.queryParam "MaximalTradingValue" with
            | Choice1Of2 s -> float s
            | _ -> 1.0
        let Email = match r.queryParam "Email" with
            | Choice1Of2 s -> s
            | _ -> " "
        let document = BsonDocument([
            BsonElement("Currencys", BsonInt32 Currencys)
            BsonElement("MinimalPriceSpread", BsonDouble MinimalPriceSpread)
            BsonElement("MinimalTransactionProfit", BsonDouble MinimalTransactionProfit)
            BsonElement("MaximalTransactionValue", BsonDouble MaximalTransactionValue)
            BsonElement("MaximalTradingValue", BsonDouble MaximalTradingValue)
            BsonElement("Email", BsonString Email)
        ])
        printfn "%A" document
        let result = MongoDBUtil.insertDocument "TradingStrategies" document
        OK (result.ToString())

    )


let webApp =
    choose [
        path "/" >=> OK "Welcome to Arbitrage Gainer "
        POST >=> choose [
            path "/strategies" >=> tradingStrategyRoute]
        
    ]

[<EntryPoint>]
let main argv =
    let config = { defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080 ] }
    startWebServer config webApp
    0 // Return an integer exit code
