using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Floom.Misc
{
    public class DBInitializer
    {
        private readonly IMongoClient _client;

        public DBInitializer(IMongoClient client)
        {
            _client = client;
        }

        public void Initialize(string database)
        {
            // List all databases to check if "Floom" exists
            var dbList = _client.ListDatabases().ToList().Select(db => db["name"].AsString);

            // If the "Floom" database does not exist, it will be implicitly created
            if (!dbList.Contains(database))
            {
                Console.WriteLine("Database Floom does not exist. Creating it...");
                var db = _client.GetDatabase(database);
                var collection = db.GetCollection<dynamic>("DummyCollection");

                // Inserting a dummy record to actually create the database
                collection.InsertOne(new { DummyField = "DummyValue" });

                // Deleting the dummy record immediately
                collection.DeleteOne(Builders<dynamic>.Filter.Eq("DummyField", "DummyValue"));

                Console.WriteLine("Database Floom created.");
            }
            else
            {
                Console.WriteLine("Database Floom already exists.");
            }



            // Connect to MongoDB

            // Access the admin database to execute createUser command
            //var adminDatabase = _client.GetDatabase("admin");

            //// Check if user exists
            //var userExists = DoesUserExist(adminDatabase, "newUser", "Floom");

            //// Create the user if they do not exist
            //if (!userExists)
            //{
            //    var command = new BsonDocument
            //{
            //    {"createUser", "FloomUser"},
            //    {"pwd", "MyFloom"},
            //    {"roles", new BsonArray
            //        {
            //            new BsonDocument
            //            {
            //                {"role", "readWrite"},
            //                {"db", "Floom"}
            //            }
            //        }
            //    }
            //};
            //    var result = adminDatabase.RunCommand<BsonDocument>(command);
            //    Console.WriteLine("User created: " + result);
            //}
            //else
            //{
            //    Console.WriteLine("User already exists.");
            //}

            //// Create the Floom database by inserting a dummy document and immediately removing it
            //var floomEngineDatabase = _client.GetDatabase("Floom");
            //var collection = floomEngineDatabase.GetCollection<BsonDocument>("dummyCollection");
            //collection.InsertOne(new BsonDocument("name", "dummy"));
            //collection.DeleteOne(new BsonDocument("name", "dummy"));

            ////Delete the collection

            //Console.WriteLine("Floom database created.");
        }

        // Function to check if a user already exists in a given database
        static bool DoesUserExist(IMongoDatabase adminDatabase, string username, string dbName)
        {
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("user", username),
                Builders<BsonDocument>.Filter.Eq("db", dbName)
            );

            var usersCollection = adminDatabase.GetCollection<BsonDocument>("system.users");
            var user = usersCollection.Find(filter).FirstOrDefault();

            return user != null;
        }
    }
}
