module Main

open System
open MongoDB.Bson
open MongoDBUtil

// 1. Make sure you add related files in the .fsproj file like this: <Compile Include="../src/infra/MongoDBUtil.fs" />
// 2. After you made changes in the .fsproj file, run "dotnet build"
// 3. You can run the program with "dotnet run" or "dotnet run --project src/main.fsproj"

[<EntryPoint>]
let main argv =
    // Example usage of MongoDBUtil
    let document = BsonDocument("name", BsonString("Test Document"))
    let result = MongoDBUtil.insertDocument(document)
    printfn "Insert operation successful: %b" result
    0 // Return an integer exit code
