type Input = 
    | UserConfiguresAlertThreshold of UserSetsAlertThreshold | UserUpdatesAlertThreshold
    | UserRequestsThresholdReset of UserInvokesThresholdReset

type Output = 
    | AlertThresholdSet of AlertThresholdUpdatedInDatabase
    | AlertThresholdUpdateFailed of AlertThresholdUpdateNotSuccessful
    | AlertThresholdUpdated of ThresholdReset

let setAlertThreshold (input: Input) =
    match input with
    | UserConfiguresAlertThreshold config ->
        validateUserInput config
        |> function
            | true ->
                updateAlertThresholdInDatabase config
                |> fun _ -> 
                    match config.AutoStopTrading with
                    | true -> updateAutoStopTradingFlagInDatabase config
                    | false -> ()
                AlertThresholdSet AlertThresholdUpdatedInDatabase
                |> emit
            | false -> AlertThresholdUpdateFailed AlertThresholdUpdateNotSuccessful |> emit
    | _ -> ()

let resetAlertThreshold (input: Input) =
    match input with
    | UserRequestsThresholdReset _ ->
        resetAlertThresholdInSystem()
        |> fun _ -> AlertThresholdUpdated ThresholdReset |> emit
    | _ -> ()

let workflowConfigureAlertThreshold () =
    getInputEvent()
    |> function
        | UserConfiguresAlertThreshold _ as input -> setAlertThreshold input
        | UserRequestsThresholdReset _ as input -> resetAlertThreshold input
        | _ -> printfn "Unsupported operation"
