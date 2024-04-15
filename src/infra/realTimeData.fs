open System
open System.Net
open System.Net.WebSockets
open System.Text.Json
open System.Threading
open System.Text

// Define Polygon message types
type PolygonMessage = {
    EventType : string
    Symbol : string
    TradeID : string
    ExchangeID : int
    Price : decimal
    Size : int
    Timestamp : int64
    Tape : int
}

// Parce the polygon message
let parsePolygonMessage (json: string) : Result<PolygonMessage, string> =
    let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    try
        let message = JsonSerializer.Deserialize<PolygonMessage>(json, options)
        match message with
        | null -> Error "Failed to parse the message: JSON is null."
        | _ -> Ok message
    with
    | ex -> Error (sprintf "Failed to parse the message: %s" ex.Message)

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

    let rec receiveLoop () = async {
        let segment = new ArraySegment<byte>(buffer)

        let! result =
            wsClient.ReceiveAsync(segment, CancellationToken.None)
            |> Async.AwaitTask

        match result.MessageType with
        | WebSocketMessageType.Text ->
            let message = Encoding.UTF8.GetString(buffer, 0, result.Count)
            match parsePolygonMessage message with
            | Ok polygonMessage ->
                // Process valid message
                printfn "Received valid message: %A" polygonMessage
                return! receiveLoop ()
            | Error errMsg ->
                // Log or handle the error
                printfn "Error processing message: %s" errMsg
                return! receiveLoop ()
        | _ -> 
            // Continue receiving if non-text message
            return! receiveLoop ()
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
    let uri = Uri("wss://socket.polygon.io/crypto")
    let apiKey = "phN6Q_809zxfkeZesjta_phpgQCMB2Dw"
    let subscriptionParameters = "XT.BTC-USD"
    start (uri, apiKey, subscriptionParameters) |> Async.RunSynchronously
    0