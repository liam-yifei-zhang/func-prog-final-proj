module MongoDBUtil

    open MongoDB.Driver
    open MongoDB.Bson
    open System

    // let connectionString = "your_connection_string_here"
    // let databaseName = "cryptoDatabase"
    // let collectionName = "transactions"

    // let client = MongoClient(connectionString)
    // let database = client.GetDatabase(databaseName)
    // let collection = database.GetCollection<BsonDocument>(collectionName)

    // let insertDocument (document: BsonDocument) =
    //     try
    //         collection.InsertOne(document)
    //         true
    //     with
    //     | ex: Exception ->
    //         printfn "Error inserting document: %s" ex.Message
    //         false

    // let insertManyDocuments (documents: BsonDocument list) =
    //     try
    //         collection.InsertMany(documents)
    //         true
    //     with
    //     | ex: Exception ->
    //         printfn "Error inserting documents: %s" ex.Message
    //         false

        // Dummy implementations for MongoDB interactions

    let insertDocument (document: BsonDocument) =
        printfn "Mock Insert Document: %A" document
        true  // Assume always successful for mock

    let insertManyDocuments (documents: BsonDocument list) =
        printfn "Mock Insert Many Documents: %A" documents
        true  // Assume always successful for mock