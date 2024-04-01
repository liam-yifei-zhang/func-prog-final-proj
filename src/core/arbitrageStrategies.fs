namespace Core.Strategy
module ArbitrageStrategies
open Core.Domain

// The namespace Core.Strategy groups together functionality related to trading strategies within the core domain of the application.
// It provides a logical organization for code that deals with the implementation and execution of various trading strategies.

// The module ArbitrageStrategies within the Core.Strategy namespace specifically focuses on strategies related to arbitrage trading.
// Arbitrage trading involves taking advantage of price differences in different markets or exchanges to make a profit.

// The open Core.Domain statement makes the types and functions defined in the Core.Domain module available in this module.
// This allows the ArbitrageStrategies module to use domain-specific types such as HistoricalQuote without fully qualifying their names.


// ToDo: Add function to compute spread for real-time trading

// Function to calculate the spread of a given pair between two crypto exchanges
// Spread between two exchanges
let calculateSpread (quote1: HistoricalQuote) (quote2: HistoricalQuote) : float =
    match quote1, quote2 with
    | { Bid = bid1; Ask = ask1 }, { Bid = bid2; Ask = ask2 } ->
        (ask1 - bid2) + (ask2 - bid1) 

// Function to identify if there is an arbitrage opportunity
// Arbitrage opportunity exists if spread is positive
let identifyArbitrageOpportunity (spread: float) : bool =
    spread > 0.0 

let IdentifyHistoricalArbitrageOpportunities (quotes: HistoricalQuote list) =

    // Define the bucket size
    let bucketSize = TimeSpan.FromMilliseconds(5.0)
    //calculate the start time of the bucket
    let startTime = quotes |> List.minBy (fun q -> q.Timestamp) |> fun q -> q.Timestamp

    quotes
    |> List.groupBy (fun q -> //group by the bucket
        let elapsed = q.Timestamp - startTimestamp
        elapsed.Ticks / bucketSize.Ticks)
    |> List.collect (fun (bucket, qs) ->
        // In each bucket, for quotes from more than one exchange:
        let groupedByExchange = qs |> List.groupBy (fun q -> q.Exchange)
                match groupedByExchange with
                | _ when groupedByExchange.Length > 1 -> 
                    groupedByExchange
                    |> List.map (fun (exchange, eqs) ->
                        eqs |> List.maxBy (fun q -> q.Bid),
                        eqs |> List.minBy (fun q -> q.Ask))
                    |> List.collect (fun (bidQuote, askQuote) ->
                        match bidQuote, askQuote with
                        | bid, ask when bid.CurrencyPair = ask.CurrencyPair && (ask.Ask - bid.Bid) > 0.01f ->
                            [{ CurrencyPair = bid.CurrencyPair; NumberOfOpportunities = 1 }]
                        | _ -> [])
                // If there is only one exchange in the bucket
                | _ -> [])
    |> List.groupBy (fun opp -> opp.CurrencyPair)
    |> List.map (fun (currencyPair, opportunities) ->
        { CurrencyPair = currencyPair; NumberOfOpportunities = List.length opportunities })
    
