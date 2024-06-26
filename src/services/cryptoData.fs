module CryptoData

open System
open System.Net.Http
open System.Threading.Tasks
open System.Text.Json

let client = HttpClient()

type TradingPair = {
    Altname: string
    WSname: string
}

let bitfinexUrl = "https://api-pub.bitfinex.com/v2/conf/pub:list:pair:exchange"
let getTradingPairsForbitfinex (apiUrl : string) =
    async {
        let! response = client.GetAsync(apiUrl :> string) |> Async.AwaitTask
        let! json = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let pairsArray = JsonSerializer.Deserialize<string[][]>(json)
        let filteredPairs =
            pairsArray
            |> Array.collect id // Flatten
            |> Array.filter (fun pair -> pair.EndsWith("USD") && pair.Length = 6) // Filter pairs
            |> Array.map (fun pair -> pair.Insert(3, "-")) // Format to "XXX-USD"
        return filteredPairs
    }
let bitstampUrl = "https://www.bitstamp.net/api/v2/ticker/"
let getTradingPairsForBitstamp (apiUrl : string) =
    async {
        let! response = client.GetAsync(apiUrl :> string) |> Async.AwaitTask
        let! json = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let data = JsonSerializer.Deserialize<JsonElement[]>(json)
        let filteredPairs =
            data
            |> Array.filter (fun pair ->
                let pairString = pair.GetProperty("pair").GetString()
                pairString.EndsWith("USD") && pairString.Length = 7)
            |> Array.map (fun pair ->
                let pairString = pair.GetProperty("pair").GetString()
                let currencyPair = sprintf "%s-%s" (pairString.Substring(0, 3)) "USD"
                currencyPair)
        return filteredPairs
    }
let krakenUrl = "https://api.kraken.com/0/public/AssetPairs"
let getTradingPairsForKraken (apiUrl : string) =
    async {
        let! response = client.GetAsync(apiUrl :> string) |> Async.AwaitTask
        let! json = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let pairsJson = JsonSerializer.Deserialize<JsonElement>(json).GetProperty("result")
        let pairs =
            [ for pair in pairsJson.EnumerateObject() do
                let wsname = pair.Value.GetProperty("wsname").GetString()
                if wsname <> null then
                    if wsname.EndsWith("USD") && wsname.Length = 7 then
                        let prefix = wsname.Substring(0, 3)
                        yield sprintf "%s-USD" prefix ]
        return pairs |> Array.ofList
    }

let findCommonPairs (pairs1 : string[]) (pairs2 : string[]) (pairs3 : string[]) =
    let set1 = Set.ofArray pairs1
    let set2 = Set.ofArray pairs2
    let set3 = Set.ofArray pairs3
    let commonPairs = Set.intersect set1 (Set.intersect set2 set3)
    commonPairs |> Set.toArray

let fetchCrossPairs =
    async {
        let! bitfinexPairs = getTradingPairsForbitfinex bitfinexUrl
        let! bitstampPairs = getTradingPairsForBitstamp bitstampUrl
        let! krakenPairs = getTradingPairsForKraken krakenUrl

        let commonPairs = findCommonPairs bitfinexPairs bitstampPairs krakenPairs
        return commonPairs
    }

(*
open Core.Domain
open Infra.Repositories
open System
open System.Net.Http
open Newtonsoft.Json.Linq

open MongoDB.Driver

open BitfinexAPI
open BitstampAPI
open KrakenAPI

module MongoDBUtil =

    let connectionString = "your_connection_string_here" // Replace with MongoDB Atlas connection string
    let databaseName = "cryptoDatabase" // database name
    let collectionName = "cryptocurrencies" // collection name

    let client = MongoClient(connectionString)
    let database = client.GetDatabase(databaseName)
    let collection = database.GetCollection<BsonDocument>(collectionName)

    let insertDocument (document: BsonDocument) =
        collection.InsertOne(document)


module CryptoExchangeData =

    type CurrencyPair = {
        Pair: string
        Exchange: string
    }

    let httpClient = new HttpClient()

    let fetchCurrencyPairs (url: string) =
        async {
            let! response = httpClient.GetAsync(url) |> Async.AwaitTask
            let! json = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            return JArray.Parse(json)
        }

    let parseCurrencyPairs (data: JArray) (exchange: string) =
        data |> Seq.choose (fun item ->
            let pair = item.Value<string>("pair")
            if pair.Length = 6 && pair.[3..].Length = 3 // Check if the pair is exactly 6 letters long
            then Some { Pair = pair; Exchange = exchange }
            else None
        )

    let exchanges = [
        "https://api.bitfinex.com/v1/symbols", "Bitfinex";
        "https://api.bitstamp.net/v1/symbols", "Bitstamp";
        "https://api.kraken.com/v1/symbols", "Kraken"
    ]

    let fetchAndParseAllPairs =
        exchanges
        |> List.map (fun (url, exchange) ->
            async {
                let! data = fetchCurrencyPairs url
                return parseCurrencyPairs data exchange
            })
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Array.concat

    let findCommonPairs pairs =
        pairs
        |> Seq.groupBy (fun pair -> pair.Pair)
        |> Seq.filter (fun (_, grouped) -> Seq.length grouped = exchanges.Length) // Filter pairs present in all exchanges
        |> Seq.map fst

    let fetchAllPairs = async {
    let! bitfinexResult = BitfinexAPI.fetchBitfinexPairs()
    let! bitstampResult = BitstampAPI.fetchBitstampPairs()
    let! krakenResult = KrakenAPI.fetchKrakenPairs()
    match (bitfinexResult, bitstampResult, krakenResult) with
    | (Result.Ok bitfinexPairs, Result.Ok bitstampPairs, Result.Ok krakenPairs) ->
        return Result.Ok (bitfinexPairs, bitstampPairs, krakenPairs)
    | _ ->
        return Result.Error "Failed to fetch pairs from one or more exchanges."
    }

    let findCommonPairsByHash (bitfinexPairs, bitstampPairs, krakenPairs) =
        let commonPairs = HashSet<_>(bitfinexPairs)
        commonPairs.IntersectWith(HashSet<_>(bitstampPairs))
        commonPairs.IntersectWith(HashSet<_>(krakenPairs))
        commonPairs

    let storePairsInMongo (pairs: seq<string>) =
        pairs |> Seq.iter (fun pair ->
            let document = BsonDocument()
            document.Add("pair", BsonString(pair))
            MongoDBUtil.insertDocument(document)
            printfn "Stored pair: %s" pair
        )

    let readAllPairs () =
        collection.Find(Builders<BsonDocument>.Filter.Empty).ToEnumerable()
        |> Seq.map (fun document -> document["pair"].AsString)
        |> Seq.toList
    
// Usage
let pairs = fetchAndParseAllPairs
let commonPairs = findCommonPairs pairs
storePairsInMongo commonPairs
fetchAllPairs
storePairsInMongo findCommonPairsByHash
*)