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

namespace SlugEnt.DocumentServer.Core
{
    /// <summary>
    /// Contains configuration and other information about the Document Server Engine between instances of it.
    /// </summary>
    public class DocumentServerInformation
    {
        public DocumentServerInformation() { }


        public DocumentServerInformation(DocServerDbContext docServerDbContext)
        {
            ServerHostInfo = new ServerHostInfo();
            Initialize     = SetupAsync(docServerDbContext);
        }


        public DocumentServerInformation(IConfiguration configuration,
                                         IOptions<DocumentServerFromAppSettings> docOptions)
        {
            string?                                     sqlConn = configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName());
            DbContextOptionsBuilder<DocServerDbContext> options = new();
            options.UseSqlServer(sqlConn);
            DocServerDbContext db = new(options.Options);
            ServerHostInfo = new ServerHostInfo();
            Initialize     = SetupAsync(db);
        }


        /// <summary>
        /// Task used to perform setup during object creation.
        /// </summary>
        public Task Initialize { get; }


        /// <summary>
        /// Returns true if initialization has completed.
        /// </summary>
        public bool IsInitialized { get; private set; } = false;


        //[Obsolete]
        //public void PostSetup(DocServerDbContext docServerDbContext) { SetupAsync(docServerDbContext); }


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


            // Load applications from DB into an internal Dictionary so we can speed up Application token authentication
            List<Application> apps = await db.Applications.Where(a => a.IsActive == true).ToListAsync();
            foreach (Application app in apps)
            {
                ApplicationTokenLookup.Add(app.Token, app);
            }

            IsInitialized = true;
        }


        public ServerHostInfo ServerHostInfo { get; set; }

        public Dictionary<string, Application> ApplicationTokenLookup { get; private set; } = new Dictionary<string, Application>();
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