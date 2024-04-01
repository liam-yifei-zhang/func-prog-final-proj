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
