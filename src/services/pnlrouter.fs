module pnlrouter

open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Newtonsoft.Json

open pnlcal
open historicalPnlCalc

let setPnLThreshold : WebPart =
    bindJson<PnLThreshold> >>= fun params ->
    // Set the PnL threshold to database
    OK $"PnL threshold set to {params.PnLThreshold}"

let launchPnLCalculation : WebPart = 
    fun _ -> 
    workflowPnLCalculation
    OK $"PnL calculated: {pnl}"

let launchAnnualizedReturnMetricCalculation : WebPart =
    fun _ -> 
    calculateAnnualizedReturnWorkflow
    OK $"Annualized return metric calculated: {annualizedReturnMetric}"
