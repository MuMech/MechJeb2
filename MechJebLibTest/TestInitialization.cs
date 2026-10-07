/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("MechJebLibTest.TestInitialization", "MechJebLibTest")]

namespace MechJebLibTest
{
    /// <summary>
    ///     Test framework that routes MechJebLib's Logger to the output of the running test, so test classes
    ///     don't need to inject an ITestOutputHelper and register it themselves.  Everything other than
    ///     LoggingTestRunner is plumbing which recreates xunit's default runner chain down to the test runner.
    /// </summary>
    public class TestInitialization : XunitTestFramework
    {
        public TestInitialization(IMessageSink messageSink) : base(messageSink)
        {
            // use per-thread object pools instead of global object pools, in order to isolate them per-test.
            ObjectPoolBase.UseGlobal = false;
        }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName) =>
            new LoggingExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
    }

    internal class LoggingExecutor : XunitTestFrameworkExecutor
    {
        public LoggingExecutor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider,
            IMessageSink diagnosticMessageSink)
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink) { }

        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions)
        {
            using var assemblyRunner = new LoggingAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink,
                executionOptions);
            await assemblyRunner.RunAsync();
        }
    }

    internal class LoggingAssemblyRunner : XunitTestAssemblyRunner
    {
        public LoggingAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink,
            IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions) { }

        protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection,
            IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource) =>
            new LoggingCollectionRunner(testCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer,
                new ExceptionAggregator(Aggregator), cancellationTokenSource).RunAsync();
    }

    internal class LoggingCollectionRunner : XunitTestCollectionRunner
    {
        public LoggingCollectionRunner(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource) { }

        protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class,
            IEnumerable<IXunitTestCase> testCases) =>
            new LoggingClassRunner(testClass, @class, testCases, DiagnosticMessageSink, MessageBus, TestCaseOrderer,
                new ExceptionAggregator(Aggregator), CancellationTokenSource, CollectionFixtureMappings).RunAsync();
    }

    internal class LoggingClassRunner : XunitTestClassRunner
    {
        public LoggingClassRunner(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource, IDictionary<Type, object> collectionFixtureMappings)
            : base(testClass, @class, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource,
                collectionFixtureMappings) { }

        protected override Task<RunSummary> RunTestMethodAsync(ITestMethod testMethod, IReflectionMethodInfo method,
            IEnumerable<IXunitTestCase> testCases, object[] constructorArguments) =>
            new LoggingMethodRunner(testMethod, Class, method, testCases, DiagnosticMessageSink, MessageBus,
                new ExceptionAggregator(Aggregator), CancellationTokenSource, constructorArguments).RunAsync();
    }

    internal class LoggingMethodRunner : XunitTestMethodRunner
    {
        private readonly IMessageSink _diagnosticMessageSink;
        private readonly object[]     _constructorArguments;

        public LoggingMethodRunner(ITestMethod testMethod, IReflectionTypeInfo @class, IReflectionMethodInfo method,
            IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource, object[] constructorArguments)
            : base(testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource,
                constructorArguments)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
            _constructorArguments  = constructorArguments;
        }

        // this mirrors XunitTestCase.RunAsync and XunitTheoryTestCase.RunAsync, other test case types (skipped data rows,
        // discovery errors) override RunAsync to do something else and don't run any test code, so leave those alone.
        protected override Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase)
        {
            if (testCase.GetType() == typeof(XunitTestCase))
                return new LoggingTestCaseRunner(testCase, testCase.DisplayName, testCase.SkipReason, _constructorArguments,
                    testCase.TestMethodArguments, MessageBus, new ExceptionAggregator(Aggregator), CancellationTokenSource).RunAsync();

            if (testCase.GetType() == typeof(XunitTheoryTestCase))
                return new LoggingTheoryTestCaseRunner(testCase, testCase.DisplayName, testCase.SkipReason, _constructorArguments,
                    _diagnosticMessageSink, MessageBus, new ExceptionAggregator(Aggregator), CancellationTokenSource).RunAsync();

            return base.RunTestCaseAsync(testCase);
        }
    }

    internal class LoggingTestCaseRunner : XunitTestCaseRunner
    {
        public LoggingTestCaseRunner(IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments,
            object[] testMethodArguments, IMessageBus messageBus, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator,
                cancellationTokenSource) { }

        protected override XunitTestRunner CreateTestRunner(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments,
            MethodInfo testMethod, object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) =>
            new LoggingTestRunner(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason,
                beforeAfterAttributes, new ExceptionAggregator(aggregator), cancellationTokenSource);
    }

    internal class LoggingTheoryTestCaseRunner : XunitTheoryTestCaseRunner
    {
        public LoggingTheoryTestCaseRunner(IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments,
            IMessageSink diagnosticMessageSink, IMessageBus messageBus, ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(testCase, displayName, skipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator,
                cancellationTokenSource) { }

        protected override XunitTestRunner CreateTestRunner(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments,
            MethodInfo testMethod, object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) =>
            new LoggingTestRunner(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason,
                beforeAfterAttributes, new ExceptionAggregator(aggregator), cancellationTokenSource);
    }

    /// <summary>
    ///     Gives every test a TestOutputHelper (xunit only creates one when the test class constructor asks for an
    ///     ITestOutputHelper) and registers it as the Logger for the duration of the test.
    /// </summary>
    internal class LoggingTestRunner : XunitTestRunner
    {
        private TestOutputHelper? _output;

        public LoggingTestRunner(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod,
            object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes,
                aggregator, cancellationTokenSource) { }

        protected override async Task<Tuple<decimal, string>> InvokeTestAsync(ExceptionAggregator aggregator)
        {
            // if the test class does take an ITestOutputHelper then xunit swaps a fresh one into the constructor arguments
            // (in place of a Func<TestOutputHelper> placeholder) for each test, which InvokeTestMethodAsync then reuses.
            if (ConstructorArguments.Any(o => o is TestOutputHelper || o is Func<TestOutputHelper>))
                return await base.InvokeTestAsync(aggregator);

            _output = new TestOutputHelper();
            _output.Initialize(MessageBus, Test);
            try
            {
                decimal executionTime = await InvokeTestMethodAsync(aggregator);
                return Tuple.Create(executionTime, _output.Output);
            }
            finally
            {
                _output.Uninitialize();
            }
        }

        protected override async Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            TestOutputHelper output = _output ?? ConstructorArguments.OfType<TestOutputHelper>().First();

            // the Logger override is per-thread, synchronous tests run on this thread from here through the test method.
            Logger.Register(o => output.WriteLine((string)o));
            try
            {
                return await base.InvokeTestMethodAsync(aggregator);
            }
            finally
            {
                Logger.Unregister();
            }
        }
    }
}
