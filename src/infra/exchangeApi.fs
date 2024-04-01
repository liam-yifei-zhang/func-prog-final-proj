namespace ExchangeApi

module ExchangeApi

// Future milestones

let private apiUrl = "https://api.exchange.com"

let getExchangeRate (currencyPair: string) : float =
    // Mock implementation for fetching exchange rate
    // Replace this with actual API call to fetch exchange rate
    printfn "Fetching exchange rate for currency pair: %s" currencyPair
    // Assuming a fixed exchange rate for demonstration purposes
    match currencyPair with
    | "BTCUSD" -> 60000.0 // Example rate for BTCUSD
    | "ETHUSD" -> 2000.0  // Example rate for ETHUSD
    | _ -> 0.0 // Default rate if currency pair is not found