// Todo:
// 1. Add the workflows for services. The current code is auto-generated.
// 2. Add a function that stops the main trading process (user invocation, transaction volume > maxTrasactionVolume in user setting)

open Services
open Core.Domain
open Infra.Db

let main () =
    // Load historical values file event
    let historicalValuesFileEvent = { FileName = "historical_values.csv" }

    // Process arbitrage opportunities
    ProcessArbitrageOpportunities.process historicalValuesFileEvent

    // Identify and store crypto data
    let cryptoDataString = "crypto data string"
    match IdentifyAndStoreCryptoData.identifyCryptoData cryptoDataString with
    | Some cryptoData -> IdentifyAndStoreCryptoData.storeCryptoData cryptoData
    | None -> printfn "No crypto data identified"

    // Update user settings
    let userSettingsEvent = {
        TradingParameter = {
            NumberOfCryptoCurrencies = 5
            MinimalPriceSpread = 0.01m
            MinimalProfit = 0.02m
            MaximalTransactionValue = 10000m
            MaximalTradingValue = 50000m
        }
        UserEmail = "user@example.com"
        MaximalTradingValue = 60000m
    }
    let updatedSettings = UpdateUserSettings.updateUserSettings userSettingsEvent
    printfn "User settings updated for %s" updatedSettings.UserEmailSet

main ()

// let main () =
//     // Load historical quotes
//     match loadHistoricalQuotes "quotes.txt" with
//     | Ok historicalQuotes ->
//         // Calculate spread for historical quotes
//         match calculateSpread historicalQuotes with
//         | Ok spreadValues ->
//             // Identify arbitrage opportunities
//             match identifyArbitrageOpportunities historicalQuotes with
//             | Ok opportunities ->
//                 // Subscribe to market data feed
//                 match subscribeToMarketDataFeed ["BTC"; "ETH"; "XRP"] ["Kraken"; "Bitstamp"; "Bitfinex"] with
//                 | Ok _ ->
//                     // Process real-time data
//                     match processRealTimeData ["BTC"; "ETH"; "XRP"] ["Kraken"; "Bitstamp"; "Bitfinex"] with
//                     | Ok _ ->
//                         // Print success message
//                         printfn "Application executed successfully"
//                     | Error err -> 
//                         printfn "Error processing real-time data: %s" (match err with | DomainError(msg) | InputError(msg) -> msg)
//                 | Error err -> 
//                     printfn "Error subscribing to market data feed: %s" (match err with | DomainError(msg) | InputError(msg) -> msg)
//             | Error err -> 
//                 printfn "Error identifying arbitrage opportunities: %s" (match err with | DomainError(msg) | InputError(msg) -> msg)
//         | Error err -> 
//             printfn "Error calculating spread: %s" (match err with | DomainError(msg) | InputError(msg) -> msg)
//     | Error err -> 
//         printfn "Error loading historical quotes: %s" (match err with | DomainError(msg) | InputError(msg) -> msg)

// // Run the main function
// main()
