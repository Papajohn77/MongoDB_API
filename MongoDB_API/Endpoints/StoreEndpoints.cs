namespace MongoDB_API.Endpoints;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB_API.Models;
using Newtonsoft.Json.Linq;

public static class StoreEndpoints
{
    public static WebApplication AddStoreEndpoints(this WebApplication app)
    {
        app.MapGet("/stores", GetAllStores);
        return app;
    }

    static async Task<IResult> GetAllStores(IMongoCollection<BsonDocument> collection,
        double longitude, double latitude, double maxDistance)
    {
        try
        {
            var coordinatesArray = new JArray(
                new JValue(longitude),
                new JValue(latitude)
            );

            var pipeline = new BsonDocument[] {
                BsonDocument.Parse($@"
                    {{
                        $geoNear: {{
                            near: {{ type: ""Point"", coordinates: {coordinatesArray} }},
                            spherical: true,
                            maxDistance: {maxDistance * 1000},
                            distanceField: ""distance""
                        }}
                    }}
                "),
                BsonDocument.Parse(@"
                    {
                        $project: {
                            _id: 0,
                            ""id"": 1,
                            ""name"": 1,
                            ""distance"": { $multiply: [""$distance"", 0.001] }
                        }
                    }
                ")
            };

            var resultTasks = new List<Task<Store>>();

            var cursor = await collection.AggregateAsync<BsonDocument>(pipeline);
            await cursor.ForEachAsync(store =>
            {
                var store_id = store.GetValue("id").ToInt32();
                var store_name = store.GetValue("name").AsString;
                var distance = store.GetValue("distance").ToDouble();

                resultTasks.Add(Task.Run(() => new Store(store_id, store_name, distance)));
            });

            var result = await Task.WhenAll(resultTasks);

            return Results.Ok(result.ToList());
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }
}
