﻿using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;

namespace DocumentServer.Core;

/// <summary>
///     Represents a Document that attempted to have some operation done to it along with some status information,  such as
///     errors when trying to store, etc.
/// </summary>
public class DocumentOperationStatus : Result<StoredDocument>
{
    /*
    public DocumentOperationStatus(StoredDocument storedDocument) { StoredDocument = storedDocument; }

    /// <summary>
    /// The ID of the object to be stored.StoredDocument Object
    /// </summary>
    public StoredDocument StoredDocument { get; set; }

    /// <summary>
    /// The Exception that was generated during the stored document operation
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Whether the StoredObject was successfully stored, retrieved, updated, etc
    /// </summary>
    public bool IsErrored { get; set; }


    public void SetError(string errorMessage)
    {
        IsErrored    = true;
        ErrorMessage = errorMessage;
    }
    */
}