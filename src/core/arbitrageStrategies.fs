namespace Core.Strategy
module ArbitrageStrategies
open Core.Domain

//assume the input is a list of historical quotes
(*
type HistoricalQuote = {
    Exchange: string
    CurrencyPair: string CHZ-USD
    Bid: float
    Ask: float
    Timestamp: System.DateTime
}
*)


let findArbitrageOpportunities quotesByPair = //Mapping Phase
    quotesByPair
    |> List.collect (fun (currencyPair, quotes) ->
        let exchangeBestBids = 
            quotes
            |> List.groupBy (fun q -> q.Exchange)
            |> List.filter (fun (_, exchangequotes) -> List.length exchangequotes > 1) //filter out the quotes from exchanges that have only one exchange
            |> List.map (fun (exchange, exchangeQuotes) ->
                exchangeQuotes |> List.maxBy (fun q -> q.Bid) |> fun quote -> (exchange, quote))
          
        exchangeBestBids
        |> List.collect (fun (bestBidExchange, bestBidQuote) ->
            quotes
            |> List.filter (fun quote -> quote.Exchange <> bestBidExchange && quote.CurrencyPair = bestBidQuote.CurrencyPair)
            |> List.choose (fun quote ->
                match bestBidQuote.Bid - quote.Ask > 0.01m with
                | true -> 
                    Some {
                        BuyExchange = quote.Exchange
                        SellExchange = bestBidExchange
                        CurrencyPair = bestBidQuote.CurrencyPair
                        BuyPrice = quote.Ask
                        SellPrice = bestBidQuote.Bid
                        Timestamp = bestBidQuote.Timestamp
                    }
                | false -> None
            )
        )
    )

let summarizeOpportunities opportunities = //Reducing Phase
    opportunities
    |> List.groupBy (fun op -> op.CurrencyPair)
    |> List.map (fun (currencyPair, ops) -> sprintf "%s; %d" currencyPair (List.length ops))

let IdentifyHistoricalArbitrageOpportunities (quotes: HistoricalQuote list) =       
//Main Function: Utilizing the above two functions.
    let groupedQuotes = 
        quotes
        |> List.groupBy (fun q -> q.Timestamp % 5)
        |> List.collect (fun (_, quotesInBucket) ->
            quotesInBucket
            |> List.groupBy (fun q -> q.CurrencyPair))
    let opportunities = groupedQuotes |> List.collect findArbitrageOpportunities
    summarizeOpportunities opportunities

    
