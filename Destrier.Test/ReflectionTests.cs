﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Destrier.Test
{
    public class ReflectionTests
    {
        public ReflectionTests()
        {
            DatabaseConfigurationContext.DefaultDatabaseName = "DestrierTest";
            DatabaseConfigurationContext.DefaultSchemaName = "dbo";
        }

        [Fact]
        public void TableName_Test()
        {
            var tableAttribute = ReflectionCache.GetTableAttribute(typeof(MockObject));
            Assert.NotNull(tableAttribute);

            var tableAttributeFromModel = Model.TableAttribute(typeof(MockObject));
            Assert.Equal(tableAttributeFromModel, tableAttribute);
            var tableName = Model.TableName(typeof(MockObject));

            var fullyQualifiedTableName = Model.TableNameFullyQualified(typeof(MockObject));

            Assert.NotNull(tableName);
            Assert.NotEmpty(tableName);

            Assert.Equal(tableName, "MockObjects");

            Assert.NotNull(fullyQualifiedTableName);
            Assert.NotEmpty(fullyQualifiedTableName);
            Assert.Equal(fullyQualifiedTableName, "DestrierTest.dbo.MockObjects");
        }

        [Fact]
        public void Members_Test()
        {
            var members = Model.MembersRecursive(typeof(MockObject));

            Assert.NotNull(members);
            Assert.NotEmpty(members);
            Assert.True(members.Any(m => m is ColumnMember));
            Assert.True(members.Any(m => m is ReferencedObjectMember));
            Assert.True(members.Any(m => m is ChildCollectionMember));
        }
    }
}
