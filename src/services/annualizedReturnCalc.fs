// Todo:
// 1. Move the calculateAnnualizedReturn function to core folder
// 2. Verify the code against the privided write up
// 3. Add error handling (domain error)

module AnnualizedReturnCalculator =
    open System
    open core.calcAnnualizedReturn

    // Assuming the existence of a function to retrieve data from the database
    // This function should be implemented to fetch the initial investment and total trading period
    let fetchInvestmentDataFromDb () =
        // Placeholder: Implement database retrieval logic here
        async {
            // Simulated database response
            let initialInvestment = 1000m // Example value
            let totalTradingPeriodYears = 5m // Example value
            return (initialInvestment, totalTradingPeriodYears, 1500m) // Example final value
        }

    let calculateAnnualizedReturn (initialInvestment: decimal) (finalValue: decimal) (years: decimal) : decimal =
        let growthRate = finalValue / initialInvestment
        let annualizedReturn = (growthRate ** (1m / years)) - 1m
        annualizedReturn

    let emitAnnualizedReturnCalculatedEvent (annualizedReturn: decimal) =
        // Placeholder: Implement event emission logic here
        printfn "Annualized Return Calculated: %A" annualizedReturn

    let calculateAnnualizedReturnWorkflow () =
        async {
            let! (initialInvestment, totalTradingPeriodYears, finalValue) = fetchInvestmentDataFromDb ()
            let annualizedReturn = calculateAnnualizedReturn initialInvestment finalValue totalTradingPeriodYears
            emitAnnualizedReturnCalculatedEvent annualizedReturn
        }


    
