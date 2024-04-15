module StrategyRouter

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Newtonsoft.Json

open RealTimeTrading
open IdentifyHistoricalArbitrageOpportunities
open UserSetting
open CryptoData

open Services.RealTimeTrading 

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

    let paramsToBson (params: TradingParams) : BsonDocument =
        BsonDocument([
            ("Currencies", BsonInt32(params.Currencies));
            ("MinimalPriceSpread", BsonDecimal128(params.MinimalPriceSpread));
            ("MinimalTransactionProfit", BsonDecimal128(params.MinimalTransactionProfit));
            ("MaximalTransactionValue", BsonDecimal128(params.MaximalTransactionValue));
            ("MaximalTradingValue", BsonDecimal128(params.MaximalTradingValue))
        ])

type TradingParams = {
    Currencies: int
    MinimalPriceSpread: float
    MinimalTransactionProfit: float
    MaximalTransactionValue: float
    MaximalTradingValue: float
}

let uri = Uri("wss://socket.polygon.io/crypto")
let apiKey = "phN6Q_809zxfkeZesjta_phpgQCMB2Dw"
let subscriptionParameters = "XT.BTC-USD"

let initTrading : WebPart =
    bindJson<TradingParams> (fun params ->
        Async.Start (startTrading (uri, apiKey, subscriptionParameters))
        OK $"Trading initialized with parameters: {params}")

let stopTrading : WebPart =
    fun _ ->
        Async.Start (stopTrading ())
        OK "Trading stopped"

let updateTradingStrategy : WebPart =
    bindJson<TradingParams> (fun params ->
        let doc = MongoDBUtil.paramsToBson(params)
        MongoDBUtil.insertDocument(doc)
        Async.Start (stopTrading ())
        Async.Sleep 1000  // Wait for a bit to ensure the trading stops
        Async.Start (startTrading (uri, apiKey, subscriptionParameters))
        OK $"Trading strategy updated with parameters: {params}")

let identifyArbitrageOpportunities : WebPart =
    fun _ ->
        let opportunities = arbitrageOpportunities ()
        OK $"Arbitrage opportunities identified: {opportunities}"

let crossCurrencies : WebPart =
    fun _ ->
        let pairs = readAllPairs ()
        OK $"Traded currencies: {pairs}"

let updateUserEmail : WebPart =
    bindJson<string> (fun email ->
        UserSetting.updateUserEmail email
        OK $"User email updated to: {email}")

let getCurrentTradingStrategy =
    match currentStrategy with
    | Some strategy -> strategy
    | None -> failwith "No active trading strategy"
