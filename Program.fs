open System
open Imed.Parser
open System.IO
open System.Diagnostics

[<EntryPoint>]
let main argv =
    // Тут тоже можно писать код и запустить с помощью "dotnet run -c Release" 
    let w = Stopwatch.StartNew()
    let allEvents =
        Directory.GetFiles(@"C:\Users\medvedev_i\Desktop\1\task_queue\", "*.log", SearchOption.AllDirectories)
        |> Seq.map Parser.getEvents
        |> Seq.collect id
    let count = allEvents |> Seq.length
    w.Stop()
    Console.WriteLine(sprintf "Длительность: %O  Количество: %i" w.Elapsed count)
    0