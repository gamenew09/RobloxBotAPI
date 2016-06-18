using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobloxBotAPI
{
    public class ObjectCache<T> : IDisposable
    {

        private T _Object = default(T);

        public class UpdateCacheEventArgs : EventArgs
        {

            private ObjectCache<T> _Cache;

            public ObjectCache<T> Cache
            {
                get { return _Cache; }
            }

            public UpdateCacheEventArgs(ObjectCache<T> cache)
            {
                _Cache = cache;
            }

            public T Object
            {
                get { return _Cache._Object; }
                set { _Cache._Object = value; }
            }
                

        }

        public event EventHandler<UpdateCacheEventArgs> UpdateCache;

        public T Object
        {
            get { return _Object; }
        }

        public double CacheLength
        {
            get;
            set;
        }

        private DateTime _FromTime, _EstimatedTime;

        Thread cacheThread;

        private bool _ThreadActive = true;

        public ObjectCache(bool shouldThread = true)
        {
            _FromTime = DateTime.Now;
            _EstimatedTime = _FromTime.AddSeconds(CacheLength);
            if (shouldThread)
            {
                cacheThread = new Thread(() =>
                {
                    while (_ThreadActive)
                    {
                        while (DateTime.Now < _EstimatedTime && _ThreadActive) Thread.Sleep(1);

                        if (!_ThreadActive)
                            break;

                        Update();
                    }
                });
                cacheThread.Start();
            }
        }

        public ObjectCache(T obj, bool shouldThread = true)
            : this(shouldThread)
        {
            _Object = obj;
        }


        public void Dispose()
        {
            _ThreadActive = false;
        }

        public void Update()
        {
            if (UpdateCache != null)
                UpdateCache(this, new UpdateCacheEventArgs(this));

            _FromTime = DateTime.Now;
            _EstimatedTime = _FromTime.AddSeconds(CacheLength);
        }

        public bool TryUpdate()
        {
            if(DateTime.Now < _EstimatedTime)
            {
                Update();
                return true;
            }
            return false;
        }
    }
}
