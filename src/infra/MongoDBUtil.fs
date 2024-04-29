module MongoDBUtil

open MongoDB.Driver
open MongoDB.Bson
open System
open System.Collections.Generic


let connectionString = "mongodb+srv://shenganw:6KUC4TDE9PAKQGvL@cluster0.pjwcuvc.mongodb.net/"
let databaseName = "cryptoDatabase"

let client = MongoClient(connectionString)
let database = client.GetDatabase(databaseName)

type InsertOneResult =
    | Inserted
    | Failed of string

let insertDocument (collectionName: string) (document: BsonDocument) =
    let collection = database.GetCollection<BsonDocument>(collectionName)
    let result = collection.InsertOne(document)
    // Hypothetical method result
    result

let insertManyDocuments (collectionName: string) (documents: BsonDocument list) =
    let collection = database.GetCollection<BsonDocument>(collectionName)
    let result = collection.InsertMany(documents)
    // Hypothetical method result
    result

let fetchAllDocuments (collectionName: string) : BsonDocument list =
    let collection = database.GetCollection<BsonDocument>(collectionName)
    let filter = Builders<BsonDocument>.Filter.Empty
    let documents = collection.Find(filter).ToList()
    documents |> Seq.toList

let printAllDocuments (collectionName: string) =
    let documents = fetchAllDocuments(collectionName)
    for doc in documents do
        printfn "%A" doc


let createUniqueIndex (collectionName: string) (indexField: string) =
    let collection = database.GetCollection<BsonDocument>(collectionName)
    let keys = Builders<BsonDocument>.IndexKeys.Ascending(indexField)
    let options = CreateIndexOptions()
    options.Unique <- true
    let indexModel = new CreateIndexModel<BsonDocument>(keys, options)
    collection.Indexes.CreateOne(indexModel)


let upsertDocumentById (collectionName: string) (id: string) (document: BsonDocument) =
    let collection = database.GetCollection<BsonDocument>(collectionName)
    let objectId = ObjectId.Parse(id)  // Parse the string ID to ObjectId
    let filter = Builders<BsonDocument>.Filter.Eq("_id", objectId)
    let options = new UpdateOptions()
    options.IsUpsert <- true
    collection.ReplaceOne(filter, document, options) |> ignore

