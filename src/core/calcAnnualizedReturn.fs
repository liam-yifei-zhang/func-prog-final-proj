open System

let calculateInvestmentDuration (period: InvestmentPeriod) : decimal =
        let totalDays = (period.EndDate - period.StartDate).TotalDays
        totalDays / 365m

let calculateInitialInvestment (cashFlows: CashFlow) : decimal =
    cashFlows.Inflow - cashFlows.Outflow

let calculateAnnualizedReturn (initialInvestment: decimal) (finalValue: decimal) (years: decimal) : decimal =
    match years > 0m with
    | true ->
        let growthRate = finalValue / initialInvestment
        (growthRate ** (1m / years)) - 1m
    | false -> 0m