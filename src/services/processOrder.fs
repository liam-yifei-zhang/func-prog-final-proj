module Services.ProcessOrder

open Core.Domain

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
                emit (OrdersProcessed (FullTransactionStored order.Transaction))
            | (BuyOrderStatus.PartiallyMatched, _) | (_, SellOrderStatus.PartiallyMatched) ->
                // At least one leg partially fulfilled
                let remainingAmount = calculateRemainingAmountForPartiallyMatchedOrder order
                let additionalOrder = sendAdditionalOrderForUnmatchedAmount order remainingAmount
                storeCompletedAndAdditionalOrdersInDatabase order additionalOrder
                emit (OrdersProcessed (PartialTransactionStored order.Transaction))
                cumulativeTransactionValue <- cumulativeTransactionValue + order.TransactionValue
            | _ ->
                // One or both orders not fulfilled
                notifyUserViaEmail input.UserEmail
                storeTransactionAttemptInDatabase order
                emit (OrdersProcessed (DomainErrorRaised order.DomainError))
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