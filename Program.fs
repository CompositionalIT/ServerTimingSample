open Saturn
open Giraffe
open Lib.AspNetCore
open Lib.AspNetCore.ServerTiming
open Lib.AspNetCore.ServerTiming.Http.Headers
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open System
open System.Diagnostics
open System.Threading.Tasks

let endpoint next (ctx: HttpContext) =
    let serverTiming = ctx.GetService<IServerTiming>()
    serverTiming.Metrics.Add(ServerTimingMetric("cache", 300, "Cache"))
    serverTiming.Metrics.Add(ServerTimingMetric("sql", 900, "Sql Server"))
    serverTiming.Metrics.Add(ServerTimingMetric("fs", 600, "FileSystem"))
    serverTiming.Metrics.Add(ServerTimingMetric("cpu", 1230, "Total CPU"))
    text "Hello world" next ctx

let myRoutes next (ctx:HttpContext) = task {
    let serverTiming = ctx.GetService<IServerTiming>()
    let! sqlResult = task {
        use _ = serverTiming.TimeAction "sql"
        do! Task.Delay 500
        return "ALFKI"
    }

    let! readFromFile = task {
        let readingTask = Task.Delay 1500
        do! serverTiming.TimeTask (readingTask, "getStuffFromFileSystem")
        return "File contents"
    }

    return! text readFromFile next ctx
}

let app = application {
    use_router myRoutes
    service_config (fun svc -> svc.AddServerTiming())
    app_config (fun app -> app.UseServerTiming())
}

run app