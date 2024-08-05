using Htmx;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder();
builder.Services.AddAntiforgery();
var app = builder.Build();

app.UseAntiforgery();

app.MapGet("/", (HttpContext context, [FromServices] IAntiforgery anti) =>
{
    var token = anti.GetAndStoreTokens(context);

    var html = $$"""
        <!DOCTYPE html>
        <html>
            <head>
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous">
                <style>
                    li{
                        cursor:pointer;
                    }
                </style>
                <meta name="htmx-config" content='{ "antiForgery": {"headerName" : "{{ token.HeaderName}}", "requestToken" : "{{token.RequestToken }}" } }'>
            </head>
            <body>
            <h1>HX-Trigger</h1>
            <p>Click on the below links to see the response.</p>
            <div class="row">
                <div class="col-md-6">
                    <ul>
                        <li hx-get="/htmx">GET</li>
                        <li hx-post="/htmx">POST</li>
                        <li hx-put="/htmx">PUT</li>
                        <li hx-patch="/htmx">PATCH</li>
                        <li hx-delete="/htmx">DELETE</li>
                    </ul>
                </div>
                <div class="col-md-6">
                    <ul>
                        <li id="get"></li>
                        <li id="post"></li>
                        <li id="put"></li>
                        <li id="patch"></li>
                        <li id="delete"></li>
                    </ul>
                </div>
            </div>

            <script src="https://unpkg.com/htmx.org@2.0.0" integrity="sha384-wS5l5IKJBvK6sPTKa2WZ1js3d947pvWXbPJ1OmWfEuxLgeHcEbjUUA5i9V5ZkpCw" crossorigin="anonymous"></script>
            <script>
                document.addEventListener("show-me", (evt) => {
                    alert("show-me event triggered");
                });

                document.addEventListener("htmx:configRequest", (evt) => {
                    let httpVerb = evt.detail.verb.toUpperCase();
                    if (httpVerb === 'GET') return;
                    
                    let antiForgery = htmx.config.antiForgery;
                    if (antiForgery) {
                        // already specified on form, short circuit
                        if (evt.detail.parameters[antiForgery.formFieldName])
                            return;
                        
                        if (antiForgery.headerName) {
                            evt.detail.headers[antiForgery.headerName]
                                = antiForgery.requestToken;
                        } else {
                            evt.detail.parameters[antiForgery.formFieldName]
                                = antiForgery.requestToken;
                        }
                    }
                });
            </script>
            </body>
        </html>
    """;
    return Results.Content(html, "text/html");
});

var htmx = app.MapGroup("/htmx").AddEndpointFilter(async (context, next) =>
{
    if (context.HttpContext.Request.IsHtmx() is false)
        return Results.Content("");

    if (context.HttpContext.Request.Method == "GET")
        return await next(context);

    await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>()!.ValidateRequestAsync(context.HttpContext);
    return await next(context);
});

htmx.MapGet("/", (HttpRequest request, HttpResponse response) =>
{
    response.Htmx(x =>
    {
        x.Retarget("#get");
    });

    return Results.Content($"GET => {DateTime.UtcNow}");
});

htmx.MapPost("/", (HttpRequest request, HttpResponse response) =>
{
    response.Htmx(x =>
    {
        x.Retarget("#post");
    });

    return Results.Content($"POST => {DateTime.UtcNow}");
});

htmx.MapDelete("/", (HttpRequest request, HttpResponse response) =>
{
    response.Htmx(x =>
    {
        x.Retarget("#delete");
    });

    return Results.Content($"DELETE => {DateTime.UtcNow}");
});

htmx.MapPut("/", (HttpRequest request, HttpResponse response) =>
{
    response.Htmx(x =>
    {
        x.Retarget("#put");
    });
    
    return Results.Content($"PUT => {DateTime.UtcNow}");
});

htmx.MapPatch("/", (HttpRequest request, HttpResponse response) =>
{   
    response.Htmx(x =>
    {
        x.Retarget("#patch");
    });

    return Results.Content($"PATCH => {DateTime.UtcNow}");
});

app.Run();