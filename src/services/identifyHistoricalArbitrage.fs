module IdentifyHistoricalArbitrage

open CrossTradedCryptos
open Types
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Utils.Collections
open Suave.RequestErrors
open System
open Azure
open Newtonsoft.Json
open System.IO
open System.Reflection
open ServiceBus
open Types

type EntryData = {
    ev: string
    pair: string
    lp: float
    ls: float
    bp: float
    bs: float
    ap: float
    as: float
    t: int64
    x: int
    r: int64
}

type Quote = {
    CurrencyPair: CurrencyPair
    Exchange: string
    BidPrice: float
    AskPrice: float
    BidSize: float
    AskSize: float
    Time: int64
}

type ArbitrageOpportunity = {
    Currency1: string
    Currency2: string
    NumberOfOpportunitiesIdentified: int
}

type ArbitrageOpportunitiesIdentified = ArbitrageOpportunity list

type BucketedQuotes = {
    Quotes: list<Quote>
}

type CurrencyPair = {
    Currency1: string
    Currency2: string
}

let currencyPairFromStr (pairStr: string) : CurrencyPair =
    {
        Currency1 = pairStr.[0..2]
        Currency2 = pairStr.[4..6]
    }

let getCrossTradedPairs (data: EntryData list) =
    data
    |> List.groupBy (fun entry -> entry.pair)
    |> List.map (fun (pair, entries) -> (pair, entries |> List.map (fun entry -> entry.x) |> Set.ofList |> Set.count))
    |> List.filter (fun (_, count) -> count > 1)
    |> List.map fst

let getExchangeFromUnprocessedQuote (quote: EntryData) =
    match quote.x with
    | 1 -> "Coinbase"
    | 2 -> "Bitfinex"
    | 6 -> "Bitstamp"
    | 23 -> "Kraken"
    | _ -> "Unknown"
    

let processQuotes (unprocessedQuotes: EntryData seq) = 
    let multiExchangePairs = getCrossTradedPairs(unprocessedQuotes |> List.ofSeq) |> Set.ofSeq
    unprocessedQuotes 
    |> Seq.map (fun quote ->
        {
            CurrencyPair = currencyPairFromStr quote.pair
            Exchange = getExchangeFromUnprocessedQuote quote
            BidPrice = quote.bp
            AskPrice = quote.ap                          
            BidSize = quote.bs
            AskSize = quote.`as`
            Time = quote.t
        })
    |> Seq.filter (fun q -> Set.contains q.CurrencyPair.Currency1 q.CurrencyPair.Currency2 multiExchangePairs)

let toBucketKey (timestamp: int64) =
    let bucketSizeMs = 5L
    let unixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
    let time = unixEpoch.AddMilliseconds(float timestamp)
    let timeSinceEpoch = (time.ToUniversalTime() - unixEpoch).TotalMilliseconds
    timeSinceEpoch / (float bucketSizeMs) |> int64

/// Regroups quotes into buckets of 5 milliseconds
let regroupQuotesIntoBuckets (quotes: Quote seq) = 
    quotes 
    |> Seq.groupBy (fun quote -> (quote.CurrencyPair, toBucketKey quote.Time))


/// Selects the quote with the highest bid price for each exchange
let selectHighestBidPerExchange (quotes: Quote seq) : Quote list =
    quotes
    |> Seq.groupBy (fun quote -> quote.Exchange)
    |> Seq.collect (fun (_, quotesByExchange) ->
        quotesByExchange
        |> Seq.tryMaxBy (fun quote -> quote.BidPrice)
        |> Seq.choose id)
    |> Seq.toList

/// Identifies arbitrage opportunities within a list of quotes
let identifyArbitrageOpportunities (selectedQuotes: Quote list) (quotes: Quote list) : list<ArbitrageOpportunity> =
    let combinations = List.allPairs selectedQuotes quotes
    combinations
    |> List.choose (fun (quote1, quote2) ->
        match (quote1.CurrencyPair = quote2.CurrencyPair, quote1.Exchange <> quote2.Exchange) with
        | (true, true) ->
            let priceDifference = quote1.BidPrice - quote2.AskPrice
            match priceDifference > 0.01 with
            | true -> Some { Currency1 = quote1.CurrencyPair.Currency1; Currency2 = quote1.CurrencyPair.Currency2; NumberOfOpportunitiesIdentified = 1 }
            | false -> None
        | _ -> None)

let writeResultToFile (opportunities: ArbitrageOpportunity list) = 
    use writer = new StreamWriter("historicalArbitrageOpportunities.txt", false)
    opportunities 
    |> List.iter (fun op -> 
        let text = sprintf "%s-%s %d" op.Currency1 op.Currency2 op.NumberOfOpportunitiesIdentified
        writer.WriteLine text
    )


// Workflow
let calculateHistoricalSpreadWorkflow () : ArbitrageOpportunitiesIdentified =
    try
        let json = System.IO.File.ReadAllText("historicalData.txt")
        match JsonConvert.DeserializeObject<UnprocessedQuote seq>(json) with
        | null -> failwith "Failed to deserialize the quotes."
        | unprocessedQuotes ->
            let buckets = 
                unprocessedQuotes 
                |> processQuotes
                |> regroupQuotesIntoBuckets 
            let opportunities =
                buckets
                |> Seq.toList
                |> List.collect (fun (_, quotes) ->
                    let selectedQuotes = selectHighestBidPerExchange quotes
                    identifyArbitrageOpportunities selectedQuotes (Seq.toList quotes)
                )
                |> List.groupBy (fun op -> (op.Currency1, op.Currency2))
                |> List.map (fun ((currency1, currency2), ops) -> 
                    let num = List.sumBy (fun op -> op.NumberOfOpportunitiesIdentified) ops 
                    { Currency1 = currency1; Currency2 = currency2; NumberOfOpportunitiesIdentified = num }
                )
            writeResultToFile opportunities
            opportunities
    with
    | ex -> 
        printfn "An error occurred: %s" ex.Message
        []


