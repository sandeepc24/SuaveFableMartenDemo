module ServerCode.Auth

open Suave
open Newtonsoft.Json
open Suave.RequestErrors
open Database
open System.Linq
open Marten
open System

let unauthorized s = Suave.Response.response HTTP_401 s

let UNAUTHORIZED s = unauthorized (UTF8.bytes s)

let login (ctx: HttpContext) = async {
    let login =
        ctx.request.rawForm 
        |> System.Text.Encoding.UTF8.GetString
        |> JsonConvert.DeserializeObject<Domain.Login>

    use session = store.QuerySession()
    let user = session.Query<User>().Where(fun x -> x.Username = login.UserName && x.Password = login.Password)
    if user.Count() = 0 then
        return! UNAUTHORIZED (sprintf "Invalid username or password.") ctx
    else
        let user : ServerTypes.UserRights = { UserName = login.UserName }
        let token = TokenUtils.encode user

        return! Successful.OK token ctx
}

let signup(ctx : HttpContext) = async {
    let signup = 
        ctx.request.rawForm 
        |> System.Text.Encoding.UTF8.GetString
        |> JsonConvert.DeserializeObject<Domain.Signup>

    use session = store.OpenSession()
    let user = session.Query<User>().Where(fun x -> x.Username = signup.Username)
    if user.Count() <> 0 then
        return! CONFLICT ("That username is taken. Please try another one.") ctx
    else
        let newUser = { Id = Guid.NewGuid(); Username = signup.Username; Password = signup.Password }
        session.Store newUser
        session.SaveChangesAsync() |> ignore
        return! Successful.OK (TokenUtils.encode "") ctx
}


let useToken ctx f = async {
    match ctx.request.header "Authorization" with
    | Choice1Of2 accesstoken when accesstoken.StartsWith "Bearer " -> 
        let jwt = accesstoken.Replace("Bearer ","")
        match TokenUtils.isValid jwt with
        | None -> return! FORBIDDEN "Accessing this API is not allowed" ctx
        | Some token -> return! f token
    | _ -> return! BAD_REQUEST "Request doesn't contain a JSON Web Token" ctx
}
