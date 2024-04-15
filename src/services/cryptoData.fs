namespace Services

open Core.Domain
open Infra.Repositories
open System
open System.Net.Http
open Newtonsoft.Json.Linq

open MongoDB.Driver

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

    let storePairsInMongo (pairs: seq<string>) =
        pairs |> Seq.iter (fun pair ->
            let document = BsonDocument()
            document.Add("pair", BsonString(pair))
            MongoDBUtil.insertDocument(document)
            printfn "Stored pair: %s" pair
        )

// Usage
let pairs = fetchAndParseAllPairs
let commonPairs = findCommonPairs pairs
storePairsInMongo commonPairs
