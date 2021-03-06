﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Capgemini.DataMigration.Core;
using Capgemini.DataMigration.Core.Tests.Base;
using Capgemini.Xrm.DataMigration.Config;
using Capgemini.Xrm.DataMigration.FileStore.DataStore;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Moq;

namespace Capgemini.Xrm.DataMigration.FileStore.UnitTests.DataStore
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class DataFileStoreReaderTest : UnitTestBase
    {
        private CrmSchemaConfiguration crmSchemaConfiguration;

        private DataFileStoreReaderCsv systemUnderTest;

        [TestInitialize]
        public void Setup()
        {
            InitializeProperties();
            crmSchemaConfiguration = new CrmSchemaConfiguration();
        }

        [TestMethod]
        [TestCategory(TestBase.AutomatedTestCategory)]
        public void ReadTestFileSystemuser()
        {
            var result = GetFirstEntities("ExportedDataSystemUser");
            Assert.AreEqual(result.Item1.Count, result.Item2.Count);
            List<Entity> firstEntList = result.Item1;
            List<Entity> firstEntJsonList = result.Item2;

            int idx = 0;
            while (idx < firstEntList.Count)
            {
                var firstEnt = firstEntList[idx];
                var firstEntJson = firstEntJsonList[idx];
                CompareAttributes(firstEnt, firstEntJson, true);
                idx++;
            }
        }

        [TestMethod]
        [TestCategory(TestBase.AutomatedTestCategory)]
        public void ReadTestFileUserprofile()
        {
            var result = GetFirstEntities("ExportedDataUserProfile");
            Assert.AreEqual(result.Item1.Count, result.Item2.Count);

            List<Entity> firstEntList = result.Item1;
            List<Entity> firstEntJsonList = result.Item2;

            int idx = 0;
            while (idx < firstEntList.Count)
            {
                var firstEnt = firstEntList[idx];
                var firstEntJson = firstEntJsonList[idx];
                CompareAttributes(firstEnt, firstEntJson);
                idx++;
            }
        }

        [TestMethod]
        [TestCategory(TestBase.AutomatedTestCategory)]
        public void ReadTestFileUserrole()
        {
            var result = GetFirstEntities("ExportedDataUserRole");
            Assert.AreEqual(result.Item1.Count, result.Item2.Count);

            List<Entity> firstEntList = result.Item1;
            List<Entity> firstEntJsonList = result.Item2;

            int idx = 0;
            while (idx < firstEntList.Count)
            {
                var firstEnt = firstEntList[idx];
                var firstEntJson = firstEntJsonList[idx];
                CompareAttributes(firstEnt, firstEntJson);
                idx++;
            }
        }

        [TestMethod]
        [TestCategory(TestBase.AutomatedTestCategory)]
        public void ReadTestFileTeammem()
        {
            var result = GetFirstEntities("ExportedDataTeamMem");
            Assert.AreEqual(result.Item1.Count, result.Item2.Count);
            List<Entity> firstEntList = result.Item1;
            List<Entity> firstEntJsonList = result.Item2;

            int idx = 0;
            while (idx < firstEntList.Count)
            {
                var firstEnt = firstEntList[idx];
                var firstEntJson = firstEntJsonList[idx];
                CompareAttributes(firstEnt, firstEntJson);
                idx++;
            }
        }

        [TestMethod]
        [TestCategory(TestBase.AutomatedTestCategory)]
        public void ReadTestFileUserset()
        {
            var result = GetFirstEntities("ExportedDataUserSet");
            Assert.AreEqual(result.Item1.Count, result.Item2.Count);
            List<Entity> firstEntList = result.Item1;
            List<Entity> firstEntJsonList = result.Item2;

            int idx = 0;
            while (idx < firstEntList.Count)
            {
                var firstEnt = firstEntList[idx];
                var firstEntJson = firstEntJsonList[idx];
                CompareAttributes(firstEnt, firstEntJson);
                idx++;
            }
        }

        [TestMethod]
        [TestCategory(TestBase.AutomatedTestCategory)]
        public void Reset()
        {
            string filePrefix = "filePrefix";
            string filesPath = "TestData";

            systemUnderTest = new DataFileStoreReaderCsv(MockLogger.Object, filePrefix, filesPath, crmSchemaConfiguration);

            FluentActions.Invoking(() => systemUnderTest.Reset())
                         .Should()
                         .NotThrow();
        }

        private static void AssertMapAttribute(Entity firstEnt, Entity firstEntJson, string attrName)
        {
            Assert.IsNotNull(firstEnt.Attributes[attrName]);
            Assert.IsNotNull(firstEntJson.Attributes[attrName]);

            var attrMap = firstEnt.GetAttributeValue<AliasedValue>(attrName);
            var attrMap2 = firstEntJson.GetAttributeValue<AliasedValue>(attrName);

            Assert.AreEqual(attrMap.AttributeLogicalName, attrMap2.AttributeLogicalName);
            Assert.AreEqual(attrMap.EntityLogicalName, attrMap2.EntityLogicalName);

            if (attrMap.Value is string)
            {
                Assert.AreEqual(attrMap.Value, attrMap2.Value);
            }
            else
            {
                var attrMapEnt = (EntityReference)attrMap.Value;
                var attrMapEnt2 = (EntityReference)attrMap2.Value;

                Assert.AreEqual(attrMapEnt.LogicalName, attrMapEnt2.LogicalName);
                Assert.AreEqual(attrMapEnt.Name, attrMapEnt2.Name);
            }
        }

        private void CompareAttributes(Entity firstEnt, Entity firstEntJson, bool ignoreCount = false)
        {
            Assert.IsNotNull(firstEnt);
            Assert.IsNotNull(firstEntJson);
            if (!ignoreCount)
            {
                Assert.AreEqual(firstEnt.Attributes.Count, firstEntJson.Attributes.Count);
            }

            int idx = 0;

            while (idx < firstEnt.Attributes.Count)
            {
                var attr1 = firstEnt.Attributes.ToList()[idx];

                if (attr1.Value is string)
                {
                    Assert.AreEqual(firstEnt.GetAttributeValue<string>(attr1.Key), firstEntJson.GetAttributeValue<string>(attr1.Key));
                }
                else if (attr1.Value is AliasedValue)
                {
                    AssertMapAttribute(firstEnt, firstEntJson, attr1.Key);
                }

                idx++;
            }
        }

        private Tuple<List<Entity>, List<Entity>> GetFirstEntities(string filePrefix)
        {
            string extractedPath = Path.Combine(TestBase.GetWorkiongFolderPath(), "TestData");
            string extractFolder = Path.Combine(extractedPath, "ExtractedData");
            string schemaFilePath = Path.Combine(extractedPath, "usersettingsschema.xml");

            CrmSchemaConfiguration schemaConfig = CrmSchemaConfiguration.ReadFromFile(schemaFilePath);

            DataFileStoreReaderCsv store = new DataFileStoreReaderCsv(new ConsoleLogger(), filePrefix, extractFolder, schemaConfig);

            var batch = store.ReadBatchDataFromStore();
            List<Entity> firstEnt = batch.Select(p => p.OriginalEntity).ToList();

            DataFileStoreReader storeJson = new DataFileStoreReader(new ConsoleLogger(), filePrefix, extractFolder);

            var batchJson = storeJson.ReadBatchDataFromStore();
            List<Entity> firstEntJson = batchJson.Select(p => p.OriginalEntity).ToList();

            return new Tuple<List<Entity>, List<Entity>>(firstEnt, firstEntJson);
        }
    }
}