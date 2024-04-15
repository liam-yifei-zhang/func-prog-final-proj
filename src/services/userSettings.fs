module UserSetting

open System
open FSharp.Control

type UserSettingsUpdate =
    | UpdateMaximalTradingValue of float
    | UpdateUserEmail of string
    | GetUserSettings of AsyncReplyChannel<UserSettings>

type UserSettings = {
    NumberOfCryptoCurrencies: int
    MinimalPriceSpread: float
    MinimalTransactionProfit: float
    MaximalTransactionValue: float
    MaximalTradingValue: float
    UserEmail: string
}

let initialSettings = {
    NumberOfCryptoCurrencies = 10
    MinimalPriceSpread = 0.1f
    MinimalTransactionProfit = 100.0f
    MaximalTransactionValue = 10000.0f
    MaximalTradingValue = 50000.0f
    UserEmail = "user@example.com"
}

let userSettingsAgent = MailboxProcessor.Start(fun inbox ->
    let rec messageLoop (currentState: UserSettings) = async {
        let! message = inbox.Receive()
        match message with
        | UpdateMaximalTradingValue maxValue ->
            let newState = { currentState with MaximalTradingValue = maxValue }
            return! messageLoop newState
        | UpdateUserEmail email ->
            let newState = { currentState with UserEmail = email }
            return! messageLoop newState
        | GetUserSettings replyChannel ->
            replyChannel.Reply(currentState)
            return! messageLoop currentState
    }
    messageLoop initialSettings
)

let updateUserSharedStates (newSettings: UserSettings) =
    userSettingsAgent.Post(UpdateMaximalTradingValue newSettings.MaximalTradingValue)
    userSettingsAgent.Post(UpdateUserEmail newSettings.UserEmail)

    // Optionally wait for some operation to complete if needed
    // Here, for example, we might want to fetch and print the updated settings
    let updatedSettings = userSettingsAgent.PostAndReply(fun reply -> GetUserSettings reply)
    printfn "Updated Settings: %A" updatedSettings



//update maximal trading value
let updateMaximalTradingValue (User: UserSettings) MaximalTradingValue = 
    async {
        let NewUser = {
            NumberOfCryptoCurrencies = User.NumberOfCryptoCurrencies
            MinimalPriceSpread = User.MinimalPriceSpread
            MinimalTransactionProfit = User.MinimalTransactionProfit
            MaximalTransactionValue = User.MaximalTransactionValue
            MaximalTradingValue = MaximalTradingValue
            UserEmail = User.UserEmail
        }
        do! updateDatabase NewUser
    }

//update user email
let updateUserEmail (User: UserSettings) UserEmail = 
    async {
        let NewUser = {
            NumberOfCryptoCurrencies = User.NumberOfCryptoCurrencies
            MinimalPriceSpread = User.MinimalPriceSpread
            MinimalTransactionProfit = User.MinimalTransactionProfit
            MaximalTransactionValue = User.MaximalTransactionValue
            MaximalTradingValue = User.MaximalTradingValue
            UserEmail = UserEmail
        }
        do! updateDatabase NewUser
    }