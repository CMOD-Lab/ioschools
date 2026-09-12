using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Runtime.Caching;
using clearpixels.Logging;

namespace ioschools.DB.caching
{
    /// <summary>
    ///  requirements on supported queries http://msdn.microsoft.com/en-us/library/ms181122.aspx
    ///  debugging help? http://rusanu.com/2006/06/17/the-mysterious-notification/
    /// </summary>
    public static class CacheHelper
    {
        private static readonly MemoryCache _cache = MemoryCache.Default;

        public static string CreateKey(object[] keys)
        {
            return string.Concat(String.Join(":", keys));
        }

        /// <summary>
        /// Caches Linq query´s that is created for LinqToSql.
        /// Limitations are the same as SqlCacheDependency
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="q">The linq query</param>
        /// <param name="dc">Your LinqToSql DataContext</param>
        /// <param name="CacheId">The unique Id for the cache</param>
        /// <returns></returns>
        public static List<T> LinqCache<T>(this IQueryable<T> q, DataContext dc, string CacheId)
        {
            try
            {
                List<T> objCache = (List<T>)_cache.Get(CacheId);

                if (objCache == null)
                {
                    // Execute LINQ-query and cache the result
                    objCache = q.ToList();

                    var policy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(5)
                    };

                    _cache.Set(CacheId, objCache, policy);
                }

                return objCache;
            }
            catch (Exception ex)
            {
                Syslog.Write(ex);
                throw;
            }
        }
    }
}
