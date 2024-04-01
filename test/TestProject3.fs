module TestProject3

open NUnit.Framework

// Define a discriminated union type to represent different causes
type Cause =
    | ArbitrageVolumeReached
    | UserInvocation

// Define a record type to represent the data related to transactions volume
type TransactionsVolumeUpdated = {
    MaximalDailyTransactionsVolume: int
    TradeBookedValue: float
}

// Define a record type to represent the reason for deactivating a trading strategy
type TradingStrategyDeactivated = {
    Cause: Cause
}

// Define the function under test that checks if the arbitrage volume has reached a certain threshold
let checkIfArbitrageVolumeReached (input: TransactionsVolumeUpdated) =
    input.MaximalDailyTransactionsVolume > 100 && input.TradeBookedValue > 1000.0

// Set up any necessary resources or state before running each test case
[<SetUp>]
let Setup () =
    // This function is called before each test case
    // Any necessary setup tasks can be performed here
    ()

// Define test cases using the TestCase attribute
// Each test case provides input values and an expected result

// Test case 1: MaximalDailyTransactionsVolume > 100 and TradeBookedValue > 1000.0
[<TestCase(200, 1500.0, true)>]
// Test case 2: MaximalDailyTransactionsVolume <= 100 or TradeBookedValue <= 1000.0
[<TestCase(50, 500.0, false)>]
// Test case 3: MaximalDailyTransactionsVolume > 100 and TradeBookedValue > 1000.0
[<TestCase(150, 2000.0, true)>]
// Add more test cases as needed

// Define the test function that will be executed for each test case
[<Test>]
let ``Update Transactions Volume Workflow Test`` (maxVolume, tradeValue, expectedResult) =
    // Arrange
    // Create an instance of TransactionsVolumeUpdated with the provided input values
    let inputEvent = { MaximalDailyTransactionsVolume = maxVolume; TradeBookedValue = tradeValue }

    // Act
    // Call the function under test with the input data
    let volumeReached = checkIfArbitrageVolumeReached inputEvent

    // Assert
    // Verify that the actual result matches the expected result
    Assert.AreEqual (expectedResult, volumeReached)
