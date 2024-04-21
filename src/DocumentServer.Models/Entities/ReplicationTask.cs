using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Models.Entities
{
    /// <summary>
    /// Keeps track of StoredDocuments that need to be replicated to another server.
    /// </summary>
    public class ReplicationTask : AbstractBaseEntity
    {
        [Key] public long Id { get; set; }

        [Required] public long StoredDocumentId { get; set; }
        public StoredDocument StoredDocument { get; set; }


        // Storage Nodes 
        [Required] public int ReplicateToStorageNodeId { get; set; }
        public StorageNode ReplicateToStorageNode { get; set; }

        [Required] public int ReplicateFromStorageNodeId { get; set; }
        public StorageNode ReplicateFromStorageNode { get; set; }
    }
}