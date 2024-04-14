using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using SlugEnt.DocumentServer.Db;
using System.Security.Cryptography;

namespace SlugEnt.DocumentServer.Core
{
    /// <summary>
    /// Responsible for Setting up the Document Server Engine FromAppSettings Object
    /// </summary>
    public class DocumentServerInformationBuilder
    {
        private          DocumentServerInformation _dsi             = new();
        private          DocServerDbContext?       _dbContext       = null;
        private          IConfiguration?           _configuration   = null;
        private          string                    _testDB          = "";
        private          string                    _nodeKey         = "";
        private          string                    _overrideDNSName = "";
        private readonly Serilog.ILogger           _logger;


        public DocumentServerInformationBuilder(Serilog.ILogger logger)
        {
            _logger         = logger;
            _dsi.SeriLogger = logger;
        }


        /// <summary>
        /// This method will allow the builder to use some information from the Configuration object to build the
        /// DocumentServerInformation object.
        /// <para>  It can set the Database Connection String from Configuration.</para>
        /// <para>  It can set the NodeKey from Configuration.</para>
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public DocumentServerInformationBuilder UseConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
            return this;
        }


        /// <summary>
        /// Sets the database to be used during initialization.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public DocumentServerInformationBuilder UseDatabase(DocServerDbContext dbContext)
        {
            _dbContext = dbContext;
            return this;
        }


        /// <summary>
        /// This should only be used during Testing to override the database to be used
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public DocumentServerInformationBuilder TestUseDatabase(string connectionString)
        {
            _testDB = connectionString;
            return this;
        }


        /// <summary>
        /// Sets the NodeKey to be used during node to node communication
        /// </summary>
        /// <param name="nodeKey"></param>
        /// <returns></returns>
        public DocumentServerInformationBuilder UseNodeKey(string nodeKey)
        {
            _nodeKey = nodeKey;
            return this;
        }


        /*
        /// <summary>
        /// Sets the Serilogger to be used for logging
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public DocumentServerInformationBuilder UseSeriLog(Serilog.ILogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException("A Valid Serilogger must be specified.");

            _dsi.SeriLogger = logger;
            return this;
        }
        */


        /// <summary>
        /// Sets the Cache Expiration time in Milliseconds
        /// </summary>
        /// <param name="expirationInMs"></param>
        /// <returns></returns>
        public DocumentServerInformationBuilder SetCacheExpiration(long expirationInMs)
        {
            _dsi.CacheExpirationTTL = expirationInMs;
            return this;
        }


        /// <summary>
        /// For Testing purposes only.  Serves no use in production.  Sets the remote port that the 2nd instance should listen on.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public DocumentServerInformationBuilder TestRemoteNodePort(int port)
        {
            _dsi.RemoteNodePort = port;
            return this;
        }


        /// <summary>
        /// For Testing there is a need to Override the Server DNS name in some cases.
        /// </summary>
        /// <param name="overrideServerDNSName"></param>
        /// <returns></returns>
        public DocumentServerInformationBuilder TestOverrideServerDNSName(string overrideServerDNSName)
        {
            _overrideDNSName = overrideServerDNSName;
            return this;
        }


        /// <summary>
        /// Builds the DocumentServerInformation object based upon the specified configuration
        /// </summary>
        /// <returns></returns>
        public DocumentServerInformation Build()
        {
            if (_dbContext == null)
                BuildDBContext();
            if (_nodeKey == string.Empty)
                BuildNodeKey();
            if (_dsi.SeriLogger == null)
                throw new ArgumentNullException("You did not specify a SeriLogger during configuration of DocumentServerInformation.  This is required.");

            _dsi.Initialize = _dsi.SetupAsync(_dbContext, _nodeKey, _overrideDNSName);
            return _dsi;
        }


        /// <summary>
        /// Waits for the DocumentServerInformation object to Finish initialization.  There are only 2 return paths from this function:
        /// <para> 1) It returns successfully.</para>
        /// <para> 2) It throws an error because it failed to initialize.</para>
        /// </summary>
        /// <exception cref="ApplicationException"></exception>
        public void AwaitInitialization()
        {
            if (_dsi.IsInitialized == false)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (_dsi.Initialize.IsFaulted)
                        throw new ApplicationException("Error 9863:  Error in DocumentServerInformation object.  Cannot run without this object!");

                    if (_dsi.IsInitialized == true)
                        break;

                    Thread.Sleep(50);
                }

                if (!_dsi.IsInitialized)
                    throw new ApplicationException("Error 9844: Failed to succesfully initialize DocumentServerInformation object.  Cannot run without this object initialized.");
            }
        }


        /// <summary>
        /// Builds the DocumentServerInformation object and waits for its Initialization
        /// </summary>
        /// <returns></returns>
        public async Task<DocumentServerInformation> BuildAndAwaitInitialization()
        {
            DocumentServerInformation dsi = Build();
            await dsi.Initialize;

            //AwaitInitialization();
            return dsi;
        }


        /// <summary>
        /// Builds a DBContext to be used to initially build the DocumentServerInformation obecjt
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        internal void BuildDBContext()
        {
            if (_configuration == null)
                throw new
                    ArgumentNullException("The DocumentServerInformation Builder needs to have a UseDatabase value set OR a UseConfiguration value with a database connection string.  Neither were supplied.  Unable to build the object.");

            // If we have a TestDB Connection string we should use that, otherwise read from Configuration
            string sqlConn = _testDB == string.Empty ? _configuration.GetConnectionString(DocServerDbContext.DatabaseReferenceName()) : _testDB;

            DbContextOptionsBuilder<DocServerDbContext> options = new();
            options.UseSqlServer(sqlConn);

            _dbContext = new(options.Options);
        }


        /// <summary>
        /// Retrieves the NodeKey from Configuration
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ApplicationException"></exception>
        internal void BuildNodeKey()
        {
            if (_configuration == null)
                throw new
                    ArgumentNullException("The DocumentServerInformationBuilder needs to have a NodeKey set.  However, one was not explicitly specifed with UseNodeKey OR a UseConfiguration value with a nodekey specified must be set.  Neither were supplied.  Unable to build the object.");

            _nodeKey = _configuration.GetValue<string>("DocumentServer:NodeKey");
            if (!string.IsNullOrWhiteSpace(_nodeKey))
                _logger.Warning("DocumentServerInformationBuilder:  Found NodeKey!");
            else
            {
                string msg = "Failed to find NodeKey in the configuration.  This is a required property.";
                _logger.Error(msg);
                throw new ApplicationException(msg);
            }
        }
    }
}