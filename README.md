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
    -   `/src/core/CalcPnL.fs`
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

## Side Effects -- along with the error handling

-   **1.1 Accept user input**:

    -   `/src/services/userSettings.fs`
    -   `/src/services/pnlrouter.fs`
    -   `/src/services/strategyRouter.fs`

-   **1.2 Retrieval of cross-traded currency pairs**:

    -   `/src/infra/BitStampApi.fs`
    -   `/src/infra/BitfinexApi.fs`
    -   `/src/infra/KrakenApi.fs`    
    -   `/src/services/cryptoData.fs`

-   **1.3 Historical arbitrage opportunities calculation**:

    -   `/src/services/identifyHistoricalArbitrage.fs`

-   **1.4 Real-time market data retrieval**:

    -   `/src/infra/realTimeData.fs`
    -   `/src/services/realTimeTrading.fs`

-   **1.5 Order management**:

    -   `/src/infra/BitStampApi.fs`
    -   `/src/infra/BitfinexApi.fs`
    -   `/src/infra/KrakenApi.fs`
    -   `/src/services/processOrder.fs`

-   **1.6 Data persistance**:

    -   `/src/services/annualizedReturnCalc.fs`
    -   `/src/services/cryptoData.fs`
    -   `/src/services/historicalPnlCalc.fs`
    -   `/src/services/processOrder.fs`

-   **1.7 E-mail notifications**:

    -   `/src/infra/email.fs`
    -   `/src/services/processOrder.fs`
    -   `/src/services/pnlCalc.fs`

### API Documentation

#### POST Requests

#### Start Trading

<details>
 <summary><code>POST</code> <code><b>/trade/start</b></code> <code>(Allows user to start real time trading)</code></summary>

##### Parameters: e.g./trade/start

</details>

---

#### All cross-traded currencies

<details>
 <summary><code>POST</code> <code><b>/trade/stop</b></code> <code>(Allows user to stop real-time trading)</code></summary>

##### Parameters: e.g./trade/stop

</details>

---

#### Post historical arbitrage opportunities

<details>
 <summary><code>POST</code> <code><b>/opportunities</b></code> <code>(Allows user to post historical arbitrage opportunities)</code></summary>

##### Parameters: e.g./opportunities

</details>

---
#### Post P&L results

<details>
 <summary><code>POST</code> <code><b>/pnl</b></code> <code>(Allows user to post P&L calculation results)</code></summary>

##### Parameters: e.g./pnl

</details>

---


#### Post User Parameters

<details>
 <summary><code>POST</code> <code><b>/strategies/:Currencies/:MinimalPriceSpread/:MinimalTransactionProfit/:MaximalTransactionValue/:MaximalTradingValue/:Email</b></code> <code>(Allows user to post parameters)</code></summary>

##### Parameters: e.g./strategies/5/0.05/5/2000/5000/beast@andrew.cmu.edu
> | name      | type      | data type | description                 |
> |-----------|-----------|-----------|-----------------------------|
> | Currencies  | required  | int    | the number of cryptocurrencies to track    |
> | MinimalPriceSpread  | required  | float | the minimal price spread    |
> | MinimalTransactionProfit  | required  | float  | the minimal transaction profit   |
> | MaximalTransactionValue  | required  | float  |  the maximal transaction value  |
> | MaximalTradingValue  | required  | float  | the maximal trading value  |
> | Email  | required  | float  | user's email  |

</details>

---
#### GET Requests
#### All cross-traded currencies

<details>
 <summary><code>GET</code> <code><b>/crosscurrency</b></code> <code>(Allows user to fetch all the cross traded currencies)</code></summary>

##### Parameters: e.g./crosscurrency

</details>

---
