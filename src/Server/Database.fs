module Database

open System
open Marten
open System.Linq

type User = {
    Id : Guid
    Username : string
    Password : string    
}

[<Literal>]
let ConnecitonString = "host=localhost;database=MartenDb;username=me;password=topSecret"

let store = DocumentStore.For(fun x -> 
    x.AutoCreateSchemaObjects <- AutoCreate.CreateOrUpdate
    x.Connection(ConnecitonString)
    x.Schema.For<User>().Index(fun x -> x.Username :> obj) |> ignore)
