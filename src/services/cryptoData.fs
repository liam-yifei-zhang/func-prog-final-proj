// Todo:
// 1. Comply with test dataset:
// [{"ev":"XQ","pair":"CHZ-USD","lp":0,"ls":0,"bp":0.0771,"bs":41650.4,"ap":0.0773,"as":142883.4,"t":1690409119847,"x":1,"r":1690409119856},
// {"ev":"XQ","pair":"CHZ-USD","lp":0,"ls":0,"bp":0.0771,"bs":41650.4,"ap":0.0773,"as":135498.5,"t":1690409119848,"x":1,"r":1690409119856},
// {"ev":"XQ","pair":"KNC-USD","lp":0,"ls":0,"bp":0.72035,"bs":314,"ap":0.7216,"as":314,"t":1690409119855,"x":2,"r":1690409119855},
// {"ev":"XQ","pair":"KNC-USD","lp":0,"ls":0,"bp":0.72034,"bs":1449.10547227,"ap":0.7216,"as":314,"t":1690409119855,"x":2,"r":1690409119855},
// {"ev":"XQ","pair":"KNC-USD","lp":0,"ls":0,"bp":0.72034,"bs":1449.10547227,"ap":0.72152,"as":23.55018688,"t":1690409119855,"x":2,"r":1690409119855},
// {"ev":"XQ","pair":"CHZ-USD","lp":0,"ls":0,"bp":0.0771,"bs":41650.4,"ap":0.0773,"as":122904.2,"t":1690409119853,"x":1,"r":1690409119861},
// {"ev":"XQ","pair":"CHZ-USD","lp":0,"ls":0,"bp":0.0771,"bs":42952.1,"ap":0.0773,"as":122904.2,"t":1690409119859,"x":1,"r":1690409119865},
// {"ev":"XQ","pair":"CHZ-USD","lp":0,"ls":0,"bp":0.0771,"bs":42952.1,"ap":0.0773,"as":130809.1,"t":1690409119871,"x":1,"r":1690409119878},
// {"ev":"XQ","pair":"CHZ-USD","lp":0,"ls":0,"bp":0.0771,"bs":42952.1,"ap":0.0773,"as":131620.7,"t":1690409119873,"x":1,"r":1690409119880},
// {"ev":"XQ","pair":"YFI-USD","lp":0,"ls":0,"bp":6808.01,"bs":0.02820207,"ap":7146.91,"as":0.0281959,"t":1690409119928,"x":6,"r":1690409119975},
// {"ev":"XQ","pair":"CRV-USD","lp":0,"ls":0,"bp":0.73135,"bs":402.1454923,"ap":0.73393,"as":657.79817779,"t":1690409119928,"x":6,"r":1690409119977},
// {"ev":"XQ","pair":"FTM-USD","lp":0,"ls":0,"bp":0.24628,"bs":779.60045476,"ap":0.24679,"as":2431.21682401,"t":1690409119927,"x":6,"r":1690409119972},
// {"ev":"XQ","pair":"KNC-USD","lp":0,"ls":0,"bp":0.65407,"bs":545.59967546,"ap":0.72986,"as":1637.1026977,"t":1690409119942,"x":6,"r":1690409119994},
// {"ev":"XQ","pair":"ZRX-USD","lp":0,"ls":0,"bp":0.20861,"bs":249.84134499,"ap":0.22145,"as":519.22771747,"t":1690409119943,"x":6,"r":1690409119999},
// {"ev":"XQ","pair":"BAT-USD","lp":0,"ls":0,"bp":0.24908,"bs":938.9300632,"ap":0.19789,"as":2529.793957,"t":1690409119943,"x":23,"r":1690409120000},
// {"ev":"XQ","pair":"ADA-USD","lp":0,"ls":0,"bp":0.307124,"bs":468.27344443,"ap":0.307153,"as":4883.58514745,"t":1690409119965,"x":23,"r":1690409120003},
// {"ev":"XQ","pair":"ADA-USD","lp":0,"ls":0,"bp":0.307124,"bs":468.27344443,"ap":0.307143,"as":800,"t":1690409119965,"x":23,"r":1690409120004},
// {"ev":"XQ","pair":"SOL-USD","lp":0,"ls":0,"bp":25.31,"bs":457.36097881,"ap":25.32,"as":556.52434963,"t":1690409119965,"x":23,"r":1690409120004},
// {"ev":"XQ","pair":"KNC-USD","lp":0,"ls":0,"bp":0.72035,"bs":314,"ap":0.72152,"as":23.55018688,"t":1690409120010,"x":2,"r":1690409120010},
// {"ev":"XQ","pair":"MKR-USD","lp":0,"ls":0,"bp":1178.1,"bs":0.34,"ap":1178.78,"as":0.985017,"t":1690409120089,"x":1,"r":1690409120095}]

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
