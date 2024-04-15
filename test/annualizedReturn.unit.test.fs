module AnnualizedReturnCalculatorTests

open NUnit.Framework
open Core.Domain
open System

// Tests for calculateInvestmentDuration
[<Test>]
[<TestCase("2023-01-01", "2023-01-01", 0.0)>] 
[<TestCase("2023-01-01", "2023-12-31", 0.9973)>]
let ``Given period should calculate correct investment duration in years with edge cases`` (startDateStr: string, endDateStr: string, expectedYears: decimal) =
    let startDate = DateTime.Parse(startDateStr)
    let endDate = DateTime.Parse(endDateStr)
    let period = { StartDate = startDate; EndDate = endDate }
    
    let actualYears = calculateInvestmentDuration period

    let delta = 0.001m 
    Assert.AreEqual(expectedYears, actualYears, delta, "Investment duration should be calculated correctly")
    Assert.IsTrue(actualYears >= 0m) 

// Tests for calculateInitialInvestment 
[<Test>]
[<TestCase(0m, 0m, 0m)>] 
[<TestCase(-1000m, -200m, -800m)>] 
let ``Given cash flows should handle edge cases correctly in initial investment calculation`` (inflow: decimal, outflow: decimal, expectedInitialInvestment: decimal) =
    let cashFlows = { Inflow = inflow; Outflow = outflow }
    
    let actualInitialInvestment = calculateInitialInvestment cashFlows

    Assert.AreEqual(expectedInitialInvestment, actualInitialInvestment)
    Assert.IsTrue(inflow >= outflow ==> actualInitialInvestment >= 0m, "Initial investment should be non-negative")

// Tests for calculateAnnualizedReturn 
[<Test>]
[<TestCase(1000m, 1100m, 1.0, 0.1)>] // 10% growth over a year
[<TestCase(1000m, 1000m, 0.5, 0.0)>] // No growth, half a year
let ``Given investment data should calculate correct annualized return`` (initialInvestment: decimal, finalValue: decimal, years: decimal, expectedAnnualizedReturn: decimal) =
    let actualAnnualizedReturn = calculateAnnualizedReturn initialInvestment finalValue years

    let delta = 0.001m 
    Assert.AreEqual(expectedAnnualizedReturn, actualAnnualizedReturn, delta, "The calculated annualized return does not match the expected value.")
    Assert.IsTrue(actualAnnualizedReturn >= 0m, "Annualized return should be non-negative")
