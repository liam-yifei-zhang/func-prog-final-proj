module annualizedReturnUnitTest

open NUnit.Framework
open Services.AnnualizedReturnCalculator
open Core.Domain
open System

[<TestCase(1000m, 1500m, 3m, 0.1459)>] 
[<TestCase(2000m, 3000m, 4m, 0.1060)>] 
let ``Annualized Return Calculation Workflow - Multiple Tests`` 
    (initialInvestment: decimal) (finalValue: decimal) (years: decimal) (expectedAnnualizedReturn: decimal) =
    
    // Calculate the actual annualized return
    let actualAnnualizedReturn = calculateAnnualizedReturn initialInvestment finalValue years

    // Assert that the actual annualized return matches the expected outcome
    Assert.AreEqual(expectedAnnualizedReturn, actualAnnualizedReturn, $"The annualized return calculated for an initial investment of {initialInvestment}, final value of {finalValue}, over {years} years did not match the expected outcome.")

    // Assert that the actual annualized return is not negative
    Assert.IsTrue(actualAnnualizedReturn >= 0m, "The calculated annualized return should not be negative.")