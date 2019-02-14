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
    //using (var stream = file.OpenReadStream())
    //{
    //    loader.LoadData(conferenceName, stream, _db);
    //    await loader.LoadDataAsync(conferenceName, stream, _db, cancellationToken);
    //}

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

Run the application and use the Swagger UI to upload the `.\lab\BackEnd\Data\Import\NDC_London_2019.json` file to the `/api/Conferences/upload` API. Let's give the conference the name `NDCLondon`.

### Change startup project

Change the startup project to launch both the FrontEnd and the BackEnd

![Change Startup Project](./screenshots/startup.PNG)