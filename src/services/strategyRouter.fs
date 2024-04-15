module strategyRouter

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Newtonsoft.Json

open RealTimeTrading
open IdentifyHistoricalArbitrageOpportunities
open UserSetting

type TradingParams = {
    Currencies: int
    MinimalPriceSpread: float
    MinimalTransactionProfit: float
    MaximalTransactionValue: float
    MaximalTradingValue: float
}

type TradingParams = {
    Currencies: int
    MinimalPriceSpread: float
    MinimalTransactionProfit: float
    MaximalTransactionValue: float
    MaximalTradingValue: float
}


let initTrading (params: TradingParams) : WebPart =
    bindJson<TradingParams> >>= fun params ->
    evaluateMarketData params
    OK $"Trading initialized with parameters}"


let stopTrading : WebPart =
    fun _ ->
    currentStrategy <- None
    OK "Trading stopped"


let updateTradingStrategy : WebPart =
    bindJson<TradingParams> >>= fun params ->
    currentStrategy <- Some params
    OK $"Trading strategy updated"

let identifyArbitrageOpportunities : WebPart =
    fun _ -> arbitrageOpportunities
    OK $"Arbitrage opportunities identified"

let tradedCurrencies : WebPart =
    //get traded currencies from the database
    fun _ -> gettradedCurrencies
    OK $"Trading strategy retrieved"

let updateUserEmail : WebPart =
    bindJson<emailaddress> >>= fun email ->
    updateUserEmail email
    OK $"User email updated"

let getCurrentTradingStrategy =
    match currentStrategy with
    | Some strategy -> strategy
    | None -> failwith "No active trading strategy"
