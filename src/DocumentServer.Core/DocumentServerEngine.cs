using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SlugEnt.DocumentServer.ClientLibrary;
using SlugEnt.DocumentServer.Core;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.DocumentServer.Models.Enums;
using SlugEnt.FluentResults;


[assembly: InternalsVisibleTo("Test_DocumentServer")]

namespace SlugEnt.DocumentServer.Core;

/// <summary>
///     Performs all core logic operations of the DocumentServer including database access and file access methods.
/// </summary>
public class DocumentServerEngine
{
    private readonly IFileSystem               _fileSystem;
    private readonly ILogger                   _logger;
    private          DocServerDbContext        _db;
    private          DocumentServerInformation _documentServerInformation;
    private          NodeToNodeHttpClient      _nodeHttpClient;


    /// <summary>
    ///     Constructor with Dependency Injection
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="fileSystem"></param>
    public DocumentServerEngine(ILogger<DocumentServerEngine> logger,
                                DocServerDbContext dbContext,
                                DocumentServerInformation documentServerInformation,
                                NodeToNodeHttpClient nodeHttpClient,
                                IFileSystem? fileSystem = null
    )
    {
        _logger                    = logger;
        _db                        = dbContext;
        _documentServerInformation = documentServerInformation;
        _nodeHttpClient            = nodeHttpClient;
        _nodeHttpClient.NodeKey    = _documentServerInformation.ServerHostInfo.NodeKey;

        if (fileSystem != null)
            _fileSystem = fileSystem;

        // Use the real file system
        else
            _fileSystem = new FileSystem();
    }



    /// <summary>
    ///     Loads the specified DocumentType and validates some values before returning.
    /// </summary>
    /// <param name="documentTypeId"></param>
    /// <returns>Result.Success if good, Result.Fail if bad</returns>
    internal async Task<Result<DocumentType>> LoadDocumentType_ForSavingStoredDocument(int documentTypeId)
    {
        // We now use the Document Cache
        Result<DocumentType> result = _documentServerInformation.GetCachedDocumentType(documentTypeId);
        return result;

        // Retrieve the DocumentType
        /*
        DocumentType? docType = await _db.DocumentTypes
                                         .Include(i => i.ActiveStorageNode1)
                                         .Include(i => i.ActiveStorageNode2)
                                         .SingleOrDefaultAsync(d => d.Id == documentTypeId);

        if (docType == null)
        {
            string msg = "Unable to locate a DocumentType with the Id [ " + documentTypeId + " ]";
            return Result.Fail(new Error(msg));
        }

        if (!docType.IsActive)
        {
            string msg = string.Format("Document type [ {0} ] - [ {1} ] is not Active.", docType.Id, docType.Name);
            return Result.Fail(new Error(msg));
        }


        // TODO Need to implement logic to try 2nd active node.
        if (docType.ActiveStorageNode1Id == null)
        {
            string msg = string.Format("The DocumentType requested does not have a storage node specified.  DocType [ " + docType.Id) + " ]";
            _logger.LogError("DocumentType has an ActiveStorageNodeId that is null.  Must have a value.  {DocTypeId}", docType.Id);

            return Result.Fail(new Error(msg));
        }

        return Result.Ok(docType);
        */
    }



    /// <summary>
    ///     This is logic that is called before saving a StoredDocument, either first save or subsequent update saves.
    ///     The StoredDocument is saved in this method.
    /// </summary>
    /// <param name="documentType"></param>
    /// <param name="storedDocument"></param>
    /// <param name="isFirstSave"></param>
    /// <returns></returns>
    private async Task<Result> PreSaveEdits(DocumentType documentType,
                                            StoredDocument storedDocument,
                                            bool isFirstSave = true)
    {
        ExpiringDocument? expiring = null;

        if (isFirstSave)
            if (documentType.StorageMode == EnumStorageMode.Temporary)
            {
                // Since this is a temporary document we immediately mark it as inactive to start its lifetime counter.
                storedDocument.IsAlive = false;

                // Need to add an entry to the ExpiringDocuments table
                Result<ExpiringDocument> expResult = ExpiringDocument.Create(documentType.InActiveLifeTime);
                if (expResult.IsFailed)
                    return Result.Fail(expResult.Errors);

                expiring = expResult.Value;
            }

        // Save the StoredDocument so we can get id to set for Expiring
        await _db.SaveChangesAsync();

        // Set Expiring Id to the storeddocuments Id.
        if (expiring != null)
        {
            expiring.StoredDocumentId = storedDocument.Id;
            _db.Add(expiring);
            await _db.SaveChangesAsync();
        }

        return Result.Ok();
    }


    /// <summary>
    /// Retrieves a Stored Document.  
    /// </summary>
    /// <param name="id"></param>
    /// <remarks>Note, this method assumes that we are the host that should be retrieving the document.</remarks>
    /// <returns></returns>
    public async Task<Result<ReturnedDocumentInfo>> GetStoredDocumentAsync(long id,
                                                                           string appToken)
    {
        _logger.LogInformation("GetStoredDocumentAsync:  Starting|   Id: {0}", id);
        try
        {
            // Verify the Application Token is correct.
            if (!_documentServerInformation.CachedApplicationTokenLookup.TryGetValue(appToken, out Application application))
                return Result.Fail("Invalid Application Token provided.");


            var storedDocument = await _db.StoredDocuments.Where(sd => sd.Id == id).Select(s => new
            {
                StoredDocument = s,
                ApplicationId  = s.DocumentType.ApplicationId
            }).FirstOrDefaultAsync();
            _logger.LogDebug("Retrieved StoredDocument Id: {0}", id);

            // Lookup storage Node Info


            if (storedDocument == null)
                return Result.Fail("Unable to find a Stored Document with that Id");

            if (storedDocument.ApplicationId != application.Id)
                return Result.Fail("Application Token provided does not match the application Id of the Stored Document requested.  Access Denied");


            StoredDocument thisStoredDocument = storedDocument.StoredDocument;

            // The path is the node host path + stored document path + filename
            string fullFileName = ComputeDocumentRetrievalPath(thisStoredDocument);
            string extension    = Path.GetExtension(thisStoredDocument.FileName);
            if (extension == string.Empty)
                extension = MediaTypes.GetExtension(thisStoredDocument.MediaType);
            else

                // Strip the returned period.
                extension = extension.Substring(1);


            // Load the TransferDocument info
            ReturnedDocumentInfo returnedDocumentInfo = new()
            {
                Description = storedDocument.StoredDocument.Description,
                Extension   = extension,
                MediaType   = storedDocument.StoredDocument.MediaType,
                ContentType = MediaTypes.GetContentType(storedDocument.StoredDocument.MediaType),
            };

            // Load the file from the storage system
            Result resultRetrieveDocument =
                await RetrieveDocumentFromThisHost(returnedDocumentInfo,
                                                   thisStoredDocument.PrimaryStorageNodeId,
                                                   thisStoredDocument.StorageFolder,
                                                   thisStoredDocument.FileName);
            if (!resultRetrieveDocument.IsSuccess)
                return resultRetrieveDocument;


            StoredDocument sd = storedDocument.StoredDocument;
            sd.NumberOfTimesAccessed++;
            sd.LastAccessedUTC = DateTime.UtcNow;
            _db.Update(sd);
            await _db.SaveChangesAsync();

            _logger.LogInformation("GetStoredDocumentAsync Exiting Ok|  Id: {0}", id);
            return Result.Ok(returnedDocumentInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError("GetStoredDocumentFileBytesAsync:  DocumentId [{DocumentId} ]Exception:  {Error}", id, ex.Message);
            return Result.Fail(new Error("Unable to read document from library.").CausedBy(ex));
        }
    }



    /// <summary>
    /// Retrieves the document from this host.  Assumes you have already validated that it is on this host or should be.
    /// </summary>
    /// <param name="nodePath"></param>
    /// <param name="documentPath"></param>
    /// <param name="filenameWithExtension"></param>
    /// <returns></returns>
    internal async Task<Result> RetrieveDocumentFromThisHost(ReturnedDocumentInfo returnedDocumentInfo,
                                                             int? fromNodeID,
                                                             string documentPath,
                                                             string filenameWithExtension)
    {
        if (fromNodeID == null)
            return Result.Fail("Cannot retrieve a document from a null Storage Node");

        try
        {
            // Lookup the Node's path
            StorageNode storageNode = null;
            if (!_documentServerInformation.CachedStorageNodes.TryGetValue((int)fromNodeID, out storageNode))
                return Result.Fail("Unable to locate the Storage Node with Id " + fromNodeID + " from the local Cache");


            string path = Path.Join(_documentServerInformation.ServerHostInfo.Path,
                                    storageNode.NodePath,
                                    documentPath,
                                    filenameWithExtension);

            returnedDocumentInfo.FileInBytes = _fileSystem.File.ReadAllBytes(path);
            returnedDocumentInfo.Size        = returnedDocumentInfo.FileInBytes.Length;
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to retrieve the document from File System. ").CausedBy(ex));
        }
    }



    internal async Task<Result> RetrieveDocument(StoredDocument storedDocument,
                                                 int? fromNodeId = null)
    {
        // If fromNodeId is null then use the one that matches this host.
        if (fromNodeId == null)
        {
            //    _documentServerInformation.CachedStorageNodes.TryGetValue(stored)
        }

        return Result.Ok();
    }



    /// <summary>
    /// Builds the entire filename path to retrieve the document.
    /// </summary>
    /// <param name="storedDocument"></param>
    /// <returns></returns>
    internal string ComputeDocumentRetrievalPath(StoredDocument storedDocument)
    {
        // Lookup the node so we can get its NodePath
        Result<StorageNode> result = _documentServerInformation.GetCachedStorageNode((int)storedDocument.PrimaryStorageNodeId);
        if (result.IsFailed)
        {
            _logger.LogError("Failed to Compute Document Retrieval Path: " + result.ToString());
            return string.Empty;
        }

        return Path.Join(_documentServerInformation.ServerHostInfo.Path,
                         result.Value.NodePath,
                         storedDocument.StorageFolder,
                         storedDocument.FileName);
    }


    /// <summary>
    ///     Replaces a Replaceable document with a new one.  Marks for deletion the old one.
    /// </summary>
    /// <returns></returns>
    public async Task<Result<StoredDocument>> ReplaceDocument(TransferDocumentDto replacementDto)
    {
        // Lets read current stored document
        StoredDocument? storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(sd => sd.Id == replacementDto.CurrentStoredDocumentId);
        if (storedDocument == null)
            return Result.Fail(new Error("Unable to find existing StoredDocument with Id [ " + replacementDto.CurrentStoredDocumentId + " ]"));


        return Result.Ok(storedDocument);
    }



    /// <summary>
    /// Changes the Alive Status for a Document Type
    /// </summary>
    /// <param name="documentTypeId"></param>
    /// <param name="isAliveValue"></param>
    /// <returns></returns>
    public async Task<Result> SetDocumentTypeAliveStatus(int documentTypeId,
                                                         bool isAliveValue)
    {
        try
        {
            DocumentType documentType = await _db.DocumentTypes.SingleAsync(d => d.Id == documentTypeId);
            if (documentType == null)
                return Result.Fail("Unable to find a DocumentType with the Id of " + documentTypeId);

            if (documentType.IsActive == isAliveValue)
                return Result.Ok();

            // Change it 
            documentType.IsActive = isAliveValue;
            await _db.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to change DocumentType [ {DocumentType} Active Status.  Error: {Error}", documentTypeId, ex);
            return Result.Fail(new Error("Failed to change DocumentType IsAlive Status due to error.").CausedBy(ex));
        }
    }


    public async Task<Result> StoreDocumentReplica(RemoteDocumentStorageDto remoteDocumentStorageDto)
    {
        try
        {
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to store replica.").CausedBy(ex));
        }
    }


    /// <summary>
    /// Saves a document to the Document Server
    /// </summary>
    /// <param name="transferDocumentDto"></param>
    /// <param name="applicationToken"></param>
    /// <returns></returns>
    public async Task<Result<StoredDocument>> StoreDocumentNew(TransferDocumentDto transferDocumentDto,
                                                               string applicationToken)
    {
        IDbContextTransaction?       transaction             = null;
        bool                         fileSavedToStorage      = false;
        string                       fullFileName            = "";
        DocumentOperationStatus      documentOperationStatus = new();
        Result<StoredDocument>       result                  = new();
        bool                         IsPrimaryNode           = false;
        List<DestinationStorageNode> destinationNodes        = null;

        try
        {
            // File Extension as null causes issues.
            if (transferDocumentDto.FileExtension == null)
                transferDocumentDto.FileExtension = string.Empty;

            // Verify the Application Token is correct.
            if (!_documentServerInformation.CachedApplicationTokenLookup.TryGetValue(applicationToken, out Application application))
                return Result.Fail("Invalid Application Token provided.");

            // Load and Validate the DocumentType is ok to use
            Result<DocumentType> documentTypeResult = _documentServerInformation.GetCachedDocumentType(transferDocumentDto.DocumentTypeId);
            if (documentTypeResult.IsFailed)
                return Result.Merge(result, documentTypeResult);

            DocumentType docType = documentTypeResult.Value;


            // Make sure the DocType application matches the application the token was for.
            if (docType.ApplicationId != application.Id)
                return Result.Fail("The document type requested is not a member of the application you provided a token for.  Access denied.");

            int tmpFileSize = (int)transferDocumentDto.File.Length;
            int fileSize    = tmpFileSize > 1024 ? tmpFileSize / 1024 : 1;

            if (string.IsNullOrWhiteSpace(transferDocumentDto.RootObjectId))
                return Result.Fail(new Error("No RootObjectId was specified on the transferDocumentContainer.  It is required."));

            if (string.IsNullOrWhiteSpace(transferDocumentDto.DocTypeExternalId))
            {
                // Ensure it is null.
                transferDocumentDto.DocTypeExternalId = null;
            }
            else
            {
                // Make sure we are following the rule for not allowing duplicates if it is set.
                if (!docType.AllowSameDTEKeys)
                {
                    // Make sure the external key does not already exist.
                    bool exists = _db.StoredDocuments.Any(sd => sd.RootObjectExternalKey == transferDocumentDto.RootObjectId &&
                                                                sd.DocTypeExternalKey == transferDocumentDto.DocTypeExternalId);
                    if (exists)
                    {
                        string msg =
                            string.Format("Duplicate Key not allowed.  RootObject Id [ {0} ] already has a DocumentType [ {1} ]  with an External Id of [ {2} ].  This DocumentType does not allow duplicate entries.",
                                          transferDocumentDto.RootObjectId,
                                          transferDocumentDto.DocumentTypeId,
                                          transferDocumentDto.DocTypeExternalId);
                        return Result.Fail(new Error(msg));
                    }
                }
            }


            StoredDocument storedDocument = new(transferDocumentDto.FileExtension,
                                                transferDocumentDto.Description,
                                                transferDocumentDto.RootObjectId,
                                                transferDocumentDto.DocTypeExternalId,
                                                "",
                                                fileSize,
                                                transferDocumentDto.DocumentTypeId);
            SetMediaType(transferDocumentDto.MediaType, transferDocumentDto.FileExtension, storedDocument);

            // Start DB transaction - So we can capture the ReplicateTask
            transaction = _db.Database.BeginTransaction();

            // TODO Determine if we are Primary or Secondary Node and fix this code

            bool        NodeIsOnThisHost = false;
            StorageNode storageNode      = null;
            Result<List<DestinationStorageNode>> storeResult = await StoreFile(storedDocument,
                                                                               docType,
                                                                               transferDocumentDto.File,
                                                                               true);

            destinationNodes = storeResult.Value;

            //fullFileName       = storeResult.Value;
            fileSavedToStorage = true;


            // Save StoredDocument
            _db.StoredDocuments.Add(storedDocument);
            Result preSaveResult = await PreSaveEdits(docType, storedDocument);
            if (preSaveResult.IsFailed)
                return preSaveResult;

            _db.Database.CommitTransaction();

            Result<StoredDocument> finalResult = Result.Ok(storedDocument);
            return finalResult;
        }
        catch (Exception ex)
        {
            string cleanupMsg = "";

            // Delete the file from storage if we successfully saved it, but failed afterward.
            if (fileSavedToStorage)
            {
                try
                {
                    await _db.Database.RollbackTransactionAsync();
                    foreach (DestinationStorageNode destinationStorageNode in destinationNodes)
                    {
                        if (destinationStorageNode.IsOnThisHost)
                            _fileSystem.File.Delete(destinationStorageNode.FullFileNameAsStored);
                    }
                }
                catch (Exception exception)
                {
                    cleanupMsg = "StoreDocumentNew:  During Error Cleanup encountered another error: " + exception.ToString();
                    _logger.LogError(cleanupMsg);
                }
            }

            string msg =
                string.Format($"StoreDocument:  Failed to store the document:  Description: {transferDocumentDto.Description}, Extension: {transferDocumentDto.FileExtension}.  Error Was: {ex.Message}.  ");

            StringBuilder sb = new();
            sb.Append(msg);
            if (ex.InnerException != null)
                sb.Append(ex.InnerException.Message);
            msg = sb.ToString();

            _logger.LogError("StoreDocument: Failed to store document.{Description}{Extension}Error:{Error}.",
                             transferDocumentDto.Description,
                             transferDocumentDto.FileExtension,
                             msg);

            Result errorResult = Result.Fail(new Error("Failed to store the document due to errors.").CausedBy(ex));
            return errorResult;
        }
    }



    /// <summary>
    /// Performs the actual storage of a file to a node.  Note, this only stores the physical file.
    /// Will store to all nodes in which the Server Host Id (server we are running on) matches the Node's Server ID, ie, it will store locally.
    /// It will store to remote node(s) if:
    ///   - None of the nodes are tied to this server host.  It will store on at least one node and depending on size more than 1 node.
    ///   - If one of the nodes was local, and the size of the document is within immediate storage limits.
    /// </summary>
    /// <param name="storedDocument"></param>
    /// <param name="documentType"></param>
    /// <param name="file"></param>
    /// <param name="isInitialSave">True, if this is the first time save of this document. Ie. Its not a replica save</param>
    /// <returns>List of the DestinationStorageNodes.  This is needed if errors following this code require this document to be deleted</returns>
    /// <exception cref="NotImplementedException"></exception>
    internal async Task<Result<List<DestinationStorageNode>>> StoreFile(StoredDocument storedDocument,
                                                                        DocumentType documentType,
                                                                        IFormFile file,
                                                                        bool isInitialSave)
    {
        DestinationStorageNode? destinationStorageNode1 = null;
        DestinationStorageNode? destinationStorageNode2 = null;

        List<DestinationStorageNode> destinationStorageNodes = new();

        try
        {
            // Retrieve the StoragePath Folder for the StoredDocument
            Result<string> storagePathResult = ComputeStoredDocumentStoragePath(documentType);
            if (storagePathResult.IsFailed)
                return Result.Fail(new Error("Failed to compute the StorageFolder path for the StoredDocument.").CausedBy(storagePathResult.Errors));

            storedDocument.StorageFolder = storagePathResult.Value;

            // Check to see if the primary storage node is on this host (The host this code is running on now)
            Result<DestinationStorageNode> destinationNodeResult = ComputeDestinationStorageNode(documentType.ActiveStorageNode1Id, destinationStorageNodes, true);
            destinationNodeResult = ComputeDestinationStorageNode(documentType.ActiveStorageNode2Id, destinationStorageNodes, false);

            // At this point we should have AT least one node if not 2 for storage.  They will be listed by if they are on this host first, then other hosts 2nd.
            // If the first one is not on this host then neither is the 2nd.
            bool                                 storedLocally  = false;
            bool                                 storedRemotely = false;
            short                                nodesStoredOn  = 0;
            Result<List<DestinationStorageNode>> finalResult    = new Result<List<DestinationStorageNode>>();

            bool  isErrored = false;
            short loopPass  = 1;
            while (nodesStoredOn == 0)
            {
                foreach (DestinationStorageNode destinationStorageNode in destinationStorageNodes)
                {
                    if (!destinationStorageNode.WasSaved)
                    {
                        if (destinationStorageNode.IsOnThisHost)
                        {
                            // Store the document on media
                            Result resultStore = await InternalStoreDocumentOnThisHost(destinationStorageNode,
                                                                                       storedDocument,
                                                                                       documentType,
                                                                                       file);
                            if (resultStore.IsFailed)
                            {
                                _logger.LogError("Failed to store document [ " + storedDocument.FileName + " ] on local node: [ " + destinationStorageNode.StorageNode.Name +
                                                 " ]  --> " +
                                                 resultStore.ToString());
                                finalResult.Errors.AddRange(resultStore.Errors);
                            }
                            else
                            {
                                storedLocally                   = true;
                                destinationStorageNode.WasSaved = true;
                                nodesStoredOn++;
                                SetStoredDocumentNode(storedDocument, destinationStorageNode);
                            }
                        }
                        else
                        {
                            // Is a remote store....
                            // If first pass then we adhere to size limits
                            if (loopPass == 1 && file.Length > _documentServerInformation.RuntimeSettings.RemoteDocumentSizeThreshold)
                                continue;

                            RemoteDocumentStorageDto remoteDocumentStorageDto = new()
                            {
                                File          = file,
                                StorageNodeId = destinationStorageNode.StorageNode.Id,
                                FileName      = storedDocument.FileName,
                                StoragePath   = storedDocument.StorageFolder,
                            };

                            string url          = destinationStorageNode.StorageNode.ServerHost.FQDN + ":" + _documentServerInformation.RemoteNodePort;
                            Result remoteResult = await _nodeHttpClient.SendDocument(url, remoteDocumentStorageDto);
                            if (remoteResult.IsFailed)
                            {
                                finalResult.Errors.AddRange(remoteResult.Errors);
                            }
                            else
                            {
                                storedRemotely                  = true;
                                destinationStorageNode.WasSaved = true;
                                nodesStoredOn++;
                                SetStoredDocumentNode(storedDocument, destinationStorageNode);
                            }
                        }
                    }
                }

                loopPass++;
                if (loopPass > 2 && nodesStoredOn == 0)
                {
                    // TODO this does not have the source errors attached.
                    Result failedResult =
                        Result.Fail(new
                                        Error("Exhausted number of attempts to try and save file on at least one of the preferred nodes.  All attempts failed.  See errors attached to this."));
                    return failedResult;
                }
            }


            // If stored on all nodes then we are done.
            if (nodesStoredOn == destinationStorageNodes.Count)
            {
                _logger.LogInformation("Document was stored on all nodes!");
                return Result.Ok(destinationStorageNodes);
            }

            if (storedLocally)
                _logger.LogInformation("Document was stored on local node only");
            if (storedRemotely)
                _logger.LogInformation("Document was stored on remote node only");


            // We need to insert a Replication Task into the DB to determine the from node and to node.
            // Since we are here, the primary node is the From node.  The Secondary should be null and we need to determine
            int? toNode;
            if (documentType.ActiveStorageNode1Id == storedDocument.Id)
                toNode = documentType.ActiveStorageNode2Id != null ? documentType.ActiveStorageNode2Id : null;
            else
                toNode = documentType.ActiveStorageNode1Id != null ? documentType.ActiveStorageNode1Id : null;

            if (toNode == null)
                return Result.Ok();


            // Insert Replication Entry into DB.
            ReplicationTask replicationTask = new ReplicationTask()
            {
                StoredDocumentId           = storedDocument.Id,
                IsActive                   = true,
                ReplicateFromStorageNodeId = (int)storedDocument.PrimaryStorageNodeId,
                ReplicateToStorageNodeId   = (int)toNode,
            };
            _db.Add(replicationTask);

            return Result.Ok(destinationStorageNodes);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }


    /// <summary>
    /// Sets the StoredDocument Node of the first unused NodeId to the DestinationNode Id.
    /// </summary>
    /// <param name="storedDocument"></param>
    /// <param name="destinationStorageNode"></param>
    internal void SetStoredDocumentNode(StoredDocument storedDocument,
                                        DestinationStorageNode destinationStorageNode)
    {
        if (storedDocument.PrimaryStorageNodeId == null)
            storedDocument.PrimaryStorageNodeId = destinationStorageNode.StorageNode.Id;
        else
            storedDocument.SecondaryStorageNodeId = destinationStorageNode.StorageNode.Id;
    }


    internal Result<DestinationStorageNode> ComputeDestinationStorageNode(int? nodeId,
                                                                          List<DestinationStorageNode> destinationStorageNodes,
                                                                          bool isPrimaryNode)
    {
        if (nodeId == null)
            return Result.Ok((DestinationStorageNode)null);

        DestinationStorageNode destinationStorageNode = new();
        Result<StorageNode>    storageNodeResult      = _documentServerInformation.GetCachedStorageNode((int)nodeId);
        if (storageNodeResult.IsFailed)
        {
            _logger.LogError("StoreDocumentNew | " + storageNodeResult.ToString());
            return Result.Fail(storageNodeResult.Errors);
        }

        destinationStorageNode.StorageNode = storageNodeResult.Value;

        if (isPrimaryNode)
            destinationStorageNode.IsPrimaryDocTypeStorage = isPrimaryNode;

        if (storageNodeResult.Value.ServerHostId == _documentServerInformation.ServerHostInfo.ServerHostId)
            destinationStorageNode.IsOnThisHost = true;

        destinationStorageNodes.Add(destinationStorageNode);
        return Result.Ok(destinationStorageNode);
    }


    /// <summary>
    /// Stores a file on the local node that was requested from a remote node.  The remote node is the one responsible for all database and document
    /// information.  This method, literally calculates the entire document storage path, stores it and returns result.
    /// </summary>
    /// <param name="storageNodeId">The Node ID that is being requested to store the document on</param>
    /// <param name="path">The Document specific part of the path to the document.</param>
    /// <param name="file">The file bytes to be stored.</param>
    /// <returns></returns>
    public async Task<Result> StoreFileFromRemoteNode(RemoteDocumentStorageDto remoteDto)
    {
        try
        {
            // Ensure file has been sent
            if (remoteDto.File == null)
                return Result.Fail(new Error("File was null."));

            // Retrieve Storage Node
            Result<StorageNode> storageNodeResult = _documentServerInformation.GetCachedStorageNode(remoteDto.StorageNodeId);
            if (storageNodeResult.IsFailed)
            {
                _logger.LogError("Failed to locate a storage node Id [ " + remoteDto.StorageNodeId + " ] in the StorageNode Cache.  Cannot compute Storage Folder Location");
                return Result.Fail(storageNodeResult.Errors);
            }

            StorageNode storageNode = storageNodeResult.Value;


            // Determine the Entire Physical storage path
            Result<string> result = ComputePhysicalStoragePath(storageNode.ServerHostId, storageNode, remoteDto.StoragePath);
            if (result.IsFailed)
            {
                _logger.LogWarning("StoreFileFromRemoteNode:  Failed to compute path: " + result.ToString());
                return Result.Fail(result.Errors);
            }


            // We need to make sure directory exits.
            if (!_fileSystem.Directory.Exists(result.Value))
                _fileSystem.Directory.CreateDirectory(result.Value);

            string fullFileName    = Path.Join(result.Value, remoteDto.FileName);
            Result storeFileResult = await StoreFileOnStorageMediaAsync(fullFileName, remoteDto.File);
            return storeFileResult;
        }
        catch (Exception e)
        {
            _logger.LogError("StoreFileFromRemoteNode |  " + e);
            return Result.Fail(new Error("StoreFileFromRemoteNode Error").CausedBy(e));
        }
    }



    /// <summary>
    /// Stores a document on the local host.  This is called when the local host is also the host that received the initial call to store the document.
    /// </summary>
    /// <param name="destinationStorageNode"></param>
    /// <param name="storedDocument"></param>
    /// <param name="documentType"></param>
    /// <param name="file"></param>
    /// <returns></returns>
    internal async Task<Result> InternalStoreDocumentOnThisHost(DestinationStorageNode destinationStorageNode,
                                                                StoredDocument storedDocument,
                                                                DocumentType documentType,
                                                                IFormFile file)
    {
        // Store the document on the storage media
        Result<string> storeResult = await StoreFileOnStorageMediaAsync(storedDocument,
                                                                        documentType,
                                                                        destinationStorageNode.StorageNode.Id,
                                                                        file);
        if (storeResult.IsFailed)
            return Result.Fail(storeResult.Errors);

        // Save the file name in case we need to delete it due to errors following this code.
        destinationStorageNode.FullFileNameAsStored = storeResult.Value;

        //if (destinationStorageNode.IsPrimaryDocTypeStorage)
        //    storedDocument.PrimaryStorageNodeId = destinationStorageNode.StorageNode.Id;
        return Result.Ok();
    }



    /// <summary>
    /// Determines and sets the Media Type based upon the passed MediaType or Extension.  
    /// </summary>
    /// <param name="storedDocument"></param>
    internal static void SetMediaType(EnumMediaTypes mediaType,
                                      string fileExtension,
                                      StoredDocument storedDocument)
    {
        // Use user specified Media Type if available.
        if (mediaType != EnumMediaTypes.NotSpecified)
        {
            storedDocument.MediaType = mediaType;
            return;
        }

        // Figure out based upon extension
        storedDocument.MediaType = MediaTypes.GetMediaType(fileExtension.ToLower());
    }



    /// <summary>
    ///     Determines where to store the file and then stores it.
    /// </summary>
    /// <param name="storedDocument">The StoredDocument that will be updated with path info</param>
    /// <param name="documentType">DocumentType this document is</param>
    /// <param name="storageNodeId">Node Id to store file at</param>
    /// <param name="formFile">Contents of the file</param>
    /// <returns>Result string where string is the full file name </returns>
    internal async Task<Result<string>> StoreFileOnStorageMediaAsync(StoredDocument storedDocument,
                                                                     DocumentType documentType,
                                                                     int storageNodeId,
                                                                     IFormFile formFile)
    {
        string fullFileName = "";

        try
        {
            Result<string> resultB = await ComputeStorageFullNameAsync(documentType, storageNodeId, storedDocument.StorageFolder);
            if (resultB.IsFailed)
                return Result.Fail(new Error("Failed to compute Where the document should be stored.").CausedBy(resultB.Errors));

            // Store the path and make sure all the paths exist.
            string storeAtPath = resultB.Value;
            _fileSystem.Directory.CreateDirectory(storeAtPath);


            fullFileName = Path.Combine(storeAtPath, storedDocument.FileName);
            Result storeFileResult = await StoreFileOnStorageMediaAsync(fullFileName, formFile);
            if (!storeFileResult.IsSuccess)
                return storeFileResult;


            // Save the path in the StoredDocument.  But here we do not save the host prefix part of the path, we only need the unique path to the document.
            // TODO DO NOT store the drive, host path or nodepath part of the path.
            //storedDocument.StorageFolder = resultB.Value.StoredDocumentPath;
            return Result.Ok(fullFileName);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to save file on permanent media. FullFileName [ " + fullFileName + " ] ").CausedBy(ex));
        }
    }



    /// <summary>
    /// Stores the file on the filesystem of the node this is running from.  Filename can either be supplied as part of the storagePath or separately in the fileName property.
    /// </summary>
    /// <param name="storagePath">The complete physical path on the storage device to store the file at.  It can optionally include the filename.  If it does then the fileName
    /// parameter should be left at default value.</param>
    /// <param name="file"></param>
    /// <param name="fileName">[Optional] The filename of the file.  If left as default then it is assumed it is part of the storagePath</param>
    /// <returns>Result indicating success or failure</returns>
    internal async Task<Result> StoreFileOnStorageMediaAsync(string storagePath,
                                                             IFormFile file,
                                                             string fileName = "")
    {
        try
        {
            if (fileName != string.Empty)
                storagePath = Path.Join(storagePath, fileName);

            // TODO Missing Storage Node path...
            //_fileSystem.Directory.CreateDirectory(fullFileName);

            using (Stream fs = _fileSystem.File.Create(storagePath))
                await file.CopyToAsync(fs);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("Failed to save file on permanent media. FullFileName [ " + storagePath + " ] ").CausedBy(ex));
        }
    }



    /// <summary>
    ///     Stores a new document that is a replacement for an existing document.
    /// </summary>
    /// <returns></returns>
    public async Task<Result<StoredDocument>> StoreReplacementDocumentAsync(TransferDocumentDto replacementDto)
    {
        // The replacement process is:
        // - Store new file
        // - Update StoredDocument
        // - Delete Old File

        IDbContextTransaction? transaction  = null;
        string                 fullFileName = "";
        bool                   docSaved     = false;
        string                 oldFileName  = "";
        Result<StoredDocument> result       = new();
        try
        {
            // We need to load the existing StoredDocument to retrieve some info.
            StoredDocument? storedDocument = await _db.StoredDocuments.SingleOrDefaultAsync(sd => sd.Id == replacementDto.CurrentStoredDocumentId);
            if (storedDocument == null)
            {
                string msg = string.Format("Unable to find existing StoredDocument with Id [ {0} ]", replacementDto.CurrentStoredDocumentId);
                return Result.Fail(new Error(msg));
            }

            // Load and Validate the DocumentType is ok to use
            Result<DocumentType> docTypeResult = await LoadDocumentType_ForSavingStoredDocument(storedDocument.DocumentTypeId);
            if (docTypeResult.IsFailed)
                return Result.Merge(result, docTypeResult);

            DocumentType docType = docTypeResult.Value;

            // Confirm this is a replaceable Document Type
            if (!docType.IsReplaceable)
            {
                return Result.Fail("This StoredDocument was originally stored as a Document Type that is not replaceable.  You cannot replace the old document with a newer copy!");
            }

            // Save the current Document FileName so we can delete it in a moment.
            oldFileName = ComputeDocumentRetrievalPath(storedDocument);

            // Store the new document on the storage media
            storedDocument.ReplaceFileName(replacementDto.FileExtension);
            Result<string> storeResult = await StoreFileOnStorageMediaAsync(storedDocument,
                                                                            docType,
                                                                            (int)docType.ActiveStorageNode1Id,
                                                                            replacementDto.File);
            if (storeResult.IsFailed)
                return Result.Merge(result, storeResult);

            fullFileName = storeResult.Value;

            // Save StoredDocument
            //  - Update description
            if (!string.IsNullOrEmpty(replacementDto.Description))
                storedDocument.Description = replacementDto.Description;

            transaction = _db.Database.BeginTransaction();
            _db.StoredDocuments.Update(storedDocument);
            await PreSaveEdits(docType, storedDocument, false);
            _db.Database.CommitTransaction();
            docSaved = true;

            // Delete the old document
            _fileSystem.File.Delete(oldFileName);

            Result<StoredDocument> finalResult = Result.Ok(storedDocument);
            return finalResult;
        }
        catch (Exception ex)
        {
            if (docSaved)
            {
                // Then error happened during file delete of old document
                // TODO add to error table.
                string msg = string.Format("Error during cleanup of old replaceable file [ {0} ].  File remains on storagemedia and will need to be deleted manually.",
                                           oldFileName);
                return Result.Fail(new Error(msg).CausedBy(ex));
            }

            _logger.LogError("StoreReplacementDocumentAsync:  {Exception}", ex.Message);
            return Result.Fail(new Error("StoreReplacementDocumentTypeAsync:  " + ex.Message).CausedBy(ex));
        }
    }


    /// <summary>
    /// Saves the requested application object to the database.  It creates the Token prior to save and returns its value in the Result object.
    /// </summary>
    /// <param name="application"></param>
    /// <returns></returns>
    /*
    public async Task<Result<string>> ApplicationSaveAsync(Application application)
    {
        // Ensure that this is a new application object and not an existing.
        if (application.Id > 0)
            return Result.Fail("Cannot use this method to save an existing Application.");

        string guid = Guid.NewGuid().ToString("N");
        application.Token = guid;

        _db.Applications.Add(application);
        await _db.SaveChangesAsync();

        return Result.Ok(guid);
    }
    */


#region "Support Functions"

    internal Result<string> ComputeStoredDocumentStoragePath(DocumentType documentType)
    {
        // Get letter for Mode.
        Result<string> modeResult = GetModeLetter(documentType.StorageMode);
        if (modeResult.IsFailed)
            return Result.Fail(new Error("Failed to get Mode for StorageMode").CausedBy(modeResult.Errors));

        string modePath = modeResult.Value;


        DateTime folderDatetime = DateTime.UtcNow;

        // For most of the documents we store them in a folder based upon the date we write them.
        // But for Temporary we write based upon the date they expire.
        if (documentType.StorageMode == EnumStorageMode.Temporary)
            folderDatetime = documentType.InActiveLifeTime switch
            {
                EnumDocumentLifetimes.HoursOne    => folderDatetime.AddHours(1),
                EnumDocumentLifetimes.HoursFour   => folderDatetime.AddHours(4),
                EnumDocumentLifetimes.HoursTwelve => folderDatetime.AddHours(12),
                EnumDocumentLifetimes.DayOne      => folderDatetime.AddDays(1),
                EnumDocumentLifetimes.MonthOne    => folderDatetime.AddMonths(1),
                EnumDocumentLifetimes.MonthsThree => folderDatetime.AddMonths(3),
                EnumDocumentLifetimes.MonthsSix   => folderDatetime.AddMonths(6),
                EnumDocumentLifetimes.Months18    => folderDatetime.AddMonths(18),
                EnumDocumentLifetimes.WeekOne     => folderDatetime.AddDays(7),
                EnumDocumentLifetimes.YearOne     => folderDatetime.AddYears(1),
                EnumDocumentLifetimes.YearsTwo    => folderDatetime.AddYears(2),
                EnumDocumentLifetimes.YearsThree  => folderDatetime.AddYears(3),
                EnumDocumentLifetimes.YearsFour   => folderDatetime.AddYears(4),
                EnumDocumentLifetimes.YearsFive   => folderDatetime.AddYears(5),
                EnumDocumentLifetimes.YearsSeven  => folderDatetime.AddYears(7),
                EnumDocumentLifetimes.YearsTen    => folderDatetime.AddYears(10),
                EnumDocumentLifetimes.Never       => DateTime.MaxValue,
                _                                 => throw new Exception("Unknown DocumentLifetime value of [ " + documentType.InActiveLifeTime + " ]")
            };

        string year  = folderDatetime.ToString("yyyy");
        string month = folderDatetime.ToString("MM");


        string storageFolderPath = Path.Combine(modePath,
                                                documentType.StorageFolderName,
                                                year,
                                                month);
        return Result.Ok(storageFolderPath);
    }


    /// <summary>
    ///     Computes the complete path including the actual file name.  Note.  Does not include the HostName or Storage Node Path parts.
    ///     All files are stored in a folder by
    ///     storageNodePath\StorageModeLetter\DocumentTypePath\year\month
    /// </summary>
    /// <param name="documentType">The DocumentType</param>
    /// <param name="storageNodeId">The StorageNode Id to store on</param>
    /// <returns>Result</returns>
    internal async Task<Result<string>> ComputeStorageFullNameAsync(DocumentType documentType,
                                                                    int storageNodeId,
                                                                    string storageFolderPath)
    {
        try
        {
            // Retrieve Storage Node
            Result<StorageNode> storageNodeResult = _documentServerInformation.GetCachedStorageNode(storageNodeId);
            if (storageNodeResult.IsFailed)
            {
                _logger.LogError("Failed to locate a storage node Id [ " + storageNodeId + " ] in the StorageNode Cache.  Cannot compute Storage Folder Location");
                return Result.Fail(storageNodeResult.Errors);
            }

            StorageNode storageNode = storageNodeResult.Value;
            if (storageNode == null)
            {
                string nodeMsg = string.Format("{0} had a storage node [ {1} ] that could not be found.", documentType.ErrorMessage, storageNodeId);
                return Result.Fail(new Error(nodeMsg));
            }

            if (storageNode.ServerHost == null)
            {
                string nodeMsg = string.Format("{0} had a server host that was null.  It must exist.", documentType.ErrorMessage);
                return Result.Fail(new Error(nodeMsg));
            }

            /*
            // Get letter for Mode.
            Result<string> modeResult = GetModeLetter(documentType.StorageMode);
            if (modeResult.IsFailed)
                return Result.Fail(new Error("Failed to get Mode for StorageMode").CausedBy(modeResult.Errors));

            string modePath = modeResult.Value;


            DateTime folderDatetime = DateTime.UtcNow;

            // For most of the documents we store them in a folder based upon the date we write them.
            // But for Temporary we write based upon the date they expire.
            if (documentType.StorageMode == EnumStorageMode.Temporary)
                folderDatetime = documentType.InActiveLifeTime switch
                {
                    EnumDocumentLifetimes.HoursOne    => folderDatetime.AddHours(1),
                    EnumDocumentLifetimes.HoursFour   => folderDatetime.AddHours(4),
                    EnumDocumentLifetimes.HoursTwelve => folderDatetime.AddHours(12),
                    EnumDocumentLifetimes.DayOne      => folderDatetime.AddDays(1),
                    EnumDocumentLifetimes.MonthOne    => folderDatetime.AddMonths(1),
                    EnumDocumentLifetimes.MonthsThree => folderDatetime.AddMonths(3),
                    EnumDocumentLifetimes.MonthsSix   => folderDatetime.AddMonths(6),
                    EnumDocumentLifetimes.Months18    => folderDatetime.AddMonths(18),
                    EnumDocumentLifetimes.WeekOne     => folderDatetime.AddDays(7),
                    EnumDocumentLifetimes.YearOne     => folderDatetime.AddYears(1),
                    EnumDocumentLifetimes.YearsTwo    => folderDatetime.AddYears(2),
                    EnumDocumentLifetimes.YearsThree  => folderDatetime.AddYears(3),
                    EnumDocumentLifetimes.YearsFour   => folderDatetime.AddYears(4),
                    EnumDocumentLifetimes.YearsFive   => folderDatetime.AddYears(5),
                    EnumDocumentLifetimes.YearsSeven  => folderDatetime.AddYears(7),
                    EnumDocumentLifetimes.YearsTen    => folderDatetime.AddYears(10),
                    EnumDocumentLifetimes.Never       => DateTime.MaxValue,
                    _                                 => throw new Exception("Unknown DocumentLifetime value of [ " + documentType.InActiveLifeTime + " ]")
                };

            string year  = folderDatetime.ToString("yyyy");
            string month = folderDatetime.ToString("MM");


            // TODO DONE!  Need to remove storagenode from the StoredDocumentPath
            StoragePathInfo storagePathInfo = new();
            storagePathInfo.StoredDocumentPath = Path.Combine(modePath,
                                                              documentType.StorageFolderName,
                                                              year,
                                                              month);
            */

            Result<string> resultCP = ComputePhysicalStoragePath(storageNode.ServerHostId, storageNode, storageFolderPath);
            if (resultCP.IsFailed)
                return Result.Fail(new Error("Failed to determine host path").CausedBy(resultCP.Errors));

            return Result.Ok(resultCP.Value);
        }
        catch (InvalidOperationException ex)
        {
            string msg = string.Format("ComputeStorageFolder Error for: {0}  -  had exception:{1}", documentType.ErrorMessage, ex);
            _logger.LogError("ComputeStorageFolder Error for {DocumentType}  - had exception:  {Exception}", documentType.ErrorMessage, ex);
            return Result.Fail(msg);
        }
        catch (Exception ex)
        {
            _logger.LogError("ComputeStorageFolder error for {DocumentType} - Storage Node Id: {StorageNode} -   {Error}",
                             documentType.Id,
                             storageNodeId,
                             ex.Message);

            string msg =
                string.Format("ComputeStorageFolder error for {0} - Storage Node Id: {1} -   {2}",
                              documentType.Id,
                              storageNodeId,
                              ex.Message);
            return Result.Fail(msg);
        }
    }



    /// <summary>
    /// Computes the physical location on the node that the document is stored at.
    /// This is HostPath + NodePath + DocumentPath
    /// </summary>
    /// <param name="serverHost"></param>
    /// <param name="documentPath"></param>
    /// <returns>The entire complete path to the file on the file system</returns>
    internal Result<string> ComputePhysicalStoragePath(short serverHostId,
                                                       StorageNode node,
                                                       string documentPath,
                                                       bool serverCheck = true)
    {
        if (serverCheck)
        {
            if (_documentServerInformation.ServerHostInfo.ServerHostId != serverHostId)
            {
                string msg = String.Format("This host is not the host that can write for this StorageNode.  Host: [ " + _documentServerInformation.ServerHostInfo.ServerHostName +
                                           " ]  Requested StorageNode [ " + node.Name + " - " + node.Id + " ]");
                _logger.LogCritical(msg);
                return Result.Fail(msg);
            }

            return Result.Ok(Path.Join(_documentServerInformation.ServerHostInfo.Path, node.NodePath, documentPath));
        }

        // Lookup Host ID 
        Result<ServerHost> hostResult = _documentServerInformation.GetCachedServerHost(serverHostId);
        if (hostResult.IsFailed)
        {
            return Result.Fail(new Error("Unable to find a ServerHost with an id of [ " + serverHostId + " ] in the ServerHost Cache"));
        }

        return Result.Ok(Path.Join(hostResult.Value.Path, node.NodePath, documentPath));
    }



    /// <summary>
    ///     Returns the Mode Letter for the given StorageMode
    /// </summary>
    /// <param name="storageMode"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    internal static Result<string> GetModeLetter(EnumStorageMode storageMode)
    {
        try
        {
            string modeLetter = storageMode switch
            {
                EnumStorageMode.WriteOnceReadMany => "W",
                EnumStorageMode.Editable          => "E",
                EnumStorageMode.Temporary         => "T",
                EnumStorageMode.Replaceable       => "R",
                EnumStorageMode.Versioned         => "V",
                _                                 => throw new NotSupportedException("Invalid StorageMode provided - " + storageMode)
            };
            return Result.Ok(modeLetter);
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError(ex));
        }
    }

#endregion
}