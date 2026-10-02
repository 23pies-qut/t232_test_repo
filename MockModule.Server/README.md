# This is a Cobbled Mock Module Server/API file structure breakdown and guide

MockModule.Server/
    Controllers/
    Middleware/
    Models/
    Services/
    Program.cs

Any request flows through these layers in order:

Incoming HTTP request -> Middleware -> Controller -> Service -> Response.
Models may be used throughout.


## Controllers
Define the API endpoints themselves. Each is a class that groups related endpoints and each method within handles one route.

Their role is to: recieve the request -> call a service to do the processing -> return a http response.

## Middleware
A layer between the incoming http request and the controllers. They can inspect or modify the request, respond with an error without the request ever reaching the controller or process a response from an endpoint before it leaves.  

Each middleware must be registered in Program.cs
'''app.UseMiddleware<MyMiddleware>();'''

## Models
Plain c# classes that define data shapes. They can be used throughout the backend. 

## Services
Where the business logic of the microservice lives. Controllers call services so the logic is reusable and testable rather than being stuck in only one endpoint.

Like Middleware, they must be registered in Program.cs
'''builder.Services.AddScoped<MyService>();'''
