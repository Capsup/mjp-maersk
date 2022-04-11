MJP Mearsk Offline Test
=========
A solution for the Maersk Offline test, created by Martin Juul Petersen.


Foreword
----
I've got to admit that I was a little puzzled by the problem definition, and I'm still unsure if it was deliberately part of the test or not. 
The problem definition states:
```
More specifically, your
API needs an endpoint that returns the last 10 prices for containers booked on a given voyage and an
endpoint through which you can register a new booking price.
```
vs what is defined in the endpoints section
```
The API should have the following two endpoints:
[POST] UpdatePrice(string voyageCode, decimal price, Currency currency, DateTimeOffset timestamp)
Example: UpdatePrice("451S", 109.5, Currency.Gbx, DateTimeOffset.Now)

[GET] GetAverage(string voyageCode, Currency currency)
Example: GetAverage("451S", Currency.Gbx) --> 152.35
```

Wat?

There seems to be no endpoint that is supposed to return the _prices_ of the bookings. But then, does GetAverage return the average of the last 10 prices? If not, and it returns the average of _all_ bookings, why is a DateTimeOffset input then defined if it's not supposed to be used? 

Anyways, if this was a real life situation, I'd sprint back to a Product Owner and start asking questions. Because something is definitely wrong here. However, in this case, I do not have access to such a person.
So instead, I'm going to assume that there's supposed to be a third endpoint, one which I've dubbed GetLastPrices:
```
[GET] GetLastPrices(string voyageCode, CurrencyEnum currency, int count = 10)
```
And I'm also assuming that GetAverage needs to return the average for _all_ existing container bookings for the given voyageCode. 

Usage
----
Docker makes it easy to run the solution, assuming a Docker engine is installed locally. On Windows, Docker Desktop is quick and easy to install from [https://docs.docker.com/desktop/windows/install/](here).
Make sure the Git repo has been cloned into a local folder using `git clone` and navigate into the cloned folder. You should now be inside the folder that contains the .sln file.

Docker
------
Using Docker, to run the solution, at the root folder of the Git repository, you'll need to build the local Docker image. This can be done like so:
```bash
docker build -f ./MJP.MaerskOfflineTest/Dockerfile -t mjp.test .
```
And, once built, started up like so:
```bash
docker run -it --rm -p 5051:80 -e ASPNETCORE_ENVIRONMENT=Development mjp.test
```
If you're doing this in a Git Bash prompt, you might have to prefix the above command with `winpty`. But the Prompt should tell you if needed.

Once the Docker container is running, you should be able to hit the Swagger UI at the following URL: `http://localhost:5051/swagger`. 

Dotnet
------
Using the Dotnet CLI, requires having installed the [net6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) on the host machine.

With the Dotnet CLI installed, the application can be started using:
```bash
dotnet run -c Debug --project MJP.MaerskOfflineTest/MJP.MaerskOfflineTest.csproj
```

This will start up the swagger UI at the following URL: `http://localhost:5051/swagger`. 

Unit tests
--------
With the Dotnet CLI, unit tests can also be executed using the Dotnet CLI test runner with just `dotnet test` inside the root folder.


Application
----
I am deliberately making using of Swagger to make it easy to test the API. Who doesn't like using a slightly fancy UI rather than manual curl calls?
However, if you're the type to do so, the application API can be hit at `http://localhost:5051/api/Voyage` instead. 

An OpenAPI specification of the API can be found in the file `swagger.json` for even heavier manual testing of the API.

3 endpoints are implemented:
```
[POST] UpdatePrice(string voyageCode, decimal price, CurrencyEnum currency, DateTimeOffset timestamp)
[GET] GetAverage(string voyageCode, CurrencyEnum currency)
[GET] GetLastPrices(string voyageCode, CurrencyEnum currency, int count = 10)
```

Architecture
----
I use a very standard ASP.Net Core setup, making use of as much default functionality as possible. This includes the built-in IoC container and the built-in ILogger implementation from Microsoft.Extensions.Logging.

The solution is split up into two different projects, the main API project using Kestrel as a webserver inside the `MJP.MaerskOfflineTest` folder. The other project is a UnitTest project using MSTest inside the `MJP.MaerskOfflineTest.Tests` folder.

Inside the API project, the project is split up into a few different folders:  
    .  
    ├── Controllers             # The implementation of the Controllers for the API  
    ├── Models                  # Regular old DTOs are implemented here  
    ├── Services                # Implementation of services  
    │   └──Interfaces           # Interfaces for the services  
    └── program.cs              # The main program, starting the Kestrel webserver and configuring the IoC container among other middleware.  

Services use both an interface and a concrete implementation, to make it easier to use them in the IoC container as well as allowing unit testing of each component, separate from each other. It also reduces coupling.

The DatabaseService handles the CRUD implementation of a really simple in-memory database using a ConcurrentDictionary. The test mentioned that it read operations were more important than inserting, so I've tried to keep this in mind. That's why I keep a CumulativeAverageUSD value for each voyage, which is updated on all insertions into the database. This means that it is a constant O(1) time complexity to get the average of _all_ prices for a particular voyage.
To optimize reading the last x prices of the container bookings, I keep the in-memory list sorted. This makes insertion into the database even slower due to an additional O(log n) operation to find and insert new bookings, but it makes reading the prices back a lot faster. Using a ConcurrentDictionary is probably neither needed (as per the requirements) nor good for performance, but hey, it's thread-safe. Which is relevant since I use the DatabaseService as a singleton in the IoC container and the instance keeps the state internally. I decided against using something like in-memory EF Core because I didn't want to deal with SQL schemas for this particular project. It just seemed way overkill.

The ExchangeRateService handles the CRUD implementation of the currencies. It also has the manual definition of each exchange rate. Making use of the `NodaMoney` package made this implementation a lot simpler, because it takes care of all the +-/* operations and handles the conversions.

I use a enum by the name of CurrencyEnum to manually define the valid currencies. I make use of the key values of this Enum, rather than the actual values, because Enum's in C# don't allow me to use a string as a backing value. It just seemed convenient, I'm sure there's better solutions.

Each HTTP method implementation in the `VoyageController.cs` is deliberately kept pretty simple. There's some input validation (because never trust the user right?), a little exception handling and then mostly a call directly into the DatabaseService. Some Currency handling too. This makes it a lot easier for me to re-use functionality between each HTTP method, since most functionality is abstracted away into the two service implementations. I personally think it makes the code that much more readable too. I'm sure it lends itself well towards being mindful of the SOLID principles as well.

I deliberately allow any exceptions that might occur to bubble to the top of the stack. ASP.NET has a super convenient exception serializer and I quite liked the data it returned. So if an internal error happens, it will be displayed to the end user. In a real application, I'd have more focus on not leaking internal data through stack traces and what not. In this case however, it just served as a convenient way to show error messages rather than implementing many possible HTTP return codes.

Security
-----
There's no kind of authentication, authorization or worry about security in this particular implementation. That's also why I use the Development config for Kestrel, because I found it useful to get full exceptions returned to the user. 

Tests
-----
I'm a huge fan of TDD and BDD. The way I personally implement unit tests is to create the "contract" that define the _business logic_ for the subject I want to implement. Not sheepishly go through every single possible branch in the code to test every single exception or edge case. I personally use unit tests as a tool to verify that all of the assumptions I've made, still exist within the implementation. This helps me refactor the code at a later date and trust, that the new implementation handles the same as the old.

I'm sure there are many more positive and negative path unit tests that could be added additionally, but I believe I've got the point across of how I make unit tests. It's a test after all, not a production-ready API.

External NuGet packages
-----
I make use of a few NuGet packages. The only one I'll mention from the API project is the `NodaMoney` package which I use to implement the Money pattern as recommended by [Martin Fowler](https://martinfowler.com/eaaCatalog/money.html). This packages solves decimal-related conversions and makes sure that no two different Currencies are added together at any point.

In the Tests project, there's quite a few more. I make use of:
- Moq: for mocking the different dependencies that each Service may have.
- AutoFixture: for easily creating test data and reducing coupling between the unit tests and the concrete implementations of related dependencies. It is especially relevant in the `VoyageControllerTests.cs` where the Controller gets tested. Autofixture allows me to create an instance of the controller with ease and handles creating mocks of depencies automatically.
- FluentAssertations: I personally quite like Fluent APIs because they feels a lot more natural. This also helps the tests become more BDD-like. 
