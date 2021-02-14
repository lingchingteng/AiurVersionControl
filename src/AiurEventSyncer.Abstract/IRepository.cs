﻿using AiurStore.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiurEventSyncer.Abstract
{
    public interface IRepository<T>
    {
        void RegisterAsyncTask(Guid key, Func<List<Commit<T>>, Task> action);
        void Register(Guid key, Action<List<Commit<T>>> action);
        void UnRegister(Guid key);
        InOutDatabase<Commit<T>> Commits { get; }
        bool OnPulled(IEnumerable<Commit<T>> subtraction, IRemote<T> remoteRecord);
        void OnPushed(IEnumerable<Commit<T>> commitsToPush, string startPosition);
    }
}
