﻿using AiurEventSyncer.Models;
using AiurStore.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiurEventSyncer.Abstract
{
    public interface IRepository<T>
    {
        void Register(Guid key, Func<List<Commit<T>>, Task> action, bool async = true);
        void UnRegister(Guid key);
        InOutDatabase<Commit<T>> Commits { get; }
        Task OnPulled(List<Commit<T>> subtraction, IRemote<T> remoteRecord);
    }
}
