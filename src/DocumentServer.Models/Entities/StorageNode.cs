﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SlugEnt.DocumentServer.Models.Enums;

namespace SlugEnt.DocumentServer.Models.Entities;

/// <summary>
/// Storage Node, represents a storage location.  It identifies both the server and the root of the path where documents should be stored.
/// </summary>
public class StorageNode : AbstractKeyEntity
{
    [Key] public int Id { get; set; }

    [MaxLength(400)] public string Description { get; set; }


    /// <summary>
    ///     If true the Node can be used for production use
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    ///     If true the node is a testing node, meaning its data can be periodically wiped no matter the Storage Mode set for
    ///     the document
    /// </summary>
    public bool IsTestNode { get; set; }

    [MaxLength(50)] public string Name { get; set; }


    /// <summary>
    ///     The fully qualified path this node stores its files at.
    /// </summary>
    [MaxLength(100)]
    public string NodePath { get; set; }


    /// <summary>
    /// The host this node is on.
    /// </summary>
    [Column(TypeName = "smallint")]
    public short ServerHostId { get; set; }

    public ServerHost ServerHost { get; set; }

    /// <summary>
    ///     Where this storage node stores its data - SMB or S3.
    /// </summary>
    [Column(TypeName = "tinyint")]
    public EnumStorageNodeLocation StorageNodeLocation { get; set; }



    /// <summary>
    ///     Indicates how fast this storage node is.
    /// </summary>
    [Column(TypeName = "tinyint")]
    public EnumStorageNodeSpeed StorageSpeed { get; set; }


    // @formatter:off — disable formatter after this line
    public virtual List<DocumentType>? ActiveNode1DocumentTypes { get; set; }
    public virtual List<DocumentType>? ActiveNode2DocumentTypes { get; set; }
    public virtual List<DocumentType>? ArchivalNode1DocumentTypes { get; set; }
    public virtual List<DocumentType>? ArchivalNode2DocumentTypes { get; set; }
    public virtual List<StoredDocument> PrimaryNodeStoredDocuments { get; set; }
    public virtual List<StoredDocument> SecondaryNodeStoredDocuments { get; set; }


    // Nodes Replicating to/from Collections
    public virtual ICollection<ReplicationTask>? ReplicationTaskToNodes {get;set;}
    public virtual ICollection<ReplicationTask>? ReplicationTaskFromNodes { get; set;}
    // @formatter:on — disable formatter after this line


#region "Non Fields"

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="isTestNode"></param>
    /// <param name="storageNodeLocation"></param>
    /// <param name="storageNodeSpeed"></param>
    /// <param name="nodePath"></param>
    public StorageNode(string name,
                       string description,
                       bool isTestNode,
                       EnumStorageNodeLocation storageNodeLocation,
                       EnumStorageNodeSpeed storageNodeSpeed,
                       string nodePath,
                       bool isActive = false)
    {
        Name                = name;
        Description         = description;
        IsTestNode          = isTestNode;
        StorageNodeLocation = storageNodeLocation;
        StorageSpeed        = storageNodeSpeed;
        NodePath            = nodePath;

        IsActive = isActive;
    }


    /// <summary>
    ///     Empty constructor
    /// </summary>
    public StorageNode() { }


    /// <summary>
    ///     For displaying information about this in an error type message
    /// </summary>
    [NotMapped]
    public string ErrorMessage
    {
        get
        {
            string className = GetType().Name;
            string msg = string.Format("{0}:  [Id: {1} | Name: {2} ]",
                                       className,
                                       Id,
                                       Name);
            return msg;
        }
    }

#endregion
}