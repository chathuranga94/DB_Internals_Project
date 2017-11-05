var mongoose = require('mongoose');
var cache = mongoose.connect('mongodb://localhost:27018/user_db', { useMongoClient: true });
var db = mongoose.connect('mongodb://localhost:27019/user_db', { useMongoClient: true });
mongoose.Promise = global.Promise;

var userSchema = mongoose.Schema({
  name: String,
  user_id: Number /*{ type: Number, required: true, unique: true },*/
});
var db_user = db.model('db_user', userSchema);
var cache_user = cache.model('cache_user', userSchema);

var getUser = function(userId){

    cache_user.findOne({ user_id: userId }, function(err, user) {
        if (err) throw err;
        else if (user) {
            console.log('cache_user found');        //else if (user) return user;
            return user;
        }
        else {
            db_user.find({}, function(err, users) {
                if (err) throw err;
                console.log(users);
            });

            /*
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
            })
            */ 
        }
    });

    console.log("getUser() ended...");
};

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
createUser(user);
getUser(100);