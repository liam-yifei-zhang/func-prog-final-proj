module MongoDBUtil =

    open MongoDB.Driver
    open MongoDB.Bson
    open System

    let connectionString = "your_connection_string_here"
    let databaseName = "cryptoDatabase"
    let collectionName = "transactions"

    let client = MongoClient(connectionString)
    let database = client.GetDatabase(databaseName)
    let collection = database.GetCollection<BsonDocument>(collectionName)

    let insertDocument (document: BsonDocument) =
        try
            collection.InsertOne(document)
            true
        with
        | ex: Exception ->
            printfn "Error inserting document: %s" ex.Message
            false

    let insertManyDocuments (documents: BsonDocument list) =
        try
            collection.InsertMany(documents)
            true
        with
        | ex: Exception ->
            printfn "Error inserting documents: %s" ex.Message
            false



module Services.ProcessOrder

open Core.Domain
open Core.Domain
open MongoDBUtil
open MongoDB.Bson

type Order = {
    TotalQuantity: decimal
    FilledQuantity: decimal
    TransactionValue: decimal  // Assuming each order knows its transaction value.
}

let calculateRemainingAmountForPartiallyMatchedOrder (order: Order) : decimal =
    order.TotalQuantity - order.FilledQuantity

// Functions
let workflowProcessOrders (input: InvokeOrderProcessing) (parameters: TradingParameter) =
    let mutable cumulativeTransactionValue = 0m

    input.Orders
    |> List.iter (fun order ->
        match cumulativeTransactionValue + order.TransactionValue > parameters.maximalTransactionValue with
        | true ->
            // Stop processing further orders as the limit is exceeded.
            printfn "Maximal transaction value exceeded. Halting trading."
        | false ->
            match sendBuyOrderToExchange order, sendSellOrderToExchange order with
            | BuyOrderStatus.FullyMatched, SellOrderStatus.FullyMatched ->
                // Both legs fully fulfilled
                cumulativeTransactionValue <- cumulativeTransactionValue + order.TransactionValue
                storeCompletedTransactionInDatabase order
            | (BuyOrderStatus.PartiallyMatched, _) | (_, SellOrderStatus.PartiallyMatched) ->
                // At least one leg partially fulfilled
                let remainingAmount = calculateRemainingAmountForPartiallyMatchedOrder order
                let additionalOrder = sendAdditionalOrderForUnmatchedAmount order remainingAmount
                storeCompletedAndAdditionalOrdersInDatabase order additionalOrder
            | _ ->
                // One or both orders not fulfilled
                let messageBody = sprintf "Attention: One or both orders not fulfilled"
                notifyUserViaEmail input.UserEmail messageBody 
                storeTransactionAttemptInDatabase order
    )

// Events and Types
type InvokeOrderProcessing = {
    Orders: Order list
    UserEmail: string
}

type OrdersProcessed =
    | FullTransactionStored of Transaction
    | PartialTransactionStored of Transaction
    | DomainErrorRaised of DomainError

let storeCompletedTransactionInDatabase (order: Order) =
    let document = BsonDocument([
        ("Type", BsonString("Complete"))
        ("TotalQuantity", BsonDouble(order.TotalQuantity))
        ("FilledQuantity", BsonDouble(order.FilledQuantity))
        ("TransactionValue", BsonDouble(order.TransactionValue))
    ])
    insertDocument document |> ignore

let storeCompletedAndAdditionalOrdersInDatabase (originalOrder: Order) (additionalOrder: Order) =
    let documents = [
        BsonDocument([
            ("Type", BsonString("Original"))
            ("TotalQuantity", BsonDouble(originalOrder.TotalQuantity))
            ("FilledQuantity", BsonDouble(originalOrder.FilledQuantity))
            ("TransactionValue", BsonDouble(originalOrder.TransactionValue))
        ])
        BsonDocument([
            ("Type", BsonString("Additional"))
            ("TotalQuantity", BsonDouble(additionalOrder.TotalQuantity))
            ("FilledQuantity", BsonDouble(additionalOrder.FilledQuantity))
            ("TransactionValue", BsonDouble(additionalOrder.TransactionValue))
        ])
    ]
    insertManyDocuments documents |> ignore

let storeTransactionAttemptInDatabase (order: Order) =
    let document = BsonDocument([
        ("Type", BsonString("Attempt"))
        ("TotalQuantity", BsonDouble(order.TotalQuantity))
        ("FilledQuantity", BsonDouble(order.FilledQuantity))
        ("TransactionValue", BsonDouble(order.TransactionValue))
    ])
    insertDocument document |> ignore