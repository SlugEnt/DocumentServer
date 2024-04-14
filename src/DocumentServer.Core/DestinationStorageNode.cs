using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using SlugEnt.DocumentServer.Models.Entities;

namespace SlugEnt.DocumentServer.Core;

internal class DestinationStorageNode
{
    /// <summary>
    /// The StorageNode to be used
    /// </summary>
    public StorageNode StorageNode { get; set; }

    //public int StorageNodeId { get; set; }

    public bool IsOnThisHost { get; set; } = false;

    public bool IsPrimaryDocTypeStorage { get; set; }

    public string FullFileNameAsStored { get; set; }
}