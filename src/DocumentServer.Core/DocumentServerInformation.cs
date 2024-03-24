using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;

namespace SlugEnt.DocumentServer.Core
{
    /// <summary>
    /// Contains configuration and other information about the Document Server Engine between instances of it.  There should only be one instance of this object per application
    /// It should be instantiated as a singleton!
    /// </summary>
    public class DocumentServerInformation
    {
        public static DocumentServerInformation Create(IConfiguration configuration,
                                                       DocServerDbContext docServerDbContext = null)
        {
            if (docServerDbContext == null)
            {
                string?                                     sqlConn = configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName());
                DbContextOptionsBuilder<DocServerDbContext> options = new();
                options.UseSqlServer(sqlConn);
                docServerDbContext = new(options.Options);
            }

            return new DocumentServerInformation(docServerDbContext);
        }


        public DocumentServerInformation(DocServerDbContext docServerDbContext)
        {
            ServerHostInfo = new ServerHostInfo();
            Initialize     = SetupAsync(docServerDbContext);
        }


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
        /// <exception cref="ApplicationException"></exception>
        private async Task SetupAsync(DocServerDbContext db)
        {
            string localHost = Dns.GetHostName();


            ServerHost? host = db.ServerHosts.SingleOrDefault(sh => sh.NameDNS == localHost);
            if (host == null)
                throw new ApplicationException("Unable to find a ServerHosts Table Entry that matches this machines host name [ " + localHost + " ]");


            // Load the ServerHost Information
            ServerHostInfo.Path           = host.Path;
            ServerHostInfo.ServerHostId   = host.Id;
            ServerHostInfo.ServerHostName = host.NameDNS;
            ServerHostInfo.ServerFQDN     = host.FQDN;

            // Ensure the TTL is expired.  This forces the Document Server Engine to load this information the first time it tries to access anything
            KeyObjectTTLExpirationUtc = DateTime.MinValue;

            // Load the required cached objects
            Result result = await CheckIfKeyEntitiesUpdated(db);
            if (result.IsFailed) { }

            IsInitialized = true;
        }


        public async Task<Result> CheckIfKeyEntitiesUpdated(DocServerDbContext db)
        {
            bool NeedToLoad = false;

            // If TTL is still good nothing to do.
            if (KeyObjectTTLExpirationUtc > DateTime.UtcNow)
                return Result.Ok();

            // Check the Vitals table to see if any key objects have been created or updated.  If so we need to reload.
            VitalInfo vitalInfo = await db.VitalInfos.SingleOrDefaultAsync(v => v.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
            if (vitalInfo == null)
                throw new ApplicationException("Unable to locate a VitalInfo record with the Id=" + VitalInfo.VI_LASTKEYENTITY_UPDATED);
            else if (vitalInfo.LastUpdateUtc > LastUpdateToKeyEntities)
                NeedToLoad = true;

            if (!NeedToLoad)
                return Result.Ok();

            // Load Vital Info.
            Result result = await LoadCachedObjects(db);
            if (result.IsFailed)
                throw new ApplicationException("Loading of cached objects failed.  " + result.ToString());

            // Set Last Check to same value as Vitals
            LastUpdateToKeyEntities = vitalInfo.LastUpdateUtc;
            return Result.Ok();
        }



        /// <summary>
        /// Reloads / Loads the Cached Objects
        /// </summary>
        /// <param name="db"></param>
        /// <returns></returns>
        internal async Task<Result> LoadCachedObjects(DocServerDbContext db)
        {
            try
            {
                // Load Applications.  We add tokens to a Token Lookup and we add apps to dictionary
                //List<Application> apps = await db.Applications.Where(a => a.IsActive == true).ToListAsync();
                Dictionary<string, Application> cachedApplicationTokenLookup = new();
                Dictionary<int, Application>    cachedApplications           = await db.Applications.Where(a => a.IsActive == true).ToDictionaryAsync(a => a.Id);
                foreach (KeyValuePair<int, Application> cachedApplication in cachedApplications)
                    cachedApplicationTokenLookup.Add(cachedApplication.Value.Token, cachedApplication.Value);

                // Load Other Entities
                Dictionary<int, StorageNode>  cachedStorageNodes  = await db.StorageNodes.Where(sn => sn.IsActive == true).ToDictionaryAsync(sn => sn.Id);
                Dictionary<int, DocumentType> cachedDocumentTypes = await db.DocumentTypes.Where(dt => dt.IsActive == true).ToDictionaryAsync(dt => dt.Id);
                Dictionary<int, RootObject>   cachedRootObjects   = await db.RootObjects.Where(ro => ro.IsActive == true).ToDictionaryAsync(ro => ro.Id);


                // TODO Now we need to Lock the caches while we replace/
                CachedApplicationTokenLookup = cachedApplicationTokenLookup;
                CachedApplications           = cachedApplications;
                CachedRootObjects            = cachedRootObjects;
                CachedDocumentTypes          = cachedDocumentTypes;
                CachedStorageNodes           = cachedStorageNodes;
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(new Error("Failed to Load CachedObjects").CausedBy(ex));
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


        public string Path { get; set; }
    }
}