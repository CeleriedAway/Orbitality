﻿using System;
using System.Collections.Generic;
using UnityEngine;
using ZergRush.ReactiveCore;

namespace ZergRush
{
    interface IUpdatable
    {
        void Update(float dt);
    }
    
    class UnityExecutor : MonoBehaviour
    {
        static UnityExecutor instance_val;

        List<IUpdatable> updatables = new List<IUpdatable>();
        List<IUpdatable> updatablesRealtime = new List<IUpdatable>();
        Dictionary<float, Tick> intervalTicks = new Dictionary<float, Tick>();
        public EventStream appExit = new EventStream();

        public Cell<float> time = new Cell<float>();

        class Tick
        {
            public float current;
            public EventStream stream;
        }

        void OnDisable()
        {
            appExit.Send();
        }

        public EventStream TickStream(float delay)
        {
            Tick val;
            if (!intervalTicks.TryGetValue(delay, out val))
            {
                val = new Tick();
                val.stream = new EventStream();
                intervalTicks[delay] = val;
            }

            return val.stream;
        }

        public void RegisterUpdatable(IUpdatable updatable)
        {
            updatables.Add(updatable);
        }
        public void RegisterRealtimeUpdatable(IUpdatable updatable)
        {
            updatablesRealtime.Add(updatable);
        }

        public void RemoveUpdatable(IUpdatable updatable)
        {
            updatables.Remove(updatable);
            updatablesRealtime.Remove(updatable);
        }

        // DestroyUpdatable = RemoveUpdatable, but can be called in OnDestroy.
        public static void DestroyUpdatable(IUpdatable updatable)
        {
            if (instance_val == null)
                return;
            instance.RemoveUpdatable(updatable);
        }

        class RemoveUpdateDisposable : IDisposable
        {
            public IUpdatable updatable;

            public void Dispose()
            {
                DestroyUpdatable(updatable);
            }
        }

        EventStream eachFrameEvent;

        public EventStream eachFrame
        {
            get
            {
                if (eachFrameEvent == null)
                {
                    eachFrameEvent = new EventStream();
                }

                return eachFrameEvent;
            }
        }

        public IDisposable AddUpdatable(IUpdatable updatable)
        {
            RegisterUpdatable(updatable);
            return new RemoveUpdateDisposable {updatable = updatable};
        }
        public IDisposable AddRealtimeUpdatable(IUpdatable updatable)
        {
            RegisterRealtimeUpdatable(updatable);
            return new RemoveUpdateDisposable { updatable = updatable };
        }
        

        void Update()
        {
            float dt = Time.deltaTime;
            time.value += dt;
            for (int i = 0; i < updatables.Count; i++)
            {
                updatables[i].Update(dt);
            }
            for (int i = 0; i < updatablesRealtime.Count; i++)
            {
                updatablesRealtime[i].Update(Time.unscaledDeltaTime);
            }
            

            foreach (var tick in intervalTicks)
            {
                tick.Value.current += dt;
                if (tick.Value.current > tick.Key)
                {
                    tick.Value.current -= tick.Key;
                    tick.Value.stream.Send();
                }
            }

            if (eachFrameEvent != null) eachFrameEvent.Send();
        }

        public static UnityExecutor instance
        {
            get
            {
                if (instance_val == null)
                {
                    var obj = new GameObject("ZergRushExecuter");
                    instance_val = obj.AddComponent<UnityExecutor>();
                    GameObject.DontDestroyOnLoad(instance_val);
                }

                return instance_val;
            }
        }
    }
}