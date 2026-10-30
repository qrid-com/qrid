using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Qrid.Common.Crypto.Certificate
{
    public class CertCache
    {
        // this class has to be thread-save b/c when it is used server-side in a Rest Api
        // then the same instance of CartCache may be used by several threads processing concurrent requests

        // must be thread-safe b/c we use singleton here
        public static CertCache Instance = new CertCache();

        public ConcurrentDictionary<int, List<CertCacheItem>> Items { get; set; }


        public CertCache()
        {
            Items = new ConcurrentDictionary<int, List<CertCacheItem>>();
        }



        public CertCacheItem GetItemCompareAllBytes(int hash, ByteString certificateData)
        {
            if (Items.TryGetValue(hash, out var list2))
                lock(list2)
                {
                    foreach (var item in list2)
                        // ByteString has == overriden so it does compare all bytes
                        if (item.CertificateData == certificateData)
                            // this cert is in the cache
                            return item;
                }

            return null;
        }

        public void AddItem(CertCacheItem newItem)
        {
            // This code is ugly. However, Items.GetOrAdd and Items.AddOrUpdate does not provide required atomicity and would also need some lock{}ing

            var newList = new List<CertCacheItem> { newItem };

            if (!Items.TryAdd(newItem.Key32, newList))
                if (Items.TryGetValue(newItem.Key32, out var list2))
                    lock (list2)
                    {
                        list2.Add(newItem);
                    }
                else
                    throw new ApplicationException($"this should never happen - not able to Add nor able to Get from ConcurrentDictionary, Key32={newItem.Key32}");
        }

        
    }
}
