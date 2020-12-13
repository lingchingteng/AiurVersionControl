﻿using AiurEventSyncer.Models;
using AiurEventSyncer.Remotes;
using AiurEventSyncer.Tools;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace AiurEventSyncer.WebExtends
{
    public static class WebExtends
    {
        public static async Task<IActionResult> BuildWebActionResultAsync<T>(this ControllerBase controller, Repository<T> repository, string startPosition)
        {
            if (controller.HttpContext.WebSockets.IsWebSocketRequest)
            {
                var ws = await controller.HttpContext.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine($"[SERVER]: New Websocket client online! Status: '{ws.State}'");
                // Send pull result.
                var pullResult = repository.Commits.AfterCommitId(startPosition).ToList();
                await ws.SendObject(pullResult);
                Func<List<Commit<T>>, Task> pushEvent = async (List<Commit<T>> newCommits) =>
                {
                    // Broadcast new commits.
                    Console.WriteLine($"[SERVER]: I was changed with: {string.Join(',', newCommits.Select(t => t.Item.ToString()))}! Broadcasting to a remote...");
                    await ws.SendObject(newCommits);
                };
                var key = DateTime.UtcNow;
                repository.OnNewCommitsSubscribers[key]= pushEvent;
                Console.WriteLine($"[SERVER] New Websocket subscriber registered! Current registers: {repository.OnNewCommitsSubscribers.Count}.");
                while (ws.State == WebSocketState.Open)
                {
                    // Waitting for pushed commits.
                    var pushedCommits = await ws.GetObject<PushModel<T>>();
                    Console.WriteLine($"[SERVER]: I got a new push request with commits: {string.Join(',', pushedCommits.Commits.Select(t => t.Item.ToString()))}.");
                    await repository.OnPushed(pushedCommits.Commits, pushedCommits.Start);
                }
                Console.WriteLine($"[SERVER]: Websocket dropped! Reason: '{ws.State}'");
                repository.OnNewCommitsSubscribers.TryRemove(key, out _);
                return new EmptyResult();
            }
            else
            {
                return new BadRequestResult();
            }
        }
    }
}
