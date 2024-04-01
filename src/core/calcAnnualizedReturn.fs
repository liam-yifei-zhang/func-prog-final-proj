open System

// Todo: verify this function is correct. This is auto-generated.

let calculateAnnualizedReturn (initialInvestment: decimal) (finalValue: decimal) (years: double) : decimal =
    let growthRate = finalValue / initialInvestment
    let annualizedReturn = (growthRate ** (1.0 / years)) - 1.0m
    annualizedReturn
