# DocumentServer

## Unit Testing
### ENABLE_TRANSACTIONS
There are a few things to understand about the unit tests.  By default all unit tests are run in a transaction.  This means they can operate on the same base data and tables without affecting each other.  Also, the transaction is never committed.  The downside to this is if you want to see what the database tables look like you will be unable.  

For these special cases where you need to debug a specific unit test, you can turn off the transaction behavior.  The setting is a #define in the DocumentServer_Test project:
  
  DocumentServer_Test/SupportObjects/SupportMethods.cs class.
  
  .#define ENABLE_TRANSACTIONS

  There is a #define called ENABLE_TRANSACTIONS.  IF that is defined then you will not be able to see any of the data in the database.  #undef ENABLE_TRANSACTIONS to disable it and allow transactions to commit to database.


  ### RESET_DATABASE
  Another feature that can be turned on or off is the clearing of the database at the start of each Unit Test run.  This is normally preferred as it allows for a constant starting point for the database in a known state.  However, sometimes if debugging a particular piece of code it can unnecessarily delay the start of the test.  This can be turned off with the #define RESET_DATABASE

  It is located in the following folder:

  DocumentServer_Test\SupportObjects\DatabaseSetup_Test.cs file.  

  Just #undef it to to turn off the database reset.

