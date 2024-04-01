// Todo:
// Replace processOrder with workflowProcessOrders and verify the workflow is correct
// Add error handling

module Services.ProcessOrder

open Core.Domain

// Functions
let processOrder (input: InvokeOrderProcessing) : OrdersProcessed =
    // Placeholder implementation to process orders and store transaction data
    let _ = input |> ignore
    {
        FullTransactionStored = ()
        PartialTransactionStored = ()
        DomainErrorRaised = ()
    }

let workflowProcessOrders (input: InvokeOrderProcessing) =
    input
    |> List.map processOrder
    |> List.iter (fun order ->
        match (sendBuyOrderToExchange order, sendSellOrderToExchange order) with
        | (BuyOrderStatus.FullyMatched, SellOrderStatus.FullyMatched) ->
            storeCompletedTransactionInDatabase order
            emit (OrdersProcessed (FullTransactionStored order.Transaction))
        | (BuyOrderStatus.PartiallyMatched, SellOrderStatus.FullyMatched)
        | (BuyOrderStatus.FullyMatched, SellOrderStatus.PartiallyMatched) ->
            let remainingAmount = calculateRemainingAmountForPartiallyMatchedOrder order
            sendAdditionalOrderForUnmatchedAmount remainingAmount
            storeCompletedAndAdditionalOrdersInDatabase order
            emit (OrdersProcessed (PartialTransactionStored order.Transaction))
        | _ ->
            raise (DomainError "One or both orders not fulfilled")
            notifyUserViaEmail order.UserEmail
            storeTransactionAttemptInDatabase order
            emit (OrdersProcessed (DomainErrorRaised order.DomainError))
        )

// Events
type InvokeOrderProcessing = UserInitiatesOrderProcessing

type OrdersProcessed =
    | FullTransactionStored of Transaction
    | PartialTransactionStored of Transaction
    | DomainErrorRaised of DomainError


    

