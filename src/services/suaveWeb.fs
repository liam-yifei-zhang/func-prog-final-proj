namespace SuaveAPI.TradingService
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Suave
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open System

open strategyRouter
open pnlrouter

[<AutoOpen>]
module TradingService =
    open Suave.Filters

    // Define a record type for trading parameters
    type TradingStrategy = {
        Currencys: int
        MinimalPriceSpread: float
        MinimalTransactionProfit: float
        MaximalTransactionValue: float
        MaximalTradingValue: float
    }

    type emailaddress = {
        email: string
    }

    // function to update user email
    let updateUserEmail : WebPart =
        POST >=> path "/email" >=> updateUserEmail
    
    // Function to initialize trading with given parameters
    let initTrading : WebPart =
        POST >=> path "/tradeInit" >=> initTrading

    // Function to stop trading
    let stopTrading : WebPart =
        POST >=> path "/tradeStop" >=> stopTrading

    // Function to change trading strategy
    let changeTradingStrategy : WebPart =
        POST >=> path "/stratrgy" >=> changeTradingStrategy

    // Function to retrieve a list of traded currency pairs
    let getCrossTradedCurrencies : WebPart =
        GET >=> path "/currencies" >=> corssCurrencies
    
    // Funtion to identify historical arbitrage opportunities
    let identifyArbitrageOpportunities : WebPart =
        POST >=> path "/arbitrage" >=> identifyArbitrageOpportunities
    
    // Function to set P&L threshold
    let setPnLThreshold : WebPart =
        POST >=> path "/PnLThreshold" >=> setPnLThreshold
    
    // Function to launch P&L calculation
    let launchPnLCalculation : WebPart =
        POST >=> path "/PnLCalculation" >=> launchPnLCalculation
    
    // Function to launch Annualized Return Metric calculation
    let launchAnnualizedReturnMetricCalculation : WebPart =
        POST >=> path "/AnnualizedReturnMetric" >=> launchAnnualizedReturnMetricCalculation

    // Combined API
    let api : WebPart =
        choose 
            [ 
                GET >=> choose
                    [ path "/currencies" >=> tradedCurrencies

                ]
                POST >=> choose
                    [ path "/tradeInit" >=> initTrading
                    path "/tradeStop" >=> stopTrading
                    path "/strategy" >=> changeTradingStrategy
                    path "/PnLThreshold" >=> setPnLThreshold
                    path "/PnLCalculation" >=> launchPnLCalculation
                    path "/AnnualizedReturnMetric" >=> launchAnnualizedReturnMetricCalculation
                    path "/arbitrage" >=> identifyArbitrageOpportunities
                    path "/email" >=> updateUserEmail
                ]
            ]
        

namespace SuaveAPI
open Suave.Web
open SuaveAPI.TradingService

module Program =
    open SuaveAPI.TradingService

    [<EntryPoint>]
    let main argv =
        startWebServer defaultConfig TradingService.api
        0
