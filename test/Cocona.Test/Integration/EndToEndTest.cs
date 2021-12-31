using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cocona.Application;
using Cocona.Command.BuiltIn;
using Cocona.CommandLine;
using Cocona.Filters;
using Cocona.ShellCompletion.Candidate;
using FluentAssertions;
using Xunit;

#if COCONA_LITE
using CoconaApp = Cocona.CoconaLiteApp;
#endif

namespace Cocona.Test.Integration
{
    [Collection("End to End")] // NOTE: Test cases use `Console` and does not run in parallel.
    public class EndToEndTest
    {
        public enum RunBuilderMode
        {
            CreateBuilder,
            CreateHostBuilder,
            Shortcut,
        }
        private (string StandardOut, string StandardError, int ExitCode) Run(string[] args, Action<string[]> action)
        {
            var stdOutWriter = new StringWriter();
            var stdErrWriter = new StringWriter();

            Console.SetOut(stdOutWriter);
            Console.SetError(stdErrWriter);

            action(args);

            return (stdOutWriter.ToString(), stdErrWriter.ToString(), Environment.ExitCode);
        }

        private async Task<(string StandardOut, string StandardError, int ExitCode)> RunAsync(string[] args, Func<string[], Task> action)
        {
            var stdOutWriter = new StringWriter();
            var stdErrWriter = new StringWriter();

            Console.SetOut(stdOutWriter);
            Console.SetError(stdErrWriter);

            await action(args);

            return (stdOutWriter.ToString(), stdErrWriter.ToString(), Environment.ExitCode);
        }

        private (string StandardOut, string StandardError, int ExitCode) Run<T>(RunBuilderMode mode, string[] args)
        {
            var stdOutWriter = new StringWriter();
            var stdErrWriter = new StringWriter();

            Console.SetOut(stdOutWriter);
            Console.SetError(stdErrWriter);

            switch (mode)
            {
                case RunBuilderMode.CreateBuilder:
                    var builder = CoconaApp.CreateBuilder(args);
                    var app = builder.Build();
                    app.AddCommands<T>();

                    app.Run();
                    break;
                case RunBuilderMode.CreateHostBuilder:
                    CoconaApp.CreateHostBuilder()
                        .Run<T>(args);
                    break;
                case RunBuilderMode.Shortcut:
                    CoconaApp.Run<T>(args);
                    break;
            }

            return (stdOutWriter.ToString(), stdErrWriter.ToString(), Environment.ExitCode);
        }

        private async Task<(string StandardOut, string StandardError, int ExitCode)> RunAsync<T>(RunBuilderMode mode, string[] args, CancellationToken cancellationToken)
        {
            var stdOutWriter = new StringWriter();
            var stdErrWriter = new StringWriter();

            Console.SetOut(stdOutWriter);
            Console.SetError(stdErrWriter);

            switch (mode)
            {
                case RunBuilderMode.CreateBuilder:
                    var builder = CoconaApp.CreateBuilder(args);
                    var app = builder.Build();
                    app.AddCommands<T>();

                    await app.RunAsync(cancellationToken);
                    break;
                case RunBuilderMode.CreateHostBuilder:
                    await CoconaApp.CreateHostBuilder()
                        .RunAsync<T>(args, cancellationToken: cancellationToken);
                    break;
                case RunBuilderMode.Shortcut:
                    await CoconaApp.RunAsync<T>(args, cancellationToken: cancellationToken);
                    break;
            }

            return (stdOutWriter.ToString(), stdErrWriter.ToString(), Environment.ExitCode);
        }

        private (string StandardOut, string StandardError, int ExitCode) Run(RunBuilderMode mode, string[] args, Type[] types)
        {
            var stdOutWriter = new StringWriter();
            var stdErrWriter = new StringWriter();

            Console.SetOut(stdOutWriter);
            Console.SetError(stdErrWriter);

            switch (mode)
            {
                case RunBuilderMode.CreateBuilder:
                    var builder = CoconaApp.CreateBuilder(args);
                    var app = builder.Build();
                    foreach (var type in types)
                    {
                        app.AddCommands(type);
                    }

                    app.Run();
                    break;
                case RunBuilderMode.CreateHostBuilder:
                    CoconaApp.CreateHostBuilder()
                        .Run(args, types);
                    break;
                case RunBuilderMode.Shortcut:
                    CoconaApp.Run(args, types);
                    break;
            }

            return (stdOutWriter.ToString(), stdErrWriter.ToString(), Environment.ExitCode);
        }

        private async Task<(string StandardOut, string StandardError, int ExitCode)> RunAsync<T>(RunBuilderMode mode, string[] args, Type[] types, CancellationToken cancellationToken)
        {
            var stdOutWriter = new StringWriter();
            var stdErrWriter = new StringWriter();

            Console.SetOut(stdOutWriter);
            Console.SetError(stdErrWriter);

            switch (mode)
            {
                case RunBuilderMode.CreateBuilder:
                    var builder = CoconaApp.CreateBuilder(args);
                    var app = builder.Build();
                    foreach (var type in types)
                    {
                        app.AddCommands(type);
                    }

                    await app.RunAsync(cancellationToken);
                    break;
                case RunBuilderMode.CreateHostBuilder:
                    await CoconaApp.CreateHostBuilder()
                        .RunAsync(args, types, cancellationToken: cancellationToken);
                    break;
                case RunBuilderMode.Shortcut:
                    await CoconaApp.RunAsync(args, types, cancellationToken: cancellationToken);
                    break;
            }

            return (stdOutWriter.ToString(), stdErrWriter.ToString(), Environment.ExitCode);
        }

        private (string StandardOut, string StandardError, int ExitCode) Run(string[] args, Delegate[] delegates)
        {
            var stdOutWriter = new StringWriter();
            var stdErrWriter = new StringWriter();

            Console.SetOut(stdOutWriter);
            Console.SetError(stdErrWriter);

            var app = CoconaApp.Create(args);
            foreach (var @delegate in delegates)
            {
                app.AddCommand(@delegate);
            }
            app.Run();

            return (stdOutWriter.ToString(), stdErrWriter.ToString(), Environment.ExitCode);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Single(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Single>(mode, new string[] { });

            stdOut.Should().Be("Hello Konnichiwa!" + Environment.NewLine);
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Single_Help(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Single>(mode, new string[] { "--help" });

            stdOut.Should().Contain("Usage:");
            exitCode.Should().Be(129);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Single_Version(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Single>(mode, new string[] { "--version" });

            stdOut.Should().MatchRegex(@"[^ ]+ \d+\.\d+\.\d+");
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Single_Completion(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Single>(mode, new string[] { "--completion", "zsh" });

            stdOut.Should().Contain("#compdef");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Single_CompletionCandidates(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Single_Candidates>(mode, new string[] { "--completion-candidates", "bash:name", "--", "A" });

            stdOut.Should().Contain("Alice");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        class TestCommand_Single
        {
            public void Hello()
            {
                Console.WriteLine("Hello Konnichiwa!");
            }
        }


        class TestCommand_Single_Candidates
        {
            public void Hello([CompletionCandidates(typeof(OnTheFlyCandidatesProvider))]string name)
            {
                Console.WriteLine("Hello Konnichiwa!");
            }

            public class OnTheFlyCandidatesProvider : ICoconaCompletionOnTheFlyCandidatesProvider
            {
                public IReadOnlyList<CompletionCandidateValue> GetCandidates(CoconaCompletionCandidatesMetadata metadata, ParsedCommandLine parsedCommandLine)
                {
                    return new[] {new CompletionCandidateValue("Alice", ""),};
                }
            }
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_Command(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "konnichiwa" });

            stdOut.Should().Be("Konnichiwa!" + Environment.NewLine);
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_Index(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { });

            stdOut.Should().Contain("Usage:");
            stdOut.Should().Contain("Commands:");
            stdOut.Should().Contain("  konnichiwa");
            stdOut.Should().Contain("  hello");
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_Help(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "--help" });

            stdOut.Should().Contain("Usage:");
            stdOut.Should().Contain("Commands:");
            stdOut.Should().Contain("  konnichiwa");
            stdOut.Should().Contain("  hello");
            exitCode.Should().Be(129);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_Help_Command(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "konnichiwa", "--help" });

            stdOut.Should().Contain("Usage:");
            stdOut.Should().Contain(" konnichiwa [--help]");
            exitCode.Should().Be(129);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_Help_Command_ShortOptionOverwrite(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "help-short-option-overwrite", "--help" });

            stdOut.Should().Contain("Usage:");
            stdOut.Should().Contain(" help-short-option-overwrite [--host] [--help]");
            exitCode.Should().Be(129);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_Version(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "--version" });

            stdOut.Should().MatchRegex(@"[^ ]+ \d+\.\d+\.\d+");
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_ExitCode(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "exit-code" });

            stdOut.Should().Contain("ExitCode=128");
            exitCode.Should().Be(128);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_CommandMissing(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] {"axit-mode"});

            stdOut.Should().BeEmpty();
            stdErr.Should().Contain("Similar");
            stdErr.Should().Contain("exit-code");
            exitCode.Should().Be(1);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public async Task CoconaApp_Run_Multiple_Task(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = (await RunAsync<TestCommand_Multiple>(mode, new string[] { "long-running" }, new CancellationTokenSource(1000).Token));

            stdOut.Should().Contain("Begin");
            stdOut.Should().Contain("Canceled");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(127);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_MultipleClass(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run(mode, new string[] { }, new [] { typeof(TestCommand_Multiple), typeof(TestCommand2) });

            stdOut.Should().Contain("exit-code");
            stdOut.Should().Contain("foo-bar");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_ArgTest_1(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "arg-test", "Alice" });

            stdOut.Should().Contain("Hello Alice (17)!");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_ArgTest_2(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "arg-test", "Karen", "18" });

            stdOut.Should().Contain("Hello Karen (18)!");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_OptionTest_1(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "option-test", "--name", "Alice" });

            stdOut.Should().Contain("Hello Alice (17)!");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_OptionTest_2(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "option-test", "--name", "Karen", "-a", "18" });

            stdOut.Should().Contain("Hello Karen (18)!");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_Completion(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple>(mode, new string[] { "--completion", "zsh" });

            stdOut.Should().Contain("#compdef");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_CompletionCandidates(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple_Candidates>(mode, new string[] { "--completion-candidates", "bash:name", "--", "hello", "A" });

            stdOut.Should().Contain("Karen");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_CompletionCandidates_UnknownCommand(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple_Candidates>(mode, new string[] { "--completion-candidates", "bash:name", "--", "unknown-command", "A" });

            stdOut.Should().BeEmpty();
            stdErr.Should().NotBeEmpty();
            exitCode.Should().Be(1);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Multiple_CompletionCandidates_UnknownOption(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Multiple_Candidates>(mode, new string[] { "--completion-candidates", "bash:unknown-option", "--", "hello", "A" });

            stdOut.Should().BeEmpty();
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        class TestCommand_Multiple
        {
            public void Hello()
            {
                Console.WriteLine("Hello!");
            }

            public void Konnichiwa()
            {
                Console.WriteLine("Konnichiwa!");
            }

            public int ExitCode()
            {
                Console.WriteLine("ExitCode=128");
                return 128;
            }

            public async Task<int> LongRunning([FromService]ICoconaAppContextAccessor context)
            {
                Console.WriteLine("Begin");
                while (!context.Current.CancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1);
                }
                Console.WriteLine("Canceled");

                return 127;
            }

            public void ArgTest([Argument] string name, [Argument]int age = 17)
            {
                Console.WriteLine($"Hello {name} ({age})!");
            }

            public void OptionTest([Option] string name, [Option('a')]int age = 17)
            {
                Console.WriteLine($"Hello {name} ({age})!");
            }

            public void HelpShortOptionOverwrite([Option('h')] bool host)
            {
                Console.WriteLine($"Host={host}");
            }
        }

        class TestCommand_Multiple_Candidates
        {
            public void Hello([CompletionCandidates(typeof(OnTheFlyCandidatesProvider))]string name)
            {
                Console.WriteLine("Hello Konnichiwa!");
            }

            public void Konnichiwa()
            {
                Console.WriteLine("Konnichiwa!");
            }

            public class OnTheFlyCandidatesProvider : ICoconaCompletionOnTheFlyCandidatesProvider
            {
                public IReadOnlyList<CompletionCandidateValue> GetCandidates(CoconaCompletionCandidatesMetadata metadata, ParsedCommandLine parsedCommandLine)
                {
                    return new[] { new CompletionCandidateValue("Karen", ""), };
                }
            }
        }

        class TestCommand2
        {
            public void FooBar() { }
        }

        public enum TestValues
        {
            Alice,
            Karen
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Nested(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Nested>(mode, new string[] { "nested", "hello", "Karen" });

            stdOut.Should().Contain("Hello Karen");
            stdErr.Should().BeEmpty();
            exitCode.Should().Be(0);
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Nested_CommandHelp(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Nested>(mode, new string[] { "nested", "hello", "--help" });

            stdOut.Should().Contain("Usage:");
            stdOut.Should().Contain(" nested hello [--help] arg0");
            stdOut.Should().Contain("Arguments:");
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Nested_Index_0(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Nested>(mode, new string[] { });

            stdOut.Should().Contain("Usage:");
            stdOut.Should().Contain("Commands:");
            stdOut.Should().Contain("  konnichiwa");
            stdOut.Should().Contain("  nested");
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Nested_Index_1(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Nested>(mode, new string[] { "nested" });

            stdOut.Should().Contain("Usage:");
            stdOut.Should().Contain("Commands:");
            stdOut.Should().Contain("  hello");
            stdOut.Should().Contain("  bye");
        }

        [HasSubCommands(typeof(Nested))]
        class TestCommand_Nested
        {
            public void Konnichiwa()
            {
                Console.WriteLine("Konnichiwa");
            }

            class Nested
            {
                public void Hello([Argument] string arg0)
                {
                    Console.WriteLine($"Hello {arg0}");
                }
                public void Bye([Argument] string arg0)
                {
                    Console.WriteLine($"Bye {arg0}");
                }
            }
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_CommandMethodForwarding_Multiple(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_CommandMethodForwarding_Multiple>(mode, new string[] { "forward", "--option0", "OptionValue0", "ArgumentValue0" });

            stdErr.Should().BeNullOrEmpty();
            stdOut.Should().Contain("Forwarded:OptionValue0:ArgumentValue0");
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_CommandMethodForwarding_Multiple_BuiltInShowHelp(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_CommandMethodForwarding_Multiple>(mode, new string[] { "my-help" });

            stdErr.Should().BeNullOrEmpty();
            stdOut.Should().Contain("Usage:");
            stdOut.Should().Contain("Commands:");
        }

        class TestCommand_CommandMethodForwarding_Multiple
        {
            public void Hello() { }

            [CommandMethodForwardedTo(typeof(TestCommand_CommandMethodForwarding_Multiple), nameof(TestCommand_CommandMethodForwarding_Multiple.ForwardTarget))]
            public void Forward() { }
            public void ForwardTarget(string option0, [Argument]string arg0) { Console.WriteLine($"Forwarded:{option0}:{arg0}"); }

            [CommandMethodForwardedTo(typeof(BuiltInOptionLikeCommands), nameof(BuiltInOptionLikeCommands.ShowHelp))]
            public void MyHelp() { }
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_Throw(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Throw>(mode, new string[] { "my-help" });

            stdErr.Should().Contain("Unhandled Exception:");
            stdErr.Should().Contain("ThrowCore()");
        }

        class TestCommand_Throw
        {
            public void Throw()
            {
                ThrowCore();
            }

            private void ThrowCore()
            {
                throw new Exception("Exception!");
            }
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_ParameterSet(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_ParameterSet>(mode, new string[] { "--option1", "--option-required=alice", "argValue0", "argValue1" });
            exitCode.Should().Be(0);
            stdOut.Should().Contain("False;argValue0;True;argValue1");

            (stdOut, stdErr, exitCode) = Run<TestCommand_ParameterSet>(mode, new string[] { "--help" });
            stdOut.Should().Contain("0: arg0");
            stdOut.Should().Contain("1: arg1");
            stdOut.Should().Contain("--option0");
            stdOut.Should().Contain("--option1");
            stdOut.Should().MatchRegex(@"--option-has-default <String>\s+\(Default: hello\)");
            stdOut.Should().MatchRegex(@"--option-required <String>\s+\(Required\)");
        }

        class TestCommand_ParameterSet
        {
            public class ParameterSet : ICommandParameterSet
            {
                public bool Option1 { get; set; }

                [HasDefaultValue]
                public string OptionHasDefault { get; set; } = "hello";
                public string OptionRequired { get; set; } = "ignored";

                [Argument]
                public string Arg0 { get; set; }
            }

            public void Run(bool option0, ParameterSet paramSet, [Argument]string arg1)
            {
                Console.WriteLine($"{option0};{paramSet.Arg0};{paramSet.Option1};{arg1}");
            }
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_ParameterSet_Record(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_ParameterSet_Record>(mode, new string[] { "--option1", "--option-required=alice", "argValue0", "argValue1" });
            exitCode.Should().Be(0);
            stdOut.Should().Contain("False;argValue0;True;argValue1");

            (stdOut, stdErr, exitCode) = Run<TestCommand_ParameterSet_Record>(mode, new string[] { "--help" });
            stdOut.Should().Contain("0: arg0");
            stdOut.Should().Contain("1: arg1");
            stdOut.Should().Contain("--option0");
            stdOut.Should().Contain("--option1");
            stdOut.Should().MatchRegex(@"--option-required <String>\s+\(Required\)");
            stdOut.Should().MatchRegex(@"--option-has-default <String>\s+\(Default: hello\)");
        }

        class TestCommand_ParameterSet_Record
        {
            public record ParameterSet(bool Option1, string OptionRequired, [Argument] string Arg0, string OptionHasDefault = "hello") : ICommandParameterSet;

            public void Run(bool option0, ParameterSet paramSet, [Argument] string arg1)
            {
                Console.WriteLine($"{option0};{paramSet.Arg0};{paramSet.Option1};{arg1}");
            }
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        [InlineData(RunBuilderMode.Shortcut)]
        public void CoconaApp_Run_ParameterSet_Record_MultipleCommand(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_ParameterSet_Record_MultipleCommand>(mode, new string[] { "command-a", "--option1", "--option-required=alice", "argValue0", "argValue1" });
            exitCode.Should().Be(0);
            stdOut.Should().Contain("A:False;argValue0;True;argValue1");

            (stdOut, stdErr, exitCode) = Run<TestCommand_ParameterSet_Record_MultipleCommand>(mode, new string[] { "--help" });
            stdOut.Should().Contain("command-a");
            stdOut.Should().Contain("command-b");

            (stdOut, stdErr, exitCode) = Run<TestCommand_ParameterSet_Record_MultipleCommand>(mode, new string[] { "command-a", "--help" });
            stdOut.Should().Contain("0: arg0");
            stdOut.Should().Contain("1: arg1");
            stdOut.Should().Contain("--option0");
            stdOut.Should().Contain("--option1");
            stdOut.Should().MatchRegex(@"--option-required <String>\s+\(Required\)");
            stdOut.Should().MatchRegex(@"--option-has-default <String>\s+\(Default: hello\)");
        }

        class TestCommand_ParameterSet_Record_MultipleCommand
        {
            public record ParameterSet(bool Option1, string OptionRequired, [Argument] string Arg0, string OptionHasDefault = "hello") : ICommandParameterSet;

            public void CommandA(bool option0, ParameterSet paramSet, [Argument] string arg1)
            {
                Console.WriteLine($"A:{option0};{paramSet.Arg0};{paramSet.Option1};{arg1}");
            }

            public void CommandB(bool option0, ParameterSet paramSet, [Argument] string arg1)
            {
                Console.WriteLine($"B:{option0};{paramSet.Arg0};{paramSet.Option1};{arg1}");
            }
        }

        [Fact]
        public void CoconaApp_Run_AddCommand_Delegate_Static_Single()
        {
            var (stdOut, stdErr, exitCode) = Run(Array.Empty<string>(), new[] { new Action(TestCommand_Delegate.StaticCommandA) });
            stdOut.Should().Contain("StaticCommandA");
        }

        [Fact]
        public void CoconaApp_Run_AddCommand_Delegate_Static_Multiple()
        {
            var (stdOut, stdErr, exitCode) = Run(new [] { "static-command-b" }, new[] { new Action(TestCommand_Delegate.StaticCommandA), new Action(TestCommand_Delegate.StaticCommandB) });
            stdOut.Should().Contain("StaticCommandB");
        }

        [Fact]
        public void CoconaApp_Run_AddCommand_Delegate_Instance_Single()
        {
            var command = new TestCommand_Delegate();
            var (stdOut, stdErr, exitCode) = Run(Array.Empty<string>(), new[] { new Action(command.InstanceCommandA) });
            stdOut.Should().Contain($"InstanceCommandA:{command.Id}");
        }

        [Fact]
        public void CoconaApp_Run_AddCommand_Delegate_Instance_Multiple()
        {
            var command = new TestCommand_Delegate();
            var (stdOut, stdErr, exitCode) = Run(new[] { "instance-command-b" }, new[] { new Action(command.InstanceCommandA), new Action(command.InstanceCommandB) });
            stdOut.Should().Contain($"InstanceCommandB:{command.Id}");
        }

        class TestCommand_Delegate
        {
            public Guid Id { get; } = Guid.NewGuid();

            public static void StaticCommandA()
                => Console.WriteLine($"{nameof(StaticCommandA)}");
            public static void StaticCommandB()
                => Console.WriteLine($"{nameof(StaticCommandB)}");

            public void InstanceCommandA()
                => Console.WriteLine($"{nameof(InstanceCommandA)}:{Id}");
            public void InstanceCommandB()
                => Console.WriteLine($"{nameof(InstanceCommandB)}:{Id}");
        }


        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        public void CoconaApp_Run_Static_Single(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Static_Single>(mode, new[] { "--value", "123" });
            stdOut.Should().Contain($"A:123");
        }

        class TestCommand_Static_Single
        {
            [Command]
            public static void A(int value) => Console.WriteLine($"A:{value}");
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        public void CoconaApp_Run_Static_Multiple(RunBuilderMode mode)
        {
            var command = new TestCommand_Delegate();
            var (stdOut, stdErr, exitCode) = Run<TestCommand_Static_Multiple>(mode, new[] { "b", "--value", "123" });
            stdOut.Should().Contain($"B:123");
        }

        class TestCommand_Static_Multiple
        {
            [Command]
            public static void A(int value) => Console.WriteLine($"A:{value}");
            [Command]
            public static void B(int value) => Console.WriteLine($"B:{value}");
        }

        [Theory]
        [InlineData(RunBuilderMode.CreateHostBuilder)]
        [InlineData(RunBuilderMode.CreateBuilder)]
        public void CoconaApp_Run_StopParsingOption(RunBuilderMode mode)
        {
            var (stdOut, stdErr, exitCode) = Run<TestCommand_StopParsingOption>(mode, new[] { "--a", "123", "--b", "valueB", "A", "B", "C", "D" });
            stdOut.Should().Contain($"A:123:valueB:A:B,C,D");
        }

        class TestCommand_StopParsingOption
        {
            public void A([Option]int a, [Option(StopParsingOptions = true)]string b, [Argument]string arg0, [Argument]string[] args)
                => Console.WriteLine($"A:{a}:{b}:{arg0}:{string.Join(",", args)}");
        }

        [Fact]
        public void ParameterInjection_CoconaApp_CreateBuilder_Delegate()
        {
            var (stdOut, stdErr, exitCode) = Run(new[] { "--age", "18" }, args =>
            {
                var builder = CoconaApp.CreateBuilder(args);
#if COCONA_LITE
                builder.Services.Add(new Lite.ServiceDescriptor(typeof(IMyService), (_, _) => new MyService(), singleton: true));
#else
                builder.Services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(IMyService), new MyService()));
#endif
                var app = builder.Build();
                app.AddCommand((int age, IMyService myService) => Console.WriteLine($"Hello {myService.GetName()} ({age})!"));
                app.Run();
            });

            stdOut.Should().Be("Hello Alice (18)!" + Environment.NewLine);
            exitCode.Should().Be(0);
        }

        [Fact]
        public void ParameterInjection_CoconaApp_CreateBuilder_Type()
        {
            {
                var (stdOut, stdErr, exitCode) = Run(new[] { "hello-without-from-service", "--age", "18" }, args =>
                {
                    var builder = CoconaApp.CreateBuilder(args);
#if COCONA_LITE
                    builder.Services.Add(new Lite.ServiceDescriptor(typeof(IMyService), (_, _) => new MyService(), singleton: true));
#else
                    builder.Services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(IMyService), new MyService()));
#endif
                    var app = builder.Build();
                    app.AddCommands<ParameterInjectionTestCommands>();
                    app.Run();
                });

                stdErr.Should().Contain("'--my-service' is required");
                exitCode.Should().Be(1);
            }

            {
                var (stdOut, stdErr, exitCode) = Run(new[] { "hello-with-from-service", "--age", "18" }, args =>
                {
                    var builder = CoconaApp.CreateBuilder(args);
#if COCONA_LITE
                    builder.Services.Add(new Lite.ServiceDescriptor(typeof(IMyService), (_, _) => new MyService(), singleton: true));
#else
                    builder.Services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(IMyService), new MyService()));
#endif
                    var app = builder.Build();
                    app.AddCommands<ParameterInjectionTestCommands>();
                    app.Run();
                });

                stdOut.Should().Be("Hello Alice (18)!" + Environment.NewLine);
                exitCode.Should().Be(0);
            }
        }
        class ParameterInjectionTestCommands
        {
            public void HelloWithoutFromService(int age, IMyService myService) => Console.WriteLine($"Hello {myService.GetName()} ({age})!");
            public void HelloWithFromService(int age, [FromService]IMyService myService) => Console.WriteLine($"Hello {myService.GetName()} ({age})!");
        }

        interface IMyService
        {
            string GetName();
        }
        class MyService : IMyService
        {
            public string GetName() => "Alice";
        }

        [Fact]
        public void CommandFilter_CommandType_Attributes()
        {
            var (stdOut, stdErr, exitCode) = Run(Array.Empty<string>(), args =>
            {
                var app = CoconaApp.Create(args);
                app.AddCommands<TestCommand_HasFilter>();
                app.Run();
            });

            stdOut.Should().Be(String.Join(Environment.NewLine, new[] { "Class#Begin", "Method#Begin", "Hello", "Method#End", "Class#End" }) + Environment.NewLine);
            exitCode.Should().Be(0);
        }

        [Fact]
        public void CommandFilter_CommandDelegate()
        {
            var (stdOut, stdErr, exitCode) = Run(Array.Empty<string>(), args =>
            {
                var app = CoconaApp.Create(args);
                app.AddCommand([TestCommand_HasFilter.MyCommandFilter("Method")]() => Console.WriteLine("Hello"))
                    .WithFilter(new TestCommand_HasFilter.MyCommandFilter("Builder"));
                app.Run();
            });

            stdOut.Should().Be(String.Join(Environment.NewLine, new[] { "Method#Begin", "Builder#Begin", "Hello", "Builder#End", "Method#End" }) + Environment.NewLine);
            exitCode.Should().Be(0);
        }

        [Fact]
        public void CommandFilter_CommandDelegate_Multiple()
        {
            var (stdOut, stdErr, exitCode) = Run(Array.Empty<string>(), args =>
            {
                // Filters are applied from the outer side
                // Class -> Builder2 -> Builder1 -> Method
                var app = CoconaApp.Create(args);
                app.AddCommand([TestCommand_HasFilter.MyCommandFilter("Method")]() => Console.WriteLine("Hello"))
                    .WithFilter(new TestCommand_HasFilter.MyCommandFilter("Builder1"))
                    .WithFilter(new TestCommand_HasFilter.MyCommandFilter("Builder2"));
                app.Run();
            });

            stdOut.Should().Be(String.Join(Environment.NewLine, new[] { "Method#Begin", "Builder1#Begin", "Builder2#Begin", "Hello", "Builder2#End", "Builder1#End", "Method#End" }) + Environment.NewLine);
            exitCode.Should().Be(0);
        }

        [Fact]
        public void CommandFilter_UseFilter_CommandDelegate()
        {
            var (stdOut, stdErr, exitCode) = Run(Array.Empty<string>(), args =>
            {
                // Filters are applied from the outer side
                // Builder0 (UseFilter) -> Method -> Builder1 -> Builder2
                var app = CoconaApp.Create(args);
                app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder0"));
                app.AddCommand([TestCommand_HasFilter.MyCommandFilter("Method")]() => Console.WriteLine("Hello")) // Same as .WithFilter()
                    .WithFilter(new TestCommand_HasFilter.MyCommandFilter("Builder1"))
                    .WithFilter(new TestCommand_HasFilter.MyCommandFilter("Builder2")); // Method, Builder1, Builder2
                app.Run();
            });

            stdOut.Should().Be(String.Join(Environment.NewLine, new[] { "Builder0#Begin", "Method#Begin", "Builder1#Begin", "Builder2#Begin", "Hello", "Builder2#End", "Builder1#End", "Method#End", "Builder0#End" }) + Environment.NewLine);
            exitCode.Should().Be(0);
        }

        [Fact]
        public void CommandFilter_UseFilter_CommandType()
        {
            var (stdOut, stdErr, exitCode) = Run(Array.Empty<string>(), args =>
            {
                // Filters are applied from the outer side
                // Builder0 (UseFilter) -> Class -> Method
                var app = CoconaApp.Create(args);
                app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder0"));
                app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder1"));
                app.AddCommands<TestCommand_HasFilter>();
                app.Run();
            });

            stdOut.Should().Be(String.Join(Environment.NewLine, new[] { "Builder0#Begin", "Builder1#Begin", "Class#Begin", "Method#Begin", "Hello", "Method#End", "Class#End", "Builder1#End", "Builder0#End" }) + Environment.NewLine);
            exitCode.Should().Be(0);
        }

        [Fact]
        public void CommandFilter_UseFilter_CommandDelegate_SubCommand()
        {
            var (stdOut, stdErr, exitCode) = Run(new [] { "sub-command", "command1" }, args =>
            {
                // Filters are applied from the outer side
                // Builder0 (UseFilter) -> Class -> Method
                var app = CoconaApp.Create(args);
                app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder0"));
                app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder1"));
                app.AddSubCommand("sub-command", x =>
                {
                    x.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder2"));
                    x.AddCommand("command1", () => Console.WriteLine("Hello"));
                    x.AddCommand("command2", () => Console.WriteLine("A"));
                });

                app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder3"));
                app.AddSubCommand("sub-command-2", x =>
                {
                    app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder4"));
                    x.AddCommand("command1", () => Console.WriteLine("B"));
                    x.AddCommand("command2", () => Console.WriteLine("C"));
                });
                app.Run();
            });

            stdOut.Should().Be(String.Join(Environment.NewLine, new[] { "Builder0#Begin", "Builder1#Begin", "Builder2#Begin", "Hello", "Builder2#End", "Builder1#End", "Builder0#End" }) + Environment.NewLine);
            exitCode.Should().Be(0);
        }

        [Fact]
        public void CommandFilter_UseFilter_CommandType_SubCommand()
        {
            var (stdOut, stdErr, exitCode) = Run(new [] { "sub-command", "hello" }, args =>
            {
                // Filters are applied from the outer side
                // Builder0 (UseFilter) -> Class -> Method
                var app = CoconaApp.Create(args);
                app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder0"));
                app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder1"));
                app.AddSubCommand("sub-command", x =>
                {
                    x.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder2"));
                    x.AddCommands<TestCommandMany_HasFilter>();
                });

                app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder3"));
                app.AddSubCommand("sub-command-2", x =>
                {
                    app.UseFilter(new TestCommand_HasFilter.MyCommandFilter("Builder4"));
                    x.AddCommand("command1", () => Console.WriteLine("A"));
                    x.AddCommand("command2", () => Console.WriteLine("B"));
                });
                app.Run();
            });

            stdOut.Should().Be(String.Join(Environment.NewLine, new[] { "Builder0#Begin", "Builder1#Begin", "Builder2#Begin", "Class#Begin", "Method#Begin", "Hello", "Method#End", "Class#End", "Builder2#End", "Builder1#End", "Builder0#End" }) + Environment.NewLine);
            exitCode.Should().Be(0);
        }

        [MyCommandFilter("Class")]
        class TestCommand_HasFilter
        {
            [MyCommandFilter("Method")]
            public void Hello() => Console.WriteLine("Hello");

            public class MyCommandFilter : CommandFilterAttribute
            {
                private string _label;
                public MyCommandFilter(string label)
                {
                    _label = label;
                }
                public override async ValueTask<int> OnCommandExecutionAsync(CoconaCommandExecutingContext ctx, CommandExecutionDelegate next)
                {
                    Console.WriteLine($"{_label}#Begin");
                    var result = await next(ctx);
                    Console.WriteLine($"{_label}#End");
                    return result;
                }
            }
        }

        [MyCommandFilter("Class")]
        class TestCommandMany_HasFilter
        {
            [MyCommandFilter("Method")]
            public void Hello() => Console.WriteLine("Hello");
            public void Konnichiwa() => Console.WriteLine("Konnichiwa");

            public class MyCommandFilter : CommandFilterAttribute
            {
                private string _label;
                public MyCommandFilter(string label)
                {
                    _label = label;
                }
                public override async ValueTask<int> OnCommandExecutionAsync(CoconaCommandExecutingContext ctx, CommandExecutionDelegate next)
                {
                    Console.WriteLine($"{_label}#Begin");
                    var result = await next(ctx);
                    Console.WriteLine($"{_label}#End");
                    return result;
                }
            }
        }
    }
}
