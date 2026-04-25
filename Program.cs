using Microsoft.EntityFrameworkCore;
using myApp.Data;
using myApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var dataDirectory = builder.Configuration["DataDirectory"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={Path.Combine(dataDirectory, "trips.db")}"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapGet("/api/trips", async (AppDbContext db) =>
{
    var trips = await db.Trips
        .Include(trip => trip.People)
        .Include(trip => trip.Places)
        .OrderByDescending(trip => trip.CreatedAt)
        .ToListAsync();

    return Results.Ok(trips.Select(ToTripResponse));
});

app.MapPost("/api/trips", async (TripRequest request, AppDbContext db) =>
{
    var trip = FromTripRequest(request);
    db.Trips.Add(trip);
    await db.SaveChangesAsync();

    return Results.Created($"/api/trips/{trip.Id}", ToTripResponse(trip));
});

app.MapPut("/api/trips/{id:int}", async (int id, TripRequest request, AppDbContext db) =>
{
    var trip = await db.Trips
        .Include(existingTrip => existingTrip.People)
        .Include(existingTrip => existingTrip.Places)
        .FirstOrDefaultAsync(existingTrip => existingTrip.Id == id);

    if (trip is null)
    {
        return Results.NotFound();
    }

    trip.Title = CleanTitle(request.Title);
    trip.StartDate = request.StartDate.Trim();
    trip.EndDate = request.EndDate.Trim();
    trip.Rating = request.Rating.Trim();
    trip.People.Clear();
    trip.Places.Clear();

    foreach (var person in CleanPeople(request.People))
    {
        trip.People.Add(new TripPerson { Name = person });
    }

    foreach (var place in CleanPlaces(request.Places))
    {
        trip.Places.Add(place);
    }

    await db.SaveChangesAsync();

    return Results.Ok(ToTripResponse(trip));
});

app.MapDelete("/api/trips/{id:int}", async (int id, AppDbContext db) =>
{
    var trip = await db.Trips.FindAsync(id);

    if (trip is null)
    {
        return Results.NotFound();
    }

    db.Trips.Remove(trip);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

static Trip FromTripRequest(TripRequest request)
{
    return new Trip
    {
        Title = CleanTitle(request.Title),
        StartDate = request.StartDate.Trim(),
        EndDate = request.EndDate.Trim(),
        Rating = request.Rating.Trim(),
        People = CleanPeople(request.People)
            .Select(person => new TripPerson { Name = person })
            .ToList(),
        Places = CleanPlaces(request.Places)
    };
}

static TripResponse ToTripResponse(Trip trip)
{
    return new TripResponse
    {
        Id = trip.Id.ToString(),
        Title = trip.Title,
        StartDate = trip.StartDate,
        EndDate = trip.EndDate,
        Rating = trip.Rating,
        People = trip.People.Select(person => person.Name).ToList(),
        Places = trip.Places.Select(place => new TripPlaceResponse
        {
            Name = place.Name,
            Rating = place.Rating,
            Info = place.Info
        }).ToList()
    };
}

static string CleanTitle(string title)
{
    var cleanTitle = title.Trim();
    return string.IsNullOrWhiteSpace(cleanTitle) ? "Untitled Trip" : cleanTitle;
}

static IEnumerable<string> CleanPeople(IEnumerable<string> people)
{
    return people
        .Select(person => person.Trim())
        .Where(person => !string.IsNullOrWhiteSpace(person));
}

static List<TripPlace> CleanPlaces(IEnumerable<TripPlaceRequest> places)
{
    return places
        .Select(place => new TripPlace
        {
            Name = place.Name.Trim(),
            Rating = place.Rating.Trim(),
            Info = place.Info.Trim()
        })
        .Where(place => !string.IsNullOrWhiteSpace(place.Name)
            || !string.IsNullOrWhiteSpace(place.Rating)
            || !string.IsNullOrWhiteSpace(place.Info))
        .ToList();
}
