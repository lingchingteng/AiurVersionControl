﻿using AiurStore.Abstracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiurStore.Providers.MemoryProvider
{
    public class MemoryStoreProvider : IStoreProvider
    {
        private readonly ConcurrentBag<string> _store = new ConcurrentBag<string>();

        public void Drop()
        {
            _store.Clear();
        }

        public IEnumerable<string> GetAll()
        {
            return _store;
        }

        public void Insert(string newItem)
        {
            _store.Add(newItem);
        }
    }
}
