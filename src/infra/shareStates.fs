open System
open Microsoft.FSharp.Control

type TransactionRecord = {
    TransactionType: string
    Quantity: decimal
    Price: decimal
    CurrencyPair: string
    TransactionDate: DateTime
}

type MailboxMessage =
    | AddRecords of TransactionRecord list
    | GetRecords of AsyncReplyChannel<TransactionRecord list>

let transactionRecordsAgent = MailboxProcessor<MailboxMessage>.Start(fun inbox ->
    let rec loop (records: TransactionRecord list) = async {
        let! msg = inbox.Receive()
        match msg with
        | AddRecords newRecords ->
            return! loop (records @ newRecords)  // Append new records to the existing list.
        | GetRecords replyChannel ->
            replyChannel.Reply(records)  // Send the current list of records back through the reply channel.
            return! loop records
    }
    loop [])  // Start with an empty list of records.

let getTransactionRecordsAsync () : Async<TransactionRecord list> =
    Async.FromContinuations(fun (cont, econt, ccont) ->
        transactionRecordsAgent.PostAndAsyncReply(GetRecords, cont))

let processTransactionRecords () =
    async {
        let! records = getTransactionRecordsAsync ()
        printfn "Retrieved %d records." (List.length records)
        // Further processing can happen here.
    } |> Async.Start
