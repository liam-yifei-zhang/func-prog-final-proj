module UserSetting

open Core.Domain
open Infra.Db

//in domain.fs
(*
type UserSettings = {
    NumberOfCryptoCurrencies: int
    MinimalPriceSpread: float
    MinimalTransactionProfit: float
    MaximalTransactionValue: float
    MaximalTradingValue: float // Updated to reflect actual usage
    UserEmail: string
}
*)

//assume there is a user record in the database
let User = {
    NumberOfCryptoCurrencies = 0
    MinimalPriceSpread = 0.0f
    MinimalTransactionProfit = 0.0f
    MaximalTransactionValue = 0.0f
    MaximalTradingValue = 0.0f
    UserEmail = ""
}

//call database to update user settings
let updateDatabase (User: UserSettings) =
    async {
        do! Db.updateUserSettings User
        return ()
    }

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