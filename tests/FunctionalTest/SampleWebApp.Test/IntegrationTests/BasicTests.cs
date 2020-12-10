﻿using AiurEventSyncer.Models;
using AiurEventSyncer.Remotes;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SampleWebApp.Controllers;
using SampleWebApp.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWebApp.Tests.IntegrationTests
{
    [TestClass]
    public class BasicTests
    {
        private const int _port = 15151;
        private readonly string _endpointUrl = $"http://localhost:{_port}/repo.ares";
        private IHost _server;

        [TestInitialize]
        public async Task CreateServer()
        {
            _server = Program.BuildHost(null, _port);
            await _server.StartAsync();
        }

        [TestCleanup]
        public async Task CleanServer()
        {
            await _server.StopAsync();
            _server.Dispose();
            HomeController._repo = null;
        }

        [TestMethod]
        public async Task SingleCommit()
        {
            var repo = new Repository<LogItem>();
            var remote = new WebSocketRemote<LogItem>(_endpointUrl, true);
            repo.AddRemote(remote);
            await Task.Factory.StartNew(async () => await repo.PullAsync(true));
            await Task.Delay(300);
            await repo.CommitAsync(new LogItem { Message = "1" });
            await Task.Delay(300);
            Assert.IsNotNull(remote.Position);
        }

        [TestMethod]
        public async Task ManualPushPull()
        {
            var repo = new Repository<LogItem>();
            repo.AddRemote(new WebSocketRemote<LogItem>(_endpointUrl));

            await repo.CommitAsync(new LogItem { Message = "1" });
            await repo.CommitAsync(new LogItem { Message = "2" });
            await repo.CommitAsync(new LogItem { Message = "3" });
            await repo.PushAsync();
            await Task.Delay(300);

            HomeController._repo.Assert(
                new LogItem { Message = "1" },
                new LogItem { Message = "2" },
                new LogItem { Message = "3" });

            var repo2 = new Repository<LogItem>();
            repo2.AddRemote(new WebSocketRemote<LogItem>(_endpointUrl));
            await repo2.PullAsync();

            repo2.Assert(
                new LogItem { Message = "1" },
                new LogItem { Message = "2" },
                new LogItem { Message = "3" });
        }

        [TestMethod]
        public async Task OnewayAutoPull()
        {
            var repo = new Repository<LogItem>();
            repo.AddRemote(new WebSocketRemote<LogItem>(_endpointUrl, autoPush: true));

            var repo2 = new Repository<LogItem>();
            repo2.AddRemote(new WebSocketRemote<LogItem>(_endpointUrl));
            await Task.Factory.StartNew(async () => await repo2.PullAsync(true));
            await Task.Delay(300);

            await repo.CommitAsync(new LogItem { Message = "1" });
            await Task.Delay(300);
            await repo.CommitAsync(new LogItem { Message = "2" });
            await Task.Delay(300);
            await repo.CommitAsync(new LogItem { Message = "3" });
            await Task.Delay(300);

            repo.Assert(
                new LogItem { Message = "1" },
                new LogItem { Message = "2" },
                new LogItem { Message = "3" });
            repo2.Assert(
                new LogItem { Message = "1" },
                new LogItem { Message = "2" },
                new LogItem { Message = "3" });
        }

        [TestMethod]
        public async Task DoubleWaySync()
        {
            var repoA = new Repository<LogItem>();
            repoA.AddRemote(new WebSocketRemote<LogItem>(_endpointUrl, autoPush: true));
            await Task.Factory.StartNew(async () => await repoA.PullAsync(true));

            var repoB = new Repository<LogItem>();
            repoB.AddRemote(new WebSocketRemote<LogItem>(_endpointUrl, autoPush: true));
            await Task.Factory.StartNew(async () => await repoB.PullAsync(true));

            await Task.Delay(300);

            await Task.WhenAll(
                repoA.CommitAsync(new LogItem { Message = "1" }),
                repoA.CommitAsync(new LogItem { Message = "2" }),
                repoA.CommitAsync(new LogItem { Message = "3" })
            );

            await Task.WhenAll(
                repoB.CommitAsync(new LogItem { Message = "4" }),
                repoB.CommitAsync(new LogItem { Message = "5" }),
                repoB.CommitAsync(new LogItem { Message = "6" })
            );

            await Task.Delay(1300);

            HomeController._repo.Assert(
                new LogItem { Message = "1" },
                new LogItem { Message = "2" },
                new LogItem { Message = "3" },
                new LogItem { Message = "4" },
                new LogItem { Message = "5" },
                new LogItem { Message = "6" });
            repoA.Assert(
                new LogItem { Message = "1" },
                new LogItem { Message = "2" },
                new LogItem { Message = "3" },
                new LogItem { Message = "4" },
                new LogItem { Message = "5" },
                new LogItem { Message = "6" });
            repoB.Assert(
                new LogItem { Message = "1" },
                new LogItem { Message = "2" },
                new LogItem { Message = "3" },
                new LogItem { Message = "4" },
                new LogItem { Message = "5" },
                new LogItem { Message = "6" });
        }

        [TestMethod]
        public async Task DoubleWayDataBinding()
        {
            var repoA = new Repository<LogItem>();
            repoA.AddRemote(new WebSocketRemote<LogItem>(_endpointUrl, autoPush: true) { Name = "A to server" });
            await Task.Factory.StartNew(async () => await repoA.PullAsync(true));

            var repoB = new Repository<LogItem>();
            repoB.AddRemote(new WebSocketRemote<LogItem>(_endpointUrl, autoPush: true) { Name = "B to server" });
            await Task.Factory.StartNew(async () => await repoB.PullAsync(true));

            await Task.Delay(300);

            await repoA.CommitAsync(new LogItem { Message = "1" });
            await repoA.CommitAsync(new LogItem { Message = "2" });
            await repoB.CommitAsync(new LogItem { Message = "3" });
            await repoB.CommitAsync(new LogItem { Message = "4" });

            await Task.Delay(300);

            HomeController._repo.Assert(
                new LogItem { Message = "1" },
                new LogItem { Message = "2" },
                new LogItem { Message = "3" },
                new LogItem { Message = "4" });

            repoA.Assert(
                new LogItem { Message = "1" },
                new LogItem { Message = "2" },
                new LogItem { Message = "3" },
                new LogItem { Message = "4" });
            repoB.Assert(
                new LogItem { Message = "1" },
                new LogItem { Message = "2" },
                new LogItem { Message = "3" },
                new LogItem { Message = "4" });
        }
    }

    public static class TestExtends
    {
        public static void Assert<T>(this Repository<T> repo, params T[] array)
        {
            var commits = repo.Commits.ToArray();
            if (commits.Count() != array.Length)
            {
                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Two repo don't match! Expected: {string.Join(',', array.Select(t => t.ToString()))}; Actual: {string.Join(',', repo.Commits.Select(t => t.ToString()))}");
            }
            for (int i = 0; i < commits.Count(); i++)
            {
                if (!commits[i].Item.Equals(array[i]))
                {
                    Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail($"Two repo don't match! Expected: {string.Join(',', array.Select(t => t.ToString()))}; Actual: {string.Join(',', repo.Commits.Select(t => t.ToString()))}");
                }
            }
        }
    }
}
