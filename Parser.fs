namespace Imed.Parser

open System
open System.IO
open System.Text
open System.Text.RegularExpressions

type Event1C = {
    Date: DateTime
    Duration: TimeSpan
    Name: string
    Level: int
    Properties: array<struct(string * string)> } 
    with 
    override this.ToString() =
        let date = this.Date.ToString("dd.MM.yyyy HH:mm:ss.ffffff") 
        sprintf "%s-%i,%s" date (this.Duration.Ticks / 10L) this.Name
    member this.TryGetProperty name =
        match Array.tryFind (fun struct(k,v) -> k = name) this.Properties with
        | Some (k,v) -> Some v
        | _ -> None

[<RequireQualifiedAccess>]
module Parser = 

    let readToDelimiter (sr: StreamReader, delimiter: char, buffer: StringBuilder, endOfLine: byref<bool>) =
        endOfLine <- false
        let mutable c = sr.Read();
        if c = int('"') || c = int('\'') then
            let quote = c // Запомнить кавычку, но в буфер не нужно помещать.
            c <- sr.Read()
            while c <> -1 do
                if c = quote then
                    if sr.Peek() = quote then
                        // Значит двойная кавычка, одна должна быть пропущена
                        sr.Read() |> ignore
                    else
                        // Значит это одинарная кавычка
                        c <- -1
                if c <> -1 then
                    buffer.Append(char c) |> ignore
                    c <- sr.Read()                

            // Удалить перевод строки
            let p = sr.Peek()
            if p = int delimiter then
                sr.Read() |> ignore
            else if p = 13 then
                sr.Read() |> ignore
                if sr.Peek() = 10 then
                    sr.Read() |> ignore
                endOfLine <- true     

        else
            while c <> -1 && c <> int delimiter do
                if c = 13 then
                    endOfLine <- true
                    if sr.Peek() = 10 then
                        sr.Read() |> ignore
                    c <- -1
                else 
                    buffer.Append((char)c) |> ignore
                    c <- sr.Read()            

        let retValue = buffer.ToString()
        buffer.Clear() |> ignore
        retValue
        

    let parse(date: DateTime, sr: StreamReader, buffer: StringBuilder): Event1C =
        let mutable endOfLine = false

        let minutes = readToDelimiter(sr, ':', buffer, &endOfLine) |> Int32.Parse
        let seconds = readToDelimiter(sr, '.', buffer, &endOfLine) |> Int32.Parse
        let microSeconds = readToDelimiter(sr, '-', buffer, &endOfLine) |> Int32.Parse
        let duration = readToDelimiter(sr, ',', buffer, &endOfLine) |> Int64.Parse
        let name = readToDelimiter(sr, ',', buffer, &endOfLine).ToUpper()
        let level = readToDelimiter(sr, ',', buffer, &endOfLine) |> Int32.Parse

        let properties = [|
            while (not endOfLine && not sr.EndOfStream) do
                let name = readToDelimiter(sr, '=', buffer, &endOfLine)
                let value = readToDelimiter(sr, ',', buffer, &endOfLine)
                struct(name, value)
        |]

        {
            Date = date.AddMinutes(float minutes).AddSeconds(float seconds).AddTicks((int64 microSeconds) * 10L) 
            Duration = TimeSpan.FromTicks(duration * 10L)
            Name = name
            Level = level
            Properties = properties
        }

    let getEvents(fileName: string) =
        seq {
            let info = FileInfo fileName
            if not info.Exists then
                FileNotFoundException(fileName) |> raise
            if not (Regex.IsMatch(info.Name, @"\d{8}\.log$")) then
                ArgumentException("Invalid format filename") |> raise
            let year = 2000 + (Int32.Parse(info.Name.AsSpan().Slice(0, 2)))
            let month = Int32.Parse (info.Name.AsSpan().Slice(2, 2))
            let day = Int32.Parse (info.Name.AsSpan().Slice(4, 2))
            let hour = Int32.Parse (info.Name.AsSpan().Slice(6, 2))
            let date = DateTime(year, month, day, hour, 0, 0)
            let buffer = StringBuilder(4096 * 128)
            use fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            use sr = new StreamReader(fs, Encoding.UTF8, true, 4096 * 1024)

            while not sr.EndOfStream do
                parse(date, sr, buffer)
        }
  