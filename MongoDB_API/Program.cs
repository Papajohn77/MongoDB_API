using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB_API.Endpoints;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(_ => new MongoClient(
    builder.Configuration.GetConnectionString("DefaultConnection")
));
builder.Services.AddSingleton(
    provider => provider.GetRequiredService<MongoClient>().GetDatabase("Benchmark")
);
builder.Services.AddSingleton(
    provider => provider.GetRequiredService<IMongoDatabase>().GetCollection<BsonDocument>("Stores")
);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.AddStoreEndpoints();
app.AddProductEndpoints();
app.Run();
