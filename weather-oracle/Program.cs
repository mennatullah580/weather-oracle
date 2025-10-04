//using WeatherOracle.Services;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container
//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//// Register PowerService with HttpClient
//builder.Services.AddHttpClient<PowerService>();

//// Add CORS
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll", policy =>
//    {
//        policy.AllowAnyOrigin()
//              .AllowAnyHeader()
//              .AllowAnyMethod();
//    });
//});

//var app = builder.Build();

//// ⭐⭐⭐ CRITICAL: These 2 lines MUST come FIRST ⭐⭐⭐
//app.UseDefaultFiles();
//app.UseStaticFiles();

//app.UseCors("AllowAll");

//// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();
//app.UseAuthorization();
//app.MapControllers();

//// Create cache directory on startup
//var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), "cache");
//Directory.CreateDirectory(cacheDir);

//app.Run();

using WeatherOracle.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register PowerService with HttpClient
builder.Services.AddHttpClient<PowerService>();

// Add CORS - MUST allow credentials for localhost
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:7036", "https://localhost:7036")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ⭐ CORS MUST come BEFORE other middleware ⭐
app.UseCors("AllowFrontend");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Static files
app.UseDefaultFiles();
app.UseStaticFiles();

// NO HTTPS redirect for now
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

// Create cache directory on startup
var cacheDir = Path.Combine(Directory.GetCurrentDirectory(), "cache");
Directory.CreateDirectory(cacheDir);

app.Run();