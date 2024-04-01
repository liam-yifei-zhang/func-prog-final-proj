# Crypto Arbitrage Gainer

This project is written in F#.

## File Structure

The project follows the Onion Architecture to maintain a clean separation of concerns. Here's an overview of the primary layers:

-   **Core**: Contains the domain model and business logic. It's the heart of the application, independent of any infrastructure or external dependencies.

    -   `/src/Core` - Domain entities, value objects, and domain interfaces.

-   **Infrastructure**: Implements external concerns and tools such as database access, file system manipulation, and external APIs. This layer supports the core by implementing interfaces defined within it.

    -   `/src/Infra` - Database context, migrations, repositories, and any external service implementations.

-   **Services**: Acts as an intermediary between the UI/API layer and the core logic. It contains application services that orchestrate the execution of domain logic.
    -   `/src/Services` - Application services, DTOs (Data Transfer Objects), and mappers.

This structure ensures that dependencies flow inwards, with the core at the center, thus facilitating maintainability and scalability.

## Instructions

1. Complete the `src/core/arbitrageStrategies.fs` implementation. Specifically, we need to:
    1. calcuate the spread of a given pair between 2 crypto exchanges at a given moment.
    2. identify if there is arbitrage opportunity.
2. Complete the `src/core/domain.fs` with the types and interfaces that are used in the rest of the codebase.
3. Check if the `src/infra` files has the correct skeleton.
4. For `src/services`, these files only need the code skeleton:
    1. `cryptoData.fs`
    2. `userSettings.fs`
    3. `processOrder.fs`
    4. `alertThreshold.fs`
5. These files in `src/services` need to be implemented (only the pure functions):
    1. `pnlCalc.fs`
    2. `annualizedReturnCalc.fs`
    3. `backTest.fs`
    4. `historicalPnlCalc.fs`
    5. `realTimeTrading.fs`
6. We need error handling for every functions. Do your best, check the psuedo code in google file.
7. We need unit tests specified in the `test` folder (follow the sample test I have included).

### Side note

1. All workflow needs to be converted into F#.
2. We can only use match-case, so no if-else are allowed.
3. We can only use recursion, so no loop are allowed.
4. Try your best to use sample data provided. If you can't you can make up some data.
