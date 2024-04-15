module CrossExchangeTrading

open System.IO

open BitfinexAPI
open BitstampAPI
open KrakenAPI


let fetchAllPairs = async {
    let! bitfinexResult = BitfinexAPI.fetchBitfinexPairs()
    let! bitstampResult = BitstampAPI.fetchBitstampPairs()
    let! krakenResult = KrakenAPI.fetchKrakenPairs()
    match (bitfinexResult, bitstampResult, krakenResult) with
    | (Result.Ok bitfinexPairs, Result.Ok bitstampPairs, Result.Ok krakenPairs) ->
        return Result.Ok (bitfinexPairs, bitstampPairs, krakenPairs)
    | _ ->
        return Result.Error "Failed to fetch pairs from one or more exchanges."
}

let findCommonPairs (bitfinexPairs, bitstampPairs, krakenPairs) =
    let commonPairs = HashSet<_>(bitfinexPairs)
    commonPairs.IntersectWith(HashSet<_>(bitstampPairs))
    commonPairs.IntersectWith(HashSet<_>(krakenPairs))
    commonPairs

let writePairsToFile (pairs: HashSet<string>) (filePath: string) =
    let data = pairs |> Seq.map (fun pair -> pair + Environment.NewLine) |> String.concat ""
    File.WriteAllText(filePath, data)

let processAndStorePairs = async {
    let! result = fetchAllPairs
    match result with
    | Result.Ok (bitfinexPairs, bitstampPairs, krakenPairs) ->
        let commonPairs = findCommonPairs (bitfinexPairs, bitstampPairs, krakenPairs)
        let filePath = "common_pairs.txt"
        writePairsToFile commonPairs filePath
        return Result.Ok "Pairs written to file successfully."
    | Result.Error msg ->
        return Result.Error msg
}



