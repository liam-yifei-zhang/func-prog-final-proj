namespace Core.Domain

module Core.Domain

type CurrencyPair = string * string

type ArbitrageOpportunity = {
    BuyExchange: string
    SellExchange: string
    Pair: string
    Profit: decimal
}

type Trade = {
    Pair: string
    Quantity: decimal
    Price: decimal
}

type Transaction = {
    BuyTrade: Trade
    SellTrade: Trade
    Profit: decimal
}

type AnnualizedReturnData = {
    InitialInvestment: decimal
    FinalValue: decimal
    Years: decimal
}

type UserSettings = {
    NumberOfCryptoCurrencies: int
    MinimalPriceSpread: float
    MinimalTransactionProfit: float
    MaximalTransactionValue: float
    MaximalTradingValue: float // Updated to reflect actual usage
    UserEmail: string
}

type HistoricalQuote = {
    Exchange: string
    CurrencyPair: string
    Bid: float
    Ask: float
    Timestamp: System.DateTime
}

type EventQuote = {
    ev: string
    pair: CurrencyPair
    lp: int
    ls: int
    bp: float
    bs: float
    ap: float
    as: float
    t: int
    x: int
    r: int
}

type DomainError =
    | BelowMinimalProfit
    | ExceedsMaximalTransactionValue
    | ExceedsMaximalTradingValue
    | NoOpportunityFound
    | InvalidMarketData