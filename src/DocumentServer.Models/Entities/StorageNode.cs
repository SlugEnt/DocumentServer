using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DocumentServer.Models.Enums;

namespace DocumentServer.Models.Entities;

public class StorageNode : AbstractBaseEntity
{
    /// <summary>
    /// Constructor
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
                       string nodePath)
    {
        Name                = name;
        Description         = description;
        IsTestNode          = isTestNode;
        StorageNodeLocation = storageNodeLocation;
        storageNodeSpeed    = StorageSpeed;
        NodePath            = nodePath;

        IsActive = false;
    }


    /// <summary>
    /// Empty constructor
    /// </summary>
    public StorageNode() { }



    [MaxLength(400)] public string Description { get; set; }
    [Key] public ushort Id { get; set; }

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
    [MaxLength(500)]
    public string NodePath { get; set; }

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
        public List<DocumentType> ActiveNode1DocumentTypes { get; set; }
        public List<DocumentType> ActiveNode2DocumentTypes { get; set; }
        public List<DocumentType> ArchivalNode1DocumentTypes { get; set; }
        public List<DocumentType> ArchivalNode2DocumentTypes { get; set; }

        public List<StoredDocument> PrimaryNodeStoredDocuments { get; set; }
        public List<StoredDocument> SecondaryNodeStoredDocuments { get; set; }
    // @formatter:on — disable formatter after this line
}