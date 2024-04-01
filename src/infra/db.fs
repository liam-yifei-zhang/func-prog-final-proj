
module Infra.Db

open System
open System.Data.SqlClient

type Database(connectionString: string) =
    let connection = new SqlConnection(connectionString)

    member this.Connect() = 
        try
            connection.Open()
            printfn "Database connected successfully."
        with
        | ex -> printfn "Error connecting to database: %s" ex.Message

    member this.Disconnect() = 
        try
            connection.Close()
            printfn "Database disconnected."
        with
        | ex -> printfn "Error disconnecting from database: %s" ex.Message

    member this.Query(query: string) = 
        try
            let command = new SqlCommand(query, connection)
            let reader = command.ExecuteReader()
            // Process the query result if needed
            printfn "Query executed successfully."
            reader.Close() // Close the reader when done
        with
        | ex -> printfn "Error executing query: %s" ex.Message