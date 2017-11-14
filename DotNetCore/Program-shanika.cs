// using System;
// using MongoDB.Bson;
// using MongoDB.Driver;
// using MongoDB.Bson.Serialization;


// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// //using FluentAssertions;

// using System.Diagnostics;
// using Microsoft.Extensions.Caching.Distributed;
// using Microsoft.Extensions.Caching.Redis;
// using System.Text;


// namespace DotNetCore
// {
//     class Program
//     {
//         private static IMongoClient _dbClient;
//         private static IMongoDatabase _db;
//         private static IMongoCollection<BsonDocument> _dbUser;

//         private static IMongoClient _cacheClient;
//         private static IMongoDatabase _cache;
//         private static IMongoCollection<BsonDocument> _cacheUser;

//         private static RedisCache _redis_cache;



//         public static int InCACHE; 
//         public static int InDB;
        
//         static void Main(string[] args)
//         {
//             Console.Write("Start Program\n");
//             _dbClient = new MongoClient("mongodb://localhost:27020");
//             _db = _dbClient.GetDatabase("userdb");
//             _dbUser = _db.GetCollection<BsonDocument>("db_users");
            
//             _cacheClient = new MongoClient("mongodb://localhost:27018");
//             _cache = _cacheClient.GetDatabase("userdb");
//             _cacheUser = _cache.GetCollection<BsonDocument>("db_users");

//             _redis_cache = new RedisCache(new RedisCacheOptions
//                 {   Configuration = "localhost",  InstanceName = "db_users" }
//             );


//             /* 
//             int user_id = 230;
//             String user_name = "Sammy";

//             var new_user_document = new BsonDocument
//             {
//                 { "name", user_name },
//                 { "user_id", user_id }
//             };
//             AddUser(new_user_document);

//             GetUser_WithCache(200);
//             GetUser_WithCache(230);
//             GetUser_WithCache(250);
//             */


//             /* 
//                     var new_user = new user();
//                     new_user.name = "bibi";
//                     new_user.user_id = 250;

//                     _cacheUser.InsertOne(new_user);
//                     */

//                     /* 
//                     var cursor = collection.Find(new BsonDocument()).ToCursor();
//                     foreach (var document in cursor.ToEnumerable())
//                     {
//                         Console.WriteLine(document);   
//                     }
//             */

//             Random rnd = new Random();
//             int getIndexes = rnd.Next(1, 100); 
//             int setIndexes = rnd.Next(1, 1000);

//             List<int> getId_List = new List<int>();
            
//             for(int times=0; times<10000; times++){
//                 int random = rnd.Next(1, 100);
//                 //  Console.Write(random + " ");
//                 getId_List.Add(random);
//             }  

//             Stopwatch sw = new Stopwatch();

//             /*
//             for(int id=1 ; id<=1000; id++){
//                 var new_doc = new BsonDocument
//                 {
//                     { "name", "bibi"+id },
//                     { "user_id", id }
//                 };
//                 AddUser(new_doc);
//             }
//             */
            

//             InDB = 0;   InCACHE = 0;
//             sw.Start();
//             foreach(int id in getId_List){
//                 //GetUser_NoCache(id);
//                 //GetUser_RedisCache(id);
//                 //GetUser_WithCache(id);
//             }
//             sw.Stop();
//             Console.WriteLine("\nTIME => {0}",sw.Elapsed);
//             Console.WriteLine("IN DB: " + InDB + "  IN CACHE: " + InCACHE);

//         }

//         public static String GetUser_RedisCache(int user_id){

//             //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);

//             var _InREDIS = _redis_cache.Get(user_id.ToString());
//             if(_InREDIS != null)
//             {
//                 InCACHE++;
//                 //  Console.WriteLine("IN THE CACHE...\n");
//                 return Encoding.UTF8.GetString(_InREDIS); 
//             } 
//             else 
//             {
//                 //  Console.WriteLine("NOT IN THE CACHE...");
//                 var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
//                 var db_query = _dbUser.Find(filter);
//                 if( (int)db_query.Count()>0)
//                 {
//                     InDB++;
//                     String db_user_str = db_query.First().ToString();
//                     //  Console.WriteLine("IN THE DATABASE...");
//                     _redis_cache.Set(user_id.ToString(), Encoding.UTF8.GetBytes(db_user_str), new DistributedCacheEntryOptions());
//                     //  Console.WriteLine("ADDED TO THE CACHE...\n");
//                     return db_user_str;
//                 }
//                 else 
//                 {
//                     //  Console.WriteLine("NOT IN THE DATABASE AS WELL...\n");
//                     return "";
//                 }
//             }
//         }

//         public static BsonDocument GetUser_WithCache(int user_id){

//             //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);

//             var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
//             var cache_query = _cacheUser.Find(filter);

//             if((int)cache_query.Count()>0)
//             {
//                 InCACHE++;
//                 //  Console.WriteLine("IN THE CACHE...\n");
//                 return cache_query.First();  //.ToList() and .Count
//             } 
//             else 
//             {
//                 InDB++;
//                 //  Console.WriteLine("NOT IN THE CACHE...");
//                 var db_query = _dbUser.Find(filter);
//                 if( (int)db_query.Count()>0)
//                 {
//                     //  Console.WriteLine("IN THE DATABASE...");
//                     _cacheUser.InsertOne(db_query.First());
//                     //  Console.WriteLine("ADDED TO THE CACHE...\n");
//                     return db_query.First();

//                     /* Is ObjectId set too?

//                     var queried_user = (User)db_query.First(); 
//                     var new_user_doc = new BsonDocument
//                     {
//                         { "name", queried_user.user_name },
//                         { "user_id", queried_user.user_id }
//                     };
//                     _cacheUser.InsertOne(new_user_doc);
//                     */
//                 }
//                 else 
//                 {
//                     //  Console.WriteLine("NOT IN THE DATABASE AS WELL...\n");
//                     return new BsonDocument();
//                 }
//             }
//             // ??? return new BsonDocument();
//         }

//         public static BsonDocument GetUser_NoCache(int user_id){

//             //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);

//             var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
//             var db_query = _dbUser.Find(filter);

//             if((int)db_query.Count()>0)
//             {
//                 InDB++;
//                 //  Console.WriteLine("IN THE DATABASE...\n");
//                 return db_query.First();  
//             } 
//             else 
//             {
//                 //  Console.WriteLine("NOT IN THE DATABASE...\n");
//                 return new BsonDocument();
//             }
//         }

//         public static BsonDocument UpdateUser_RedisCache(int user_id){
//             //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);
//             String timestamp = GetTimestamp(DateTime.Now);
//             var _InREDIS = _redis_cache.Get(user_id.ToString());
//             if (_InREDIS != null) {
//                 InCACHE++;
//                 _redis_cache.Remove(user_id.ToString());
//                 String user = Encoding.UTF8.GetString(_InREDIS);
//                 BsonDocument db_user = BsonSerializer.Deserialize<BsonDocument>(user);
//                 db_user.Add("updated_at", timestamp);
//                 user = db_user.ToString();
//                 _redis_cache.Set(user_id.ToString(), Encoding.UTF8.GetBytes(user), new DistributedCacheEntryOptions());
//             }
            
//             var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
//             var update = Builders<BsonDocument>.Update.Set("updated_at", timestamp);

//             var db_update_result = _dbUser.UpdateOne(filter, update);
            
//             if(_InREDIS == null)
//             {
//                 InDB += (int)db_update_result.ModifiedCount;
//             } 
//             return db_update_result.ToBsonDocument();
//         }

//         public static BsonDocument UpdateUser_WithCache(int user_id){
//             //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);
//             var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
//             var update = Builders<BsonDocument>.Update.Set("updated_at", GetTimestamp(DateTime.Now));
            
//             var cache_update_result = _cacheUser.UpdateOne(filter, update);
//             var db_update_result = _dbUser.UpdateOne(filter, update);
            
//             if(cache_update_result.ModifiedCount > 0)
//             {
//                 InCACHE += (int)cache_update_result.ModifiedCount;
//                 //  Console.WriteLine("IN THE CACHE...\n");
//                 return cache_update_result.ToBsonDocument();  //.ToList() and .Count
//             } 
//             else
//             {
//                 InDB += (int)db_update_result.ModifiedCount;
//                 //  Console.WriteLine("NOT IN THE CACHE...");
//                 return db_update_result.ToBsonDocument();
//             }
//         }

//         public static BsonDocument UpdateUser_NoCache(int user_id){
//             //  Console.WriteLine("SEARCH FOR USER_ID: " + user_id);
//             var filter = Builders<BsonDocument>.Filter.Eq("user_id", user_id);
//             var update = Builders<BsonDocument>.Update.Set("updated_at", GetTimestamp(DateTime.Now));
            
//             var update_result = _dbUser.UpdateOne(filter, update);
            
//             InDB += (int)update_result.ModifiedCount;
//             return update_result.ToBsonDocument();  
//         }

//         public static Boolean AddUser(BsonDocument new_user){

//             _dbUser.InsertOne(new_user);

//             return true;
//         }

//         /*
//         public class User : BsonDocument{
//             public string user_name;
//             public int user_id;
//         }
//         */

//         public static String GetTimestamp(DateTime value) {
//             return value.ToString("yyyyMMddHHmmssffff");
//         }
//     }
// }