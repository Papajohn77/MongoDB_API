namespace MongoDB_API.Endpoints;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB_API.Models;
using Newtonsoft.Json.Linq;

public static class ProductEndpoints
{
    public static WebApplication AddProductEndpoints(this WebApplication app)
    {
        app.MapGet("/products_loaded", ProductsLoaded);
        app.MapGet("/products", GetAllProducts);
        return app;
    }

    static async Task<IResult> ProductsLoaded(IMongoDatabase db)
    {
        try
        {
            IMongoCollection<BsonDocument> collection = db.GetCollection<BsonDocument>("Products");
            long count = await collection.CountDocumentsAsync(new BsonDocument());
            return Results.Ok(count);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message);
        }
    }

    static async Task<IResult> GetAllProducts(IMongoCollection<BsonDocument> collection,
        double longitude, double latitude, double maxDistance, double minPrice,
        double maxPrice, int minCalories, int maxCalories)
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
                        $lookup: {
                            from: ""Products"",
                            localField: ""id"",
                            foreignField: ""store_id"",
                            as: ""products""
                        }
                    }
                "),
                BsonDocument.Parse(@"
                    {
                        $unwind: {
                            path: ""$products""
                        }
                    }
                "),
                BsonDocument.Parse($@"
                    {{
                        $match: {{
                            ""products.price"": {{ $gte: {minPrice}, $lte: {maxPrice} }},
                            ""products.calories"": {{ $gte: {minCalories}, $lte: {maxCalories} }}
                        }}
                    }}
                "),
                BsonDocument.Parse(@"
                    {
                        $addFields: {
                            ""products.distance"": ""$distance""
                        }
                    }
                "),
                BsonDocument.Parse(@"
                    {
                        $project: {
                            _id: 0,
                            ""products.id"": 1,
                            ""products.name"": 1,
                            ""products.price"": 1,
                            ""products.calories"": 1,
                            ""products.distance"": { $multiply: [""$products.distance"", 0.001] }
                        }
                    }
                "),
                BsonDocument.Parse(@"
                    {
                        $replaceRoot: {
                            newRoot: ""$products""
                        }
                    }
                ")
            };

            var resultTasks = new List<Task<Product>>();

            var cursor = await collection.AggregateAsync<BsonDocument>(pipeline);
            await cursor.ForEachAsync(product =>
            {
                var prod_id = product.GetValue("id").ToInt32();
                var prod_name = product.GetValue("name").AsString;
                var prod_price = product.GetValue("price").ToDouble();
                var prod_calories = product.GetValue("calories").ToInt32();
                var distance = product.GetValue("distance").ToDouble();

                resultTasks.Add(Task.Run(() => new Product(prod_id, prod_name, prod_price, prod_calories, distance)));
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
