module MongoDBUtil =

    open MongoDB.Driver
    open MongoDB.Bson
    open System

    let connectionString = "your_actual_connection_string_here"  // Update this with the actual MongoDB Atlas connection string
    let databaseName = "cryptoDatabase"
    let collectionName = "transactions"

    let client = MongoClient(connectionString)
    let database = client.GetDatabase(databaseName)
    let collection = database.GetCollection<BsonDocument>(collectionName)

module AnnualizedReturnCalculator =
    open System
    open MongoDBUtil
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

    let fetchInvestmentDataFromDb (): Async<InvestmentData> =
        async {
            let filter = Builders<BsonDocument>.Filter.Gte("date", DateTime(2023, 1, 1)) &
                         Builders<BsonDocument>.Filter.Lte("date", DateTime(2023, 12, 31))
            let sort = Builders<BsonDocument>.Sort.Ascending("date")
            let options = FindOptions<BsonDocument, BsonDocument>(Sort = sort)
            let! docs = collection.Find(filter, options).ToListAsync() |> Async.AwaitTask

            match docs with
            | [] -> failwith "No investment data found for the specified period"
            | _ ->
                let startDate = docs.Head["date"].ToUniversalTime()
                let endDate = docs.Last["date"].ToUniversalTime()
                let dayOneCashFlows = { Inflow = docs.Head["inflow"].AsDecimal; Outflow = docs.Head["outflow"].AsDecimal }
                let finalValue = docs.Last["finalValue"].AsDecimal

                return { Period = { StartDate = startDate; EndDate = endDate }; DayOneCashFlows = dayOneCashFlows; FinalValue = finalValue }
        }

    let calculateAnnualizedReturnWorkflow (invokeData: InvokeAnnualizedReturnCalculation) =
        async {
            let! investmentData = fetchInvestmentDataFromDb ()
            let durationYears = calculateInvestmentDuration investmentData.Period
            let initialInvestment = calculateInitialInvestment investmentData.DayOneCashFlows
            let finalValue = investmentData.FinalValue

            match initialInvestment > 0m && durationYears > 0m && finalValue >= initialInvestment with
            | true ->
                let annualizedReturn = calculateAnnualizedReturn initialInvestment finalValue durationYears
                printfn "Annualized Return Calculated: %A" annualizedReturn
                annualizedReturn |> Async.Return
            | false ->
                failwith "Invalid investment data for annualized return calculation"
        }
