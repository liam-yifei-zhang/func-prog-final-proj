module LoadHistoricalData
open System
open System.IO
open Newtonsoft.Json
open Core.Domain
open inf.Db
open Core.ArbitrageStrategies

//in domain
(*
type EventQuote = {
    ev: string  //The event type.
    pair: CurrencyPair //The crypto pair.
    lp: int
    ls: int
    bp: float   //The bid price.
    bs: float   //The bid size.
    ap: float   //The ask price.
    as: float   //The ask size.
    t: int  //The Timestamp in Unix MS.
    x: int  //The crypto exchange ID
    r: int  //The timestamp that the tick was received by Polygon.
}
*)


//in domain
(*
type HistoricalQuote = {
    Exchange: string
    CurrencyPair: string
    Bid: float
    Ask: float
    Timestamp: System.DateTime
}
*)

//read data adn filter out the data
let filepath = " "

let readFileContents (filePath: string) : string =
    File.ReadAllText(filePath)

let parseJsonToHistoricalQuotes (json: string) : HistoricalQuote list =
    let eventQuotes = JsonConvert.DeserializeObject<EventQuote[]>(json)
    Array.map (fun eq ->
        {
            Exchange = eq.x.ToString()
            CurrencyPair = eq.pair
            Bid = eq.bp
            Ask = eq.ap
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(eq.t).DateTime
        }
    ) eventQuotes
    |> Array.toList

let historicalQuotes = loadAndParseFile filepath
let finalList = parseJsonToHistoricalQuotes historicalQuotes

//apply the arbitrage strategy
let arbitrageOpportunities = ArbitrageStrategies.IdentifyHistoricalArbitrageOpportunities finalList

//stroe to Db

//todo ..
