# Workshop2019

## What you'll be building

In this workshop, we'll start with sample ASP.NET Core application. This application comprises of an API-backend, a web front-end application, and a common library for shared data transfer objects.

However, this application is riddled with anti-patterns that prevent this application from scaling well. Due course of this workshop, we'll attempt to fix some of the issue with the application.

## Getting started

To begin with let's ensure we cloning and trying the build the starting point of the project. Start by navigating to the `./lab` directory and building the solution

```powershell
git clone https://github.com/shirhatti/ReadyWorkshop2019.git
cd .\ReadyWorkshop2019\lab\
dotnet build
```

At this point you should able to successfully launch Visual Studio

```powershell
.\ConferencePlanner.sln
```

If everything is looking good so far, navigate to the Backend project and run the following commands in the command prompt to setup your database correctly

```powershell
cd .\ReadyWorkshop2019\lab\BackEnd
dotnet ef migrations add Initial
dotnet ef database update
```

## Upload the initial data

Instead of using Entity Framework to seed the data, let's use an existing API endpoint that we have that allows us to upload a conference file format.

We will be using the existing `/api/Conferences/upload` endpoint to `POST` a file containing the conference schedule.

Let's a closer look at this method

```csharp
[HttpPost("upload")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadConference([Required, FromForm]string conferenceName, IFormFile file, CancellationToken cancellationToken)
{
    var loader = new SessionizeLoader();

    using (var ms = new MemoryStream())
    {
        file.CopyTo(ms);
        ms.Position = 0;
        await loader.LoadDataAsync(conferenceName, ms, _db);
    }

    await _db.SaveChangesAsync();

    return Ok();
}
```

While on first glance there isn't anything wrong this method, on closer inspection we notice that we can directly access the underlying stream via `file.OpenReadStream()`. While in our case, there isn't of much impact, it could be problematic when dealing with larger files. By default when reading any single file larger than 64KB, it will be moved from RAM to a temp file. By copying it into a MemoryStream, we just undid all hard work done by the framework for us.

Let's go ahead and fix this code as shown below.

```csharp
[HttpPost("upload")]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadConference([Required, FromForm]string conferenceName, IFormFile file, CancellationToken cancellationToken)
{
    var loader = new SessionizeLoader();

    using (var stream = file.OpenReadStream())
    {
       loader.LoadData(conferenceName, stream, _db);
       await loader.LoadDataAsync(conferenceName, stream, _db, cancellationToken);
    }

    await _db.SaveChangesAsync();

    return Ok();
}
```

At this point we're ready to import data.

Run the application and use the Swagger UI to upload the `.\lab\BackEnd\Data\Import\NDC_London_2019.json` file to the `/api/Conferences/upload` API. Let's give the conference the name `NDCLondon`.

You can use the Swagger UI and verify that your upload was successful by trying a `GET` request on `/api/Conferences`. We should see the conference with the name `NDCLondon` that we just created.

## Explore

Now that we have some initial data and our database is seeded, trying playing around with app and get a feel for what is happening. I recommend changing your startup project in Visual Studio to launch both the FrontEnd and the Backend.

### Change startup project

Change the startup project to launch both the FrontEnd and the BackEnd

![Change Startup Project](./screenshots/startup.PNG)

## Cancelling Tasks

Let's us turn our attention over to the `SearchController` in the backend. Search operations can be expensive and we want limit the thrashing of our database. Therefore in the `SearchController` we have implemented Timeouts on expensive database queries. The `TimeoutAfter` method has been implemented as an extension method that hangs off of `Task`. Let look at how's it's being used in the Controller.

```csharp
...
var sessionResults = await sessionResultsTask.TimeoutAfter(TimeSpan.FromSeconds(1));
var speakerResults = await speakerResultsTask.TimeoutAfter(TimeSpan.FromSeconds(1));
...
```

As evident from the usage, we want our expensive database queries to timout after 1 seconds. Let's look at the actual implementation of how we've achieved this.

```csharp
public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
{
    var delayTask = Task.Delay(timeout);

    var resultTask = await Task.WhenAny(task, delayTask);
    if (resultTask == delayTask)
    {
        // Operation cancelled
        throw new OperationCanceledException();
    }

    return await task;
}
```

While naively this might look like it works, we're not cancelling the delayTask we're creating even when our operation successfully completes. This even we could easily end up with timer queue flooding especially if this occurs on the hot path. The right approach here is to ensure the timer is being disposed of.

```csharp
public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
{
    using (var cts = new CancellationTokenSource())
    {
        var delayTask = Task.Delay(timeout, cts.Token);

        var resultTask = await Task.WhenAny(task, delayTask);
        if (resultTask == delayTask)
        {
            // Operation cancelled
            throw new OperationCanceledException();
        }
        else
        {
            // Cancel the timer task so that it does not fire
            cts.Cancel();
        }

        return await task;
    }
}
```

## Prevent port exhaustion

Let's jump over to our FrontEnd to look at this problem. We've registered a scoped service `ApiClient` to make it easier to make API calls to our backend. The `ApiClient` class abstract aways the underlying HTTP semanticcs and exposes an easy to use API in all our controllers.

Let's look at what we're doing in the Contructor of this service.

```csharp
public ApiClient(IOptions<ApiClientOptions> options)
{
    _httpClient = new HttpClient
    {
        BaseAddress = options.Value.BaseAddress
    };
}
```
Since the ServiceContainer ensures our scoped services get disposed for us, we're effectively disposing of HttpClient for every request.

Despite the fact `HttpClient` implements the `IDisposable`, it should not be disposed of. Although it is re-entrant, the superior way to use it is pool the underlying `HttpMessageHandler` that owns the TCP socket and just create a new HttpClient using a pooled instance of the `HttpMessageHandler`. When you dispose of a client instance the underlying HttpMessageHandler is disposed. Once that happens and you initiate closing the TCP socket, you're waiting for OS timeout (2 minutes) for the socket to have gone from `TIME_WAIT` to closed. Though HttpClient is thread-safe, you don't want to be using a singleton since that ensures that DNS won't get re-resolved. If your app stays enough long enough, you may no longer be able to connect to destination server since you don't respect DNS TTL.

Conveniently enough, we ship a `HttpClientFactory` that you can use that does all this for you.

To switch to HttpClientFactory, add it to your `ConfigureServices` method in `Startup.cs`. 

```csharp
services.AddHttpClient<IApiClient, ApiClient>();
```

You should also **REMOVE** the call to add your existing client as a scoped service.

```diff
- services.AddScoped<IApiClient, ApiClient>();
```

We'll also need to change the constructor of the ApiClient to now accept an `HttpClient` via constructor injection instead.

```csharp
public ApiClient(HttpClient httpClient, IOptions<ApiClientOptions> options)
{
    _httpClient = httpClient;
    _httpClient.BaseAddress = options.Value.BaseAddress;
}
```