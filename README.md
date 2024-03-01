# DocumentServer

## Document Categories
DocumentServer stores documents via Document Types, each of which has a category that indicates some general storage rules about how the document is stored and what happens to it after bieng stored. 
### Category - WORM
These documents are Write Once Read Many.  Meaning the first time they are written is the only time they can be written.  Overwrite or updates to the same document is not allowed.  

### Category - Temporary 
These documents are considered transitory and only have a limited lifetime.  Once their lifetime is up they are automatically deleted.  The supporting app does not need to worry about them after sending them for storage, cleanup automatically happens by the DocumentServer engines.

### Category - Replaceable 
These documents can be overwritten.  Versioning of the documents does not happen.  It is simply if the supporting application sends an update to this document with the document Id the one will be replaced with this new one.

### Category - Versioned???

## Document Storage
All documents must be associated with an Application.  An Application is just a means of segmenting a whole bunch of related documents.  

Every Application then has 1 or more Root Objects.  A Root object is an object that can have many types of documents that it wants to store, but they are related around some central record.  For instance it might be an Insurance Claim # or a Referal # or an Accounting #, Provider #, Employer #, etc, it is an Id that associates 1 or more documents to a single entity in some upstream system.  This way you can easily in the future say: "Archive all documents for the Application Unity that are related to Claim 123" and the system will do that for every document you have associated with that claim #.  


## The DocumentServer Solution
The DocumentServer consists of the following projects

### Console Testing
Purely for testing out the basic functionality of the server.  Can be used as a sample for your own access to the DocumentServer.

### DocmentServer.API
Serves as the public API for the document server.  All external program access to the DocumentServer is via the API.

### DocumentServer.ClientLibrary
This is all the interfaces to calling the API that a client application would need.  Simplifies the code that a client app needs to implement.

### DocumentServer.Core
Contains the core business logic of the DocumentServer.  No business logic is in the API.

### SlugEnt.DocumentServer.Db
Contains the EF Core DatabaseContext, Database Migrations, etc.

### DocumentServer.Models
Contains the EF Core Models and some other common data objects.

### Test_DocumentServer 
All of the Unit Tests

WORM Cannot be replaced, Cannot be deleted
Temporary - Can be replaced, can be deleted.
Replacable - Can be replaced, cannot be deleted



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

