﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Capgemini.DataMigration.Core;
using Capgemini.DataMigration.Core.Extensions;
using Capgemini.DataMigration.Exceptions;
using Capgemini.Xrm.DataMigration.Config;
using Capgemini.Xrm.DataMigration.DataStore;
using Capgemini.Xrm.DataMigration.FileStore.Model;
using Capgemini.Xrm.DataMigration.Helpers;
using Microsoft.Xrm.Sdk;

namespace Capgemini.Xrm.DataMigration.FileStore.DataStore
{
    public class DataFileStoreWriter : IDataStoreWriter<Entity, EntityWrapper>
    {
        private readonly ILogger logger;
        private readonly string filePrefix;
        private readonly string filesPath;
        private readonly bool seperateFilesPerEntity;
        private readonly List<string> excludedFields;

        private int currentBatchNo;
        private int fileNo;
        private string lastEntity;

        public DataFileStoreWriter(ILogger logger, IFileStoreWriterConfig config)
            : this(
                  logger,
                  config?.FilePrefix,
                  config?.JsonFolderPath,
                  config?.ExcludedFields,
                  config != null && config.SeperateFilesPerEntity)
        {
        }

        public DataFileStoreWriter(ILogger logger, string filePrefix, string filesPath, List<string> excludedFields = null, bool seperateFilesPerEntity = true)
        {
            logger.ThrowArgumentNullExceptionIfNull(nameof(logger));
            filePrefix.ThrowArgumentNullExceptionIfNull(nameof(filePrefix));
            filesPath.ThrowArgumentNullExceptionIfNull(nameof(filesPath));

            this.logger = logger;
            this.filePrefix = filePrefix;
            this.filesPath = filesPath;
            this.seperateFilesPerEntity = seperateFilesPerEntity;
            this.excludedFields = excludedFields;
        }

        public void Reset()
        {
            logger.LogInfo("DataFileStoreWriter Reset performed");
            currentBatchNo = 0;
            fileNo = 0;
            lastEntity = null;
        }

        public void SaveBatchDataToStore(List<EntityWrapper> entities)
        {
            entities.ThrowArgumentNullExceptionIfNull(nameof(entities));

            currentBatchNo++;

            logger.LogVerbose($"DataFileStoreWriter SaveBatchDataToStore started, records:{entities.Count}, batchNo:{currentBatchNo}");

            var entitiesToExport = entities.Select(e => new CrmEntityStore(e)).ToList();

            if (excludedFields != null && excludedFields.Any())
            {
                foreach (var item in entitiesToExport)
                {
                    item.Attributes.RemoveAll(p => excludedFields.Contains(p.AttributeName));
                }
            }

            var exportedStore = new CrmExportedDataStore
            {
                RecordsCount = entitiesToExport.Count,
            };
            exportedStore.ExportedEntities.AddRange(entitiesToExport);

            var batchFile = GetFileNameForBatchNo(currentBatchNo, entities[0].LogicalName);

            if (File.Exists(batchFile))
            {
                throw new ConfigurationException($"Store File {batchFile} already exists, clean the store folder first");
            }

            using (FileStream stream = File.OpenWrite(batchFile))
            {
                JsonHelper.Serialize(exportedStore, stream);
            }

            logger.LogVerbose("DataFileStoreWriter SaveBatchDataToStore finished");
        }

        private string GetFileNameForBatchNo(int batchNo, string entName)
        {
            if (string.IsNullOrEmpty(entName) || !seperateFilesPerEntity)
            {
                return Path.Combine(filesPath, $"{filePrefix}_{batchNo}.json");
            }

            if (string.IsNullOrWhiteSpace(lastEntity) || lastEntity != entName)
            {
                fileNo = 0;
                lastEntity = entName;
            }

            fileNo++;

            return Path.Combine(filesPath, filePrefix + "_" + entName + "_" + fileNo + ".json");
        }
    }
}