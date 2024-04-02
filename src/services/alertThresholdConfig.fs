module alertThresholdConfig =
    open System
    type Input = 
    | UserConfiguresAlertThreshold of UserSetsAlertThreshold 
    | UserRequestsThresholdReset of UserInvokesThresholdReset

    type Output = 
        | AlertThresholdSet of AlertThresholdUpdatedInDatabase
        | AlertThresholdUpdateFailed of AlertThresholdUpdateNotSuccessful
        | AlertThresholdUpdated of ThresholdReset

    let setAlertThreshold (input: UserSetsAlertThreshold) =
        validateUserInput input
        |> function
            | true ->
                updateAlertThresholdInDatabase input
                |> fun result -> 
                    match result with
                    | UpdateSuccessful ->
                        match input.AutoStopTrading with
                        | true -> 
                            updateAutoStopTradingFlagInDatabase input
                            AlertThresholdSet AlertThresholdUpdatedInDatabase |> emit
                        | false -> 
                            AlertThresholdSet AlertThresholdUpdatedInDatabase |> emit
                    | UpdateFailed ->
                        AlertThresholdUpdateFailed AlertThresholdUpdateNotSuccessful |> emit
            | false -> 
                AlertThresholdUpdateFailed AlertThresholdUpdateNotSuccessful |> emit

    let resetAlertThreshold () =
        resetAlertThresholdInSystem()
        |> function
            | ResetSuccessful -> AlertThresholdUpdated ThresholdReset |> emit
            | ResetFailed -> AlertThresholdUpdateFailed AlertThresholdUpdateNotSuccessful |> emit

    let workflowConfigureAlertThreshold (input: Input) =
        match input with
            | UserConfiguresAlertThreshold config -> setAlertThreshold config
            | UserRequestsThresholdReset _ -> resetAlertThreshold ()
            | _ -> printfn "Domain error: unsupported operation"

    // Mock types and functions for completeness of the example
    type UserSetsAlertThreshold = {
        ThresholdValue: decimal
        AutoStopTrading: bool
    }

    type UserInvokesThresholdReset = unit

    type AlertThresholdUpdatedInDatabase = unit
    type AlertThresholdUpdateNotSuccessful = unit
    type ThresholdReset = unit

    type UpdateResult = UpdateSuccessful | UpdateFailed
    type ResetResult = ResetSuccessful | ResetFailed

    // let validateUserInput (_: UserSetsAlertThreshold) = true
    // let updateAlertThresholdInDatabase (_: UserSetsAlertThreshold) = UpdateSuccessful
    // let updateAutoStopTradingFlagInDatabase (_: UserSetsAlertThreshold) = ()
    // let resetAlertThresholdInSystem () = ResetSuccessful
    // let getInputEvent () = UserConfiguresAlertThreshold { ThresholdValue = 100m; AutoStopTrading = true }