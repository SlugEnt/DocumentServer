﻿using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;
using Serilog;
using System.Runtime.Intrinsics.X86;

namespace SlugEnt.DocumentServer.Core
{
    /// <summary>
    /// Contains configuration and other information about the Document Server Engine between instances of it.  There should only be one instance of this object per application
    /// It should be instantiated as a singleton!
    /// </summary>
    public class DocumentServerInformation
    {
        private IConfiguration  _configuration;
        private Serilog.ILogger _logger;


        /// <summary>
        /// Creates a DocumentServerInformation object
        /// </summary>
        /// <param name="docServerDbContext">The DB Context to connect to database.  Only used during object setup</param>
        /// <param name="configuration">Configuration object.  Only used during object setup</param>
        /// <param name="logger">Serilog logger object.  Note this is not a Microsoft ILogger!</param>
        /// <param name="overrideDNSName">Should only ever be used during unit testing.  Forces the code to use the passed in value
        ///   for the fully qualified name instead of using the medhod Dns.GetHostName.  This allows the unit testing code to start
        ///   a second instance of the API up so it can test API<-->API communication.</param>
        public static DocumentServerInformation Create(IConfiguration configuration,
                                                       DocServerDbContext docServerDbContext = null,
                                                       Serilog.ILogger logger = null,
                                                       string overrideDNSName = "")
        {
            try
            {
                if (docServerDbContext == null)
                {
                    string?                                     sqlConn = configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName());
                    DbContextOptionsBuilder<DocServerDbContext> options = new();
                    options.UseSqlServer(sqlConn);
                    docServerDbContext = new(options.Options);
                }

                // Get Node Key from configuration
                string nodeKey = configuration.GetValue<string>("DocumentServer:NodeKey");
                if (!string.IsNullOrWhiteSpace(nodeKey))
                    logger.Warning("DocumentServerInformation:  Found NodeKey!");
                else
                {
                    string msg = "Failed to find NodeKey in the configuration.  This is a required property.";
                    logger.Error(msg);
                    throw new ApplicationException(msg);
                }

                return new DocumentServerInformation(docServerDbContext, nodeKey, logger);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to Create DocumentServerInformation object in Create Method.  Error:  |  " + ex.ToString());
                throw;
            }
        }


        /// <summary>
        /// Creates a DocumentServerInformation object
        /// </summary>
        /// <param name="docServerDbContext">The DB Context to connect to database.  Only used during object setup</param>
        /// <param name="configuration">Configuration object.  Only used during object setup</param>
        /// <param name="logger">Serilog logger object.  Note this is not a Microsoft ILogger!</param>
        /// <param name="overrideDNSName">Should only ever be used during unit testing.  Forces the code to use the passed in value
        ///   for the fully qualified name instead of using the medhod Dns.GetHostName.  This allows the unit testing code to start
        ///   a second instance of the API up so it can test API<-->API communication.</param>
        public DocumentServerInformation(DocServerDbContext docServerDbContext,
                                         string nodeKey,
                                         Serilog.ILogger logger = null,
                                         string overrideDNSName = "")
        {
            ServerHostInfo = new ServerHostInfo();
            _logger        = logger;
            Initialize     = SetupAsync(docServerDbContext, nodeKey, overrideDNSName);
        }


        public string Id { get; private set; }

        /// <summary>
        /// Task used to perform setup during object creation.
        /// </summary>
        public Task Initialize { get; }


        /// <summary>
        /// Returns true if initialization has completed.
        /// </summary>
        public bool IsInitialized { get; private set; } = false;



        /// <summary>
        /// Performs initial setup upon construction
        /// </summary>
        /// <param name="db"></param>
        /// <param name="overrideDNSName">Should only ever be used during unit testing.  Forces the code to use the passed in value
        ///   for the fully qualified name instead of using the medhod Dns.GetHostName.  This allows the unit testing code to start
        ///   a second instance of the API up so it can test API<-->API communication.</param>
        /// <exception cref="ApplicationException"></exception>
        private async Task SetupAsync(DocServerDbContext db,
                                      string nodeKey,
                                      string overrideDNSName = "")
        {
            try
            {
                // Get LocalHost name.  
                string localHost = Dns.GetHostName();

                // If overrideDNSName is set then we will use that.
                // OverrideDNSName should only be used by unit test code and never in production.
                if (!string.IsNullOrWhiteSpace(overrideDNSName))
                    localHost = overrideDNSName;


                // Generate a Unique ID for this host...
                // TODO IS this needed anymore...
                //Id = Guid.NewGuid().ToString("N");


                ServerHost? host = db.ServerHosts.SingleOrDefault(sh => sh.NameDNS == localHost);
                if (host == null)
                    throw new ApplicationException("Unable to find a ServerHosts Table Entry that matches this machines host name [ " + localHost + " ]");


                // Load the ServerHost Information
                ServerHostInfo.Path           = host.Path;
                ServerHostInfo.ServerHostId   = host.Id;
                ServerHostInfo.ServerHostName = host.NameDNS;
                ServerHostInfo.ServerFQDN     = host.FQDN;
                ServerHostInfo.NodeKey        = nodeKey;


                // Ensure the TTL is expired.  This forces the Document Server Engine to load this information the first time it tries to access anything
                KeyObjectTTLExpirationUtc = DateTime.MinValue;

                // Load the required cached objects
                Result<bool>? result = CheckIfKeyEntitiesUpdated(db, true);

                if (result.IsFailed)
                {
                    string msg = "Failed to load the Key Object Caches.  This is required for the program to function correctly.  Errors: " + result.ToString();
                    _logger.Error(msg);
                    throw new ApplicationException(msg);
                }

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.Error("Error Setting up DocumentServerInformation:SetupAsync  |  " + ex.ToString());
            }
        }



        /// <summary>
        /// Checks to see if the Cache of Key Objects has exceeded its TTL time.  If so it calls method to check for any updates.
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        public Result CheckForExpiredCacheObjects(DocServerDbContext db)
        {
            // TODO Really need to log and not return Results!!!!


            // Nothing to do if the TTL is not expired.
            if (KeyObjectTTLExpirationUtc < DateTime.UtcNow)
                return Result.Ok();

            Result<bool> result = CheckIfKeyEntitiesUpdated(db);
            if (result.IsSuccess)
            {
                if (result.Value)

                    // TODO Change this to Configuration item
                    KeyObjectTTLExpirationUtc = DateTime.UtcNow.AddSeconds(10);
                return Result.Ok();
            }

            return Result.Fail(result.Errors);
        }


        /// <summary>
        /// Checks the VitalsInfo record VI_LASTKEYENTITY_UPDATED to see if it has been updated since the last time we loaded it.  If it has then
        /// we reload all the entities.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="initialLoad">If true, then this is an initial load and failures to load will throw errors, otherwise it enters a retry loop</param>
        /// <returns>Result with True Value = Means the KeyObjectTTLExpirationUtc needs to be set.  With false value means everything is okay, but do not set the TTL.</returns>
        /// <exception cref="ApplicationException"></exception>
        public Result<bool> CheckIfKeyEntitiesUpdated(DocServerDbContext db,
                                                      bool initialLoad = false)
        {
            // If TTL is still good nothing to do.
            //if (KeyObjectTTLExpirationUtc > DateTime.UtcNow)
            //  return Result.Ok();

            // If we already have a Write lock then some other thread is updating.  We return and use the current cache as is.
            if (CheckKeyEntitiesLock.IsWriteLockHeld)
                return Result.Ok(false);

            if (!CheckKeyEntitiesLock.TryEnterWriteLock(1))
                return Result.Ok(false);

            try
            {
                bool NeedToLoad = false;


                // Check the Vitals table to see if any key objects have been created or updated.  If so we need to reload.
                VitalInfo vitalInfo = db.VitalInfos.SingleOrDefault(v => v.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
                if (vitalInfo == null)
                    throw new ApplicationException("Unable to locate a VitalInfo record with the Id=" + VitalInfo.VI_LASTKEYENTITY_UPDATED);
                else if (vitalInfo.LastUpdateUtc > LastUpdateToKeyEntities)
                    NeedToLoad = true;

                if (!NeedToLoad)
                    return Result.Ok(true);

//                Console.WriteLine("CacheLock = ReadHeld: " + CachedLock.IsReadLockHeld + "  |  WriteHeld: " + CachedLock.IsWriteLockHeld + "  |  Upgradeable Held: " +
//                                  CachedLock.IsUpgradeableReadLockHeld);


                // Reload the Cached Entities
                Result result = LoadCachedObjects(db);
                if (result.IsFailed)
                    if (initialLoad)
                        throw new ApplicationException("Loading of cached objects failed.  " + result.ToString());
                    else
                    {
                        // TODO we should at least log this or something.
                        KeyObjectTTLExpirationUtc = DateTime.UtcNow.AddSeconds(10);
                        string msg = "Failed to load updated Cached Entities.  Setting recheck interval for 10 seconds";
                        _logger.Warning(msg);
                        return Result.Ok(false);
                    }


                // Set Last Check to same value as Vitals
                LastUpdateToKeyEntities = vitalInfo.LastUpdateUtc;
                return Result.Ok(true);
            }
            catch (Exception ex)
            {
                string msg = "CheckIfKeyEntitiesUpdated failed to run successfully.";
                _logger.Error(msg);
                return Result.Fail(new Error(msg).CausedBy(ex));
            }
            finally
            {
                CheckKeyEntitiesLock.ExitWriteLock();
            }
        }



        /// <summary>
        /// Reloads / Loads the Cached Objects
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        internal Result LoadCachedObjects(DocServerDbContext db)
        {
            bool cachedObjectsUpdated = false;

            //Console.WriteLine("Time:  " + DateTime.Now);
            //Console.WriteLine("Current Thread ID:  " + Thread.CurrentThread.ManagedThreadId);
            //Console.WriteLine("DSI ID:  " + Id);

            //Console.WriteLine("Recursive CachedLock Read Count: " + CachedLock.RecursiveReadCount);
            //Console.WriteLine("Recursive CachedLock Write Count: " + CachedLock.RecursiveWriteCount);

            if (!CachedLock.TryEnterUpgradeableReadLock(1000))
            {
                string msg = "Failed to acquire Upgradable Read Lock for [ CachedLock ].  Occassional errors of this type are ok.  Continued are probably a sign of a problem.";
                _logger.Warning(msg);

                // TODO Not sure what to do if this fails...
                return Result.Fail(new Error(msg));
            }


            //Console.WriteLine("A  |  CacheLock = ReadHeld: " + CachedLock.IsReadLockHeld + "  |  WriteHeld: " + CachedLock.IsWriteLockHeld + "  |  Upgradeable Held: " +
            //                  CachedLock.IsUpgradeableReadLockHeld);

            // ****************************************************************************************************
            // ************************************   VERY IMPORTANT  *********************************************
            // ****************************************************************************************************
            // You must not use any await 
            try
            {
                // Load Applications.  We add tokens to a Token Lookup and we add apps to dictionary
                Dictionary<string, Application> cachedApplicationTokenLookup = new();
                Dictionary<int, Application>    cachedApplications           = db.Applications.Where(a => a.IsActive == true).ToDictionary(a => a.Id);
                foreach (KeyValuePair<int, Application> cachedApplication in cachedApplications)
                    cachedApplicationTokenLookup.Add(cachedApplication.Value.Token, cachedApplication.Value);

                // Load Other Entities
                Dictionary<int, StorageNode>  cachedStorageNodes  = db.StorageNodes.Where(sn => sn.IsActive == true).ToDictionary(sn => sn.Id);
                Dictionary<int, DocumentType> cachedDocumentTypes = db.DocumentTypes.Where(dt => dt.IsActive == true).ToDictionary(dt => dt.Id);
                Dictionary<int, RootObject>   cachedRootObjects   = db.RootObjects.Where(ro => ro.IsActive == true).ToDictionary(ro => ro.Id);


                // TODO Now we need to Lock the caches while we replace/
                //Console.WriteLine("B  |  CacheLock = ReadHeld: " + CachedLock.IsReadLockHeld + "  |  WriteHeld: " + CachedLock.IsWriteLockHeld + "  |  Upgradeable Held: " +
                //                  CachedLock.IsUpgradeableReadLockHeld);
                if (!CachedLock.TryEnterWriteLock(2500))
                {
                    string msg = "Failed to acquire Write Lock [ CachedLock ].  Cached entries not updated!";
                    _logger.Error(msg);

                    //CachedLock.ExitUpgradeableReadLock();
                    return Result.Fail(msg);
                }

                try
                {
                    CachedApplicationTokenLookup = cachedApplicationTokenLookup;
                    CachedApplications           = cachedApplications;
                    CachedRootObjects            = cachedRootObjects;
                    CachedDocumentTypes          = cachedDocumentTypes;
                    CachedStorageNodes           = cachedStorageNodes;
                    _logger.Information("Cached Objects were successfully updated");
                    cachedObjectsUpdated = true;
                    return Result.Ok();
                }
                catch (Exception exception)
                {
                    _logger.Error("Exception during assignment of cached objects to new values.");
                    return Result.Fail(new Error("Failed to update the Cached entries").CausedBy(exception));
                }
                finally
                {
                    //Console.WriteLine("Exiting WriteLock  for [ CachedLock ]");
                    CachedLock.ExitWriteLock();
                }
            }
            catch (Exception exception)
            {
                _logger.Error("Exception during Loading of Cached Objects.  Cached Entries Failed to update.  Repeated same errors can cause issues.");
                return Result.Fail(new Error("Failed to Load CachedObjects").CausedBy(exception));
            }
            finally
            {
                //Console.WriteLine("C  |  CacheLock = ReadHeld: " + CachedLock.IsReadLockHeld + "  |  WriteHeld: " + CachedLock.IsWriteLockHeld + "  |  Upgradeable Held: " +
                //                  CachedLock.IsUpgradeableReadLockHeld);
                //Console.WriteLine("Exiting UpgradeableReadLock for [ CachedLock ]");
                CachedLock.ExitUpgradeableReadLock();

                //Console.WriteLine("Time:  " + DateTime.Now);
            }
        }


        /// <summary>
        /// This is when the Key Object TTL expires. When it expires the Key Object needs to be checked to determine if it is still valid
        /// </summary>
        public DateTime KeyObjectTTLExpirationUtc { get; set; }

        /// <summary>
        /// This is the date of the Last Update to Key Entities.  This is used to determine if there are new updates or changes we need to load.
        /// </summary>
        public DateTime LastUpdateToKeyEntities { get; set; } = DateTime.MinValue;


        public Dictionary<string, Application> CachedApplicationTokenLookup { get; private set; } = new Dictionary<string, Application>();
        public Dictionary<int, Application> CachedApplications { get; private set; } = new();
        public Dictionary<int, RootObject> CachedRootObjects { get; private set; } = new();
        public Dictionary<int, DocumentType> CachedDocumentTypes { get; private set; } = new();
        public Dictionary<int, StorageNode> CachedStorageNodes { get; private set; } = new();

        /// <summary>
        /// Lock used to syncrhonize access to the Caches during
        /// </summary>
        private ReaderWriterLockSlim CachedLock = new ReaderWriterLockSlim();

        /// <summary>
        /// This is used to see if we are already checking to see if there are updated Key Entities.  We only allow the check to happen once.
        /// </summary>
        private ReaderWriterLockSlim CheckKeyEntitiesLock = new ReaderWriterLockSlim();


        /// <summary>
        /// Returns an Application with the given Id from the Application Cache
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Result<Application> GetCachedApplication(int id)
        {
            if (!CachedLock.TryEnterReadLock(2000))
            {
                string msg = "Failed to acquire a lock to enter Read mode for the CachedApplications.  Cannot locate an application.";
                _logger.Error(msg);
                return Result.Fail(new Error(msg));
            }


            try
            {
                Application application;
                if (!CachedApplications.TryGetValue(id, out application))
                {
                    string msg = "No Active Application with Id [ " + id + " ] exists in the cache.";
                    _logger.Error(msg);
                    return Result.Fail(msg);
                }

                return Result.Ok(application);
            }
            catch (Exception ex)
            {
                string msg = "Error looking for Application with Id [ " + id + " ] in the Application Cache.";
                _logger.Error(msg + "  |  " + ex.ToString());
                return Result.Fail(new Error(msg).CausedBy(ex));
            }
            finally
            {
                CachedLock.ExitReadLock();
            }
        }



        /// <summary>
        /// Returns a Storage Node with the given Id from the Storage Node Cache
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Result<StorageNode> GetCachedStorageNode(int id)
        {
            if (!CachedLock.TryEnterReadLock(2000))
                return Result.Fail(new Error("Failed to acquire a lock to enter Read mode for the Cached StorageNodes."));

            try
            {
                StorageNode storageNode;
                if (!CachedStorageNodes.TryGetValue(id, out storageNode))
                    return Result.Fail("No Active StorageNode with Id [ " + id + " ] exists in the cache.");

                return Result.Ok(storageNode);
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error("Error looking for StorageNode with Id [ " + id + " ] in the StorageNode Cache.").CausedBy(ex));
            }
            finally
            {
                CachedLock.ExitReadLock();
            }
        }



        /// <summary>
        /// Returns a Document Type with the given Id from the Document Type Cache
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Result<DocumentType> GetCachedDocumentType(int id)
        {
            if (!CachedLock.TryEnterReadLock(2000))
                return Result.Fail(new Error("Failed to acquire a lock to enter Read mode for the Cached DocumentTypes."));

            try
            {
                DocumentType documentType;
                if (!CachedDocumentTypes.TryGetValue(id, out documentType))
                    return Result.Fail("No Active Document Type with Id [ " + id + " ] exists in the cache.");

                return Result.Ok(documentType);
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error("Error looking for DocumentType with Id [ " + id + " ] in the DocumentType Cache.").CausedBy(ex));
            }
            finally
            {
                CachedLock.ExitReadLock();
            }
        }


        /// <summary>
        /// Information about the host
        /// </summary>
        public ServerHostInfo ServerHostInfo { get; set; }
    }


    /// <summary>
    /// Information about this host.
    /// </summary>
    public class ServerHostInfo
    {
        /// <summary>
        /// The Host ID of this physical server we are running on
        /// </summary>
        public short ServerHostId { get; set; }

        public string ServerHostName { get; set; }

        public string ServerFQDN { get; set; }

        /// <summary>
        /// Path to the data files on this host.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Unique Value that all nodes must agree on to talk to each other.
        /// </summary>
        public string NodeKey { get; set; }
    }
}