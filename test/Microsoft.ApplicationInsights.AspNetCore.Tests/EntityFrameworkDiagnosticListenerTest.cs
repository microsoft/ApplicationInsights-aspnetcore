using System;
using System.Data;
using System.Diagnostics;
using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners;
using Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners.Implementation;
using Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers;
using Microsoft.ApplicationInsights.DataContracts;
using Xunit;
using IDbCommand = System.Data.IDbCommand;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests
{
    public class EntityFrameworkDiagnosticListenerTest
    {
        private const string TestListenerName = "TestListener";

        private DependencyTelemetry sentTelemetry;

        private DiagnosticListener telemetryListener;

        public EntityFrameworkDiagnosticListenerTest()
        {
            var listener = new EntityFrameworkDiagnosticListener(CommonMocks.MockTelemetryClient(telemetry => this.sentTelemetry = (DependencyTelemetry)telemetry));
            telemetryListener = new DiagnosticListener(TestListenerName);
            telemetryListener.SubscribeWithAdapter(listener);
        }

        [Fact]
        public void FillsTelemetryProperties()
        {
            var datetime = DateTimeOffset.Now;
            var timestamp = Stopwatch.GetTimestamp();
            telemetryListener.Write("Microsoft.EntityFrameworkCore.BeforeExecuteCommand",
                new { timestamp });
            telemetryListener.Write("Microsoft.EntityFrameworkCore.AfterExecuteCommand",
                new
                {
                    timestamp = timestamp + 100000,
                    command = new MockDbCommand()
                    {
                        Connection = new MockDbConnection()
                        {
                            Database = "Database name"
                        },
                        CommandText = "Command text"
                    }
                });

            Assert.Equal("Database name", sentTelemetry.Name);
            Assert.Equal("Command text", sentTelemetry.Data);
            Assert.Equal("SQL", sentTelemetry.Type);
            Assert.True(datetime < sentTelemetry.Timestamp);
            Assert.Equal(new TimeSpan(100000), sentTelemetry.Duration);
        }

        public class MockDbCommand : System.Data.IDbCommand
        {
            public string CommandText { get; set; }

            public int CommandTimeout { get; set; }

            public CommandType CommandType { get; set; }

            public System.Data.IDbConnection Connection { get; set; }

            public IDataParameterCollection Parameters { get; set; }

            public IDbTransaction Transaction { get; set; }

            public UpdateRowSource UpdatedRowSource { get; set; }

            public void Cancel()
            {
                throw new NotImplementedException();
            }

            public IDbDataParameter CreateParameter()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public int ExecuteNonQuery()
            {
                throw new NotImplementedException();
            }

            public IDataReader ExecuteReader()
            {
                throw new NotImplementedException();
            }

            public IDataReader ExecuteReader(CommandBehavior behavior)
            {
                throw new NotImplementedException();
            }

            public object ExecuteScalar()
            {
                throw new NotImplementedException();
            }

            public void Prepare()
            {
                throw new NotImplementedException();
            }
        }

        public class MockDbConnection : System.Data.IDbConnection
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public IDbTransaction BeginTransaction()
            {
                throw new NotImplementedException();
            }

            public IDbTransaction BeginTransaction(IsolationLevel il)
            {
                throw new NotImplementedException();
            }

            public void Close()
            {
                throw new NotImplementedException();
            }

            public IDbCommand CreateCommand()
            {
                throw new NotImplementedException();
            }

            public void Open()
            {
                throw new NotImplementedException();
            }

            public string ConnectionString { get; set; }

            public int ConnectionTimeout { get; set; }

            public string Database { get; set; }

            public ConnectionState State { get; set; }

            public void ChangeDatabase(string databaseName)
            {
                throw new NotImplementedException();
            }
        }
    }
}
