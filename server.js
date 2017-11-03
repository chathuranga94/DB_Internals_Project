var mongoose = require('mongoose');
mongoose.connect('mongodb://localhost:27018/user_db', { useMongoClient: true });
mongoose.Promise = global.Promise;

//var Schema = mongoose.Schema;

var userSchema = mongoose.Schema({
  name: String,
  user_id: { type: Number, required: true, unique: true },
});

var User = mongoose.model('User', userSchema);

/*
// create a new user
var newUser = User({
  name: 'Peter Quill',
  user_id: 3232
});

// save the user
newUser.save(function(err) {
  if (err) throw err;

  console.log('User created!');
});

User.find({}, function(err, users) {
  if (err) throw err;

  // object of all the users
  console.log(users);
});
*/

User.find({ user_id:'3232'}, function(err, users) {
  if (err) throw err;

  console.log('FIND BY ID??? NOT _id');  
  console.log(users);
});
