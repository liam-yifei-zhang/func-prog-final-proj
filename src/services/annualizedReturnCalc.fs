module AnnualizedReturnCalculator =
    open System
    open Core.CalcAnnualizedReturn

    type CashFlow = {
        Inflow: decimal
        Outflow: decimal
    }

    type InvestmentPeriod = {
        StartDate: DateTime
        EndDate: DateTime
    }

    type InvestmentData = {
        Period: InvestmentPeriod
        DayOneCashFlows: CashFlow
        FinalValue: decimal
    }

    type InvokeAnnualizedReturnCalculation = unit  // Placeholder for user invocation details

    type DomainError = 
        | InvalidInvestmentData
        | InvalidDuration
        | InvalidFinalValue

    let fetchInvestmentDataFromDb (): Async<InvestmentData> =
        async {
            // let startDate = DateTime(2023, 1, 1)
            // let endDate = DateTime(2023, 6, 30)
            // let dayOneCashFlows = { Inflow = 1000m; Outflow = 200m }
            // let finalValue = 1500m
            return { Period = { StartDate = startDate; EndDate = endDate }; DayOneCashFlows = dayOneCashFlows; FinalValue = finalValue }
        }


    let emitAnnualizedReturnCalculatedEvent (annualizedReturn: decimal) =
        printfn "Annualized Return Calculated: %A" annualizedReturn

    let calculateAnnualizedReturnWorkflow (invokeData: InvokeAnnualizedReturnCalculation) =
        async {
            let! investmentData = fetchInvestmentDataFromDb ()
            let durationYears = calculateInvestmentDuration investmentData.Period
            let initialInvestment = calculateInitialInvestment investmentData.DayOneCashFlows
            let finalValue = investmentData.FinalValue

            match initialInvestment > 0m && durationYears > 0m && finalValue >= initialInvestment with
            | true ->
                let annualizedReturn = calculateAnnualizedReturn initialInvestment finalValue durationYears
                return emitAnnualizedReturnCalculatedEvent annualizedReturn
            | false ->
                let domainError = 
                    match initialInvestment, durationYears, finalValue with
                    | _, _, _ when initialInvestment <= 0m || finalValue < initialInvestment -> InvalidInvestmentData
                    | _, duration, _ when duration <= 0m -> InvalidDuration
                    | _, _, final when final < 0m -> InvalidFinalValue
                    | _ -> InvalidInvestmentData
                return domainError
        }
