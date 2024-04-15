module MongoDBUtil =

    open MongoDB.Driver
    open MongoDB.Bson
    open System

    let connectionString = "your_connection_string_here" // Replace with MongoDB Atlas connection string
    let databaseName = "cryptoDatabase"
    let collectionName = "transactions"

    let client = MongoClient(connectionString)
    let database = client.GetDatabase(databaseName)
    let collection = database.GetCollection<BsonDocument>(collectionName)



module HistoricalPnLCalculator =
    open core.calcPnL
    open MongoDBUtil

    let fetchHistoricalTransactionsFromDb (startDate: DateTime, endDate: DateTime) =
        // Create filters to query documents within the date range
        let filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Gte("TransactionDate", startDate),
            Builders<BsonDocument>.Filter.Lte("TransactionDate", endDate)
        )

        // Find documents based on the filter
        let results = collection.Find(filter).ToList()

        // Convert results to a list of TransactionRecord
        results
        |> List.map (fun doc ->
            {
                TransactionType = doc["TransactionType"].AsString;
                Quantity = doc["Quantity"].ToDecimal();
                Price = doc["Price"].ToDecimal();
                CurrencyPair = doc["CurrencyPair"].AsString;
                TransactionDate = doc["TransactionDate"].ToUniversalTime(); // Convert BSON date to DateTime
            })

   let calculateHistoricalPnL (startDate: DateTime, endDate: DateTime, event: UserRequestsPnLCalculation) : decimal =
    match event with
        | UserInvokesPnLCalculation (startDate, endDate) ->
            let transactions = fetchHistoricalTransactionsFromDb startDate endDate
            let pnl = calculatePnL transactions
            pnl
        | _ -> printfn "Unsupported event"
