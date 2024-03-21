# DocumentServer

## Document Categories
DocumentServer stores documents via Document Types, each of which has a category that indicates some general storage rules about how the document is stored and what happens to it after bieng stored. 
### Category - WORM
These documents are Write Once Read Many.  Meaning the first time they are written is the only time they can be written.  

Overwrite or updates to the same document is not allowed.  

### Category - Temporary 
These documents are considered transitory and only have a limited lifetime.  Once their lifetime is up they are automatically deleted.  The supporting app does not need to worry about them after sending them for storage, cleanup automatically happens by the DocumentServer engines.

Overwrite or updates to the same document ARE allowed.

### Category - Replaceable 
These documents can be overwritten.  Versioning of the documents does not happen.  It is simply if the supporting application sends an update to this document with the document Id the one will be replaced with this new one.

Be definition overwrite / updates ARE allowed.

### Category - Versioned???
Overwrite is NOT allowed.

## Document Storage
All documents must be associated with an Application.  An Application is just a means of segmenting a whole bunch of related documents.  

Every Application then has 1 or more Root Objects.  A Root object is an object that can have many types of documents that it wants to store, but they are related around some central record.  For instance it might be an Insurance Claim # or a Referal # or an Accounting #, Provider #, Employer #, etc, it is an Id that associates 1 or more documents to a single entity in some upstream system.  This way you can easily in the future say: "Archive all documents for the Application SuperApp that are related to Product #123" and the system will do that for every document you have associated with that Product #.  

## Entities in Document Server
There are a number of entities in the Document Server Solution that a calling application will need to be aware of.

#### Application
Every document must be associated with an Application.  Each application has a security token created for it when the Application Entity is first created.  All callers must supply this security token.  If the token does not match then the API ignores the request.

#### RootObject
In the context of Document Server a root object is a parent for one or more document types.  An application can have more than one type of Root Object.  For example an organization might have a RootObject for Employees where documents such as W2's, PIP's, Annual Reviews, etc are all associated with the Employee ID they are related to.  They might also have one for Vendors, where invoices, quotes, support contracts, etc are associated to the vendor.  

#### DocumentType
In Document Server every document is categorized into a DocumentType.  The document type determines a number of things that determine how documents associated with this Document Type are managed.
* What the lifetime of the document is.  This can be from hours to forever.  The document can then automatically be removed from the system after this lifetime has been exceeded.
* Whether the document is temporary or a more permanent document.
* Whether the document can be replaced with a newer version or all versions are kept.

#### Stored Document
The Stored Document entity stores information about a single document that is stored in the system.  At present a Stored Document can only exist on a maximum of 2 different storage nodes at a given time.  However, it can move from one node to another depending on what type of document it is as well as settings in the Document Type it is a part of.


## The DocumentServer Solution
The DocumentServer consists of the following projects

### Console Testing
Purely for testing out the basic functionality of the server.  Can be used as a sample for your own access to the DocumentServer.

### DocmentServer.API
Serves as the public API for the document server.  All external program access to the DocumentServer is via the API.

### DocumentServer.Blazor
This is the user GUI for the application.  Allows them to create DocumentTypes and other entities.

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


  # Some Statistics
  Each of these tests comprises a continuous run of sessions with a 750ms pause between sessions.  A session varies by the test, but there was a Console Program running each test simultaneously with the others.  

  The Big Files session consisted of 3 documents with sizes of 10MB, 70Mb, 23MB.  

  The Small Files session consisted of 21 documents ranging from 19KB to 2.7 MB  With the average about 500KB.  

  The All Files session consisted of all documents from the Big and Small files.  

  The Big Files was achieving a sustained throughput of over 54 MB per second. Typical session run time was about 4.5 seconds

  The Small Files was achieving a sustained throughput of over 22 MB per second.  Typical session run time was around 806ms

  The All Files was achieving a sustained throughput of over 48 MB per second.  Typical session runtime was 6.8 seconds.

  These were running in parallel to the same server from the same pc.  The number above were for over 1000 sessions for each test.

