﻿using Microsoft.EntityFrameworkCore;
using SlugEnt.DocumentServer.Models.Entities;
using SlugEnt.FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlugEnt.DocumentServer.Core
{
    /// <summary>
    /// Contains logic to save and update entities to the Database to ensure fields are correctly set and on updates, some fields are not updated.
    /// </summary>
    public class EntityRules
    {
        private DocServerDbContext _db;

        public EntityRules(DocServerDbContext db) { _db = db; }


        /// <summary>
        /// This is the preferred method of saving a document type.  It ensures the VitalInfo record is updated which is critical to informating the
        /// API's and services that key information has changed.
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>
        public async Task<Result> SaveDocumentTypeAsync(DocumentType documentType)
        {
            try
            {
                if (documentType.Id > 0) { }
                else
                {
                    await _db.AddAsync(documentType);
                }

                VitalInfo vitalInfo = await _db.VitalInfos.SingleOrDefaultAsync(v => v.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
                vitalInfo.LastUpdateUtc = DateTime.UtcNow;

                int rowsUpdated = await _db.SaveChangesAsync();
                if (rowsUpdated > 0)
                    return Result.Ok();

                return Result.Fail("The database report it did not update any rows of data.  Expecting at least 1 to indicate success.");
            }
            catch (Exception exception)
            {
                return Result.Fail(new Error("Failed to save the Application to Database").CausedBy(exception));
            }
        }


        /// <summary>
        /// This is the preferred method of saving an Application.  It ensures the VitalInfo record is updated which is critical to informating the
        /// API's and services that key information has changed.
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        public async Task<Result> SaveApplicationAsync(Application application)
        {
            try
            {
                if (application.Id > 0) { }

                // Is a new Application
                else
                {
                    // Create App Token
                    string guid = Guid.NewGuid().ToString("N");
                    application.Token = guid;
                    await _db.AddAsync(application);
                }

                VitalInfo vitalInfo = await _db.VitalInfos.SingleOrDefaultAsync(v => v.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
                vitalInfo.LastUpdateUtc = DateTime.UtcNow;
                int rowsUpdated = await _db.SaveChangesAsync();
                if (rowsUpdated > 0)
                    return Result.Ok();

                return Result.Fail("The database report it did not update any rows of data.  Expecting at least 1 to indicate success.");
            }
            catch (Exception exception)
            {
                return Result.Fail(new Error("Failed to save the Application to Database").CausedBy(exception));
            }
        }



        /// <summary>
        /// This is the preferred method of saving a RootObject.  It ensures the VitalInfo record is updated which is critical to informating the
        /// API's and services that key information has changed.
        /// </summary>
        /// <param name="rootObject"></param>
        /// <returns></returns>
        public async Task<Result> SaveRootObjectAsync(RootObject rootObject)
        {
            try
            {
                if (rootObject.Id > 0) { }

                // Its a new Rootobject
                else
                {
                    await _db.AddAsync(rootObject);
                }

                VitalInfo vitalInfo = await _db.VitalInfos.SingleOrDefaultAsync(v => v.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
                vitalInfo.LastUpdateUtc = DateTime.UtcNow;

                int rowsUpdated = await _db.SaveChangesAsync();
                if (rowsUpdated > 0)
                    return Result.Ok();

                return Result.Fail("The database report it did not update any rows of data.  Expecting at least 1 to indicate success.");
            }
            catch (Exception exception)
            {
                return Result.Fail(new Error("Failed to save the Application to Database").CausedBy(exception));
            }
        }



        /// <summary>
        /// This is the preferred method of saving a StorageNode.  It ensures the VitalInfo record is updated which is critical to informating the
        /// API's and services that key information has changed.
        /// </summary>
        /// <param name="storageNode"></param>
        /// <returns></returns>
        public async Task<Result> SaveStorageNodeAsync(StorageNode storageNode)
        {
            try
            {
                if (storageNode.Id > 0) { }
                else
                {
                    await _db.AddAsync(storageNode);
                }

                VitalInfo vitalInfo = await _db.VitalInfos.SingleOrDefaultAsync(v => v.Id == VitalInfo.VI_LASTKEYENTITY_UPDATED);
                vitalInfo.LastUpdateUtc = DateTime.UtcNow;

                int rowsUpdated = await _db.SaveChangesAsync();
                if (rowsUpdated > 0)
                    return Result.Ok();

                return Result.Fail("The database report it did not update any rows of data.  Expecting at least 1 to indicate success.");
            }
            catch (Exception exception)
            {
                return Result.Fail(new Error("Failed to save the Application to Database").CausedBy(exception));
            }
        }
    }
}