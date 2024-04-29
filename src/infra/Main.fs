open System
open System.Net
open System.Net.WebSockets
open System.Text.Json
open System.Threading
open System.Text
open MongoDBUtil
open MongoDB.Driver
open MongoDB.Bson
open RealTimeTrading

//TODO:
//Utilize Result types for handling errors
//Introduce error handling
//Define Polygon message types and introduce a message processing function
//Improve encapsulation in the code

//Define a function to connect to the WebSocket
let connectToWebSocket (uri: Uri) =
        async {
        let wsClient = new ClientWebSocket()
        //Convert a .NET task into an async workflow
        //Run an asynchronous computation in a non-blocking way
        do! Async.AwaitTask (wsClient.ConnectAsync(uri, CancellationToken.None))
        //Returning Websockets instance from async workflow
        return wsClient
        }
        
// Define a function to receive data from the WebSocket
let receiveData (wsClient: ClientWebSocket) : Async<unit> =
    let buffer = Array.zeroCreate 10024

    let rec receiveLoop () = async{
        let segment = new ArraySegment<byte>(buffer)

        let! result =
            wsClient.ReceiveAsync(segment, CancellationToken.None)
            //Convert a .NET task into an async workflow
            //Asynchronously await the completion of an asynchronous computation (non-blocking)
            |> Async.AwaitTask

        match result.MessageType with
        | WebSocketMessageType.Text ->
            let message = Encoding.UTF8.GetString(buffer, 0, result.Count)
            printfn "%s" message
            // TODO: Process the message
            // You should define Polygon message types and message processing logic
            // You should also utilize Result types for error handling
            let quoteResult = parseQuoteFromMessage message
            match quoteResult with
            | Ok quote ->
                let quotePair = processQuote quote
                quotePair |> ignore
            | Error errMsg ->
                printfn "Error parsing message: %s" errMsg
            return! receiveLoop ()
        | _ -> return! receiveLoop () // Ignore non-text messages
    }
    receiveLoop ()
    
// Define a type for the message we want to send to the WebSocket
type Message = { action: string; params: string }    
// Define a function to send a message to the WebSocket
let sendJsonMessage (wsClient: ClientWebSocket) message =
        let messageJson = JsonSerializer.Serialize(message)
        let messageBytes = Encoding.UTF8.GetBytes(messageJson)
        wsClient.SendAsync((new ArraySegment<byte>(messageBytes)), WebSocketMessageType.Text, true, CancellationToken.None) |> Async.AwaitTask |> Async.RunSynchronously
    
// Define a function to start the WebSocket client
// Sample subscripton parameters: "XT.BTC-USD"
// See https://polygon.io/docs/crypto/ws_getting-started
let start(uri: Uri, apiKey: string, subscriptionParameters: string) =
            async {
            //Establish websockets connectivity
            //Run underlying async workflow and await the result
            let! wsClient = connectToWebSocket uri
            //Authenticate with Polygon
            sendJsonMessage wsClient { action = "auth"; params = apiKey }
            //Subscribe to market data
            sendJsonMessage wsClient { action = "subscribe" ; params = subscriptionParameters }
            //Process market data
            do! receiveData wsClient
            }
         
[<EntryPoint>]
let main args =

    // test insert a single dummy document
    
    let currencyPairCollectionName = "currencyPairs"
    let document = BsonDocument([
        BsonElement("pair", BsonString("BTC-USD"))
    ])

    let result = insertDocument currencyPairCollectionName document

    let documents = fetchAllDocuments(currencyPairCollectionName)
    for doc in documents do
        printfn "%A" doc

    

    let uri = Uri("wss://socket.polygon.io/crypto")
    let apiKey = "phN6Q_809zxfkeZesjta_phpgQCMB2Dw"
    let subscriptionParameters = "XT.BTC-USD"
    start (uri, apiKey, subscriptionParameters) |> Async.RunSynchronously
    0