using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SlugEnt.DocumentServer.Core;

internal class DestinationStorageNode
{
    public int StorageNodeId { get; set; }
    public bool IsOnThisHost { get; set; } = false;

    public bool IsPrimaryDocTypeStorage { get; set; }

    public string FullFileNameAsStored { get; set; }
}