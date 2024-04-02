# Crypto Arbitrage Gainer

This project is written in F#.

## File Structure

The project follows the Onion Architecture to maintain a clean separation of concerns. Here's an overview of the primary layers:

-   **Core**: Contains the domain model and business logic. It's the heart of the application, independent of any infrastructure or external dependencies.

    -   `/src/core` - Domain entities, value objects, and domain interfaces.

-   **Infrastructure**: Implements external concerns and tools such as database access, file system manipulation, and external APIs. This layer supports the core by implementing interfaces defined within it.

    -   `/src/infra` - Database context, migrations, repositories, and any external service implementations.

-   **Services**: Acts as an intermediary between the UI/API layer and the core logic. It contains application services that orchestrate the execution of domain logic.
    -   `/src/services` - Application services, DTOs (Data Transfer Objects), and mappers.

This structure ensures that dependencies flow inwards, with the core at the center, thus facilitating maintainability and scalability.

## Workflow - file correspondence
Note that the file we provide may have dependencies from the core folder, those imported files are specified at the top of each file, please refer to the corresponding file in the core folder if you want to check out the dependencies. All important dependecies have their own workflow-related files in the core folder. If you have further questions, please consider email the owner of this repository.

-   **Update User Settings**:

    -   `/src/core/domain.fs`
    -   `/src/services/userSettings.fs`

-   **Identify and Store Cryptocurrency Data**:

    -   `/src/core/domain.fs`
    -   `/src/services/cryptoData.fs`

-   **Identify and Store Historical Arbitrage Opportunities**:

    -   `/src/core/arbitrageStrategies.fs`
    -   `/src/core/domain.fs`
    -   `/src/services/IdentifyHistoricalArvitrage.fs`

-   **Execute Real-time Trading**:

    -   `/src/core/domain.fs`
    -   `/src/services/realTimeTrading.fs`

-   **Process Orders**:

    -   `/src/core/domain.fs`
    -   `/src/services/processOrder.fs`

-   **P&L Calculation**:

    -   `/src/core/domain.fs`
    -   `/src/services/pnlCalc.fs`

-   **Calculate Annualized Return**:

    -   `/src/core/calcAnnualizedReturn.fs`
    -   `/src/services/annualizedReturnCalc.fs`

-   **On-Demand historic P&L Calculation**:

    -   `/src/core/CalcPnL.fs`
    -   `/src/services/historicalPnlCalc.fs`

-   **Configure Alert Threshold**:

    -   `/src/services/alertThresholdConfig.fs`