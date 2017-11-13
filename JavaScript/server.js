var mongoose = require('mongoose');
var cache = mongoose.connect('mongodb://localhost:27018/userdb', { useMongoClient: true });
var db = mongoose.connect('mongodb://localhost:27020/userdb', { useMongoClient: true });
mongoose.Promise = global.Promise;

var userSchema = mongoose.Schema({
  name: String,
  user_id: Number /*{ type: Number, required: true, unique: true },*/
});
var db_user = db.model('db_user', userSchema);
var cache_user = cache.model('cache_user', userSchema);

/*
let getUser = async (userId) => {

    cache_user.findOne({ user_id: userId }, function(err, user) {
        if (err) throw err;
        else if (user) {
            console.log('cache_user found');        //else if (user) return user;
            return user;
        }
        else {

            console.log(await db_user.find({}))

            
            db_user.findOne({ user_id: userId }, function(err,user){
                console.log(user);
                if (err) throw err;
                else if (user) {
                    console.log('db_user found');
                    new cache_user(user).save(function(err){
                        if (err) throw err;
                        console.log('cache_user created'); 
                        return user;
                    });
                    // return user;
                }
            });
            
        }
    });

    console.log("getUser() ended...");
};
*/

let getInfo = async (userId) => {
 //console.log(await cache_user.findOne({ user_id: userId }))
 console.log(await db_user.find({}))
 return 'all done';
}


var createUser = function(user){

    new db_user({
    name: user.name,
    user_id: user.user_id
    }).save(function(err) {
        if (err) throw err;
        console.log('db_user created');
    });

};

//  findOne      findById        find        findAll

var user = {name: 'Udara',user_id: 100};
//createUser(user);
//console.log(getUser(100));

//console.log(getInfo(100));
//getInfo(100);

/*
db_user.find({}, function(err, users) {
    if (err) throw err;
    console.log(users);
});
*/


/*
var query = db_user.find({});

var promise = query.exec();
promise.then(function(doc){
    console.log("QUERY...");
    console.log(doc);
});
*/

let get_func = async(userId) => {

        let is_cached = await cache_user.findOne({ user_id: userId }).exec();

        if(!is_cached){
            console.log("NOT CACHIED");
            let is_dbuser = await db_user.find({}).exec();
            console.log(is_dbuser);

        } else {
            console.log("CACHED");
        }

        console.log("LEAVING get_func")
};

get_func(100);

/*
db_user.find({}, function(err, users) {
    if (err) throw err;
    console.log(users);
});
*/