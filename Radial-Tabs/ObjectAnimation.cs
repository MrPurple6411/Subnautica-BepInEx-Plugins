﻿using System.Collections.Generic;
using System;

namespace Radial_Tabs
{
    public abstract class ObjectAnimation<TActor>
        where TActor : UnityEngine.Object
    {
        private static readonly Dictionary<TActor, ObjectAnimation<TActor>> animations = new Dictionary<TActor, ObjectAnimation<TActor>>();
        private static readonly List<TActor> toRemove = new List<TActor>();

        static ObjectAnimation()
        {
            Updater.updated += OnAnimationsUpdate;
        }

        private static void OnAnimationsUpdate()
        {
            foreach (var pair in animations)
            {
                var actor = pair.Key;
                var anim = pair.Value;
                try
                {
                    var playing = anim.OnUpdate(actor);
                    if (!playing)
                        toRemove.Add(actor);
                }
                catch
                {
                    toRemove.Add(actor);
                }
            }
            toRemove.ForEach(x => animations.Remove(x));
            toRemove.Clear();
        }

        public void Play(TActor actor)
        {
            if (!actor)
                throw new ArgumentException("actor is null or destroyed");

            if (animations.ContainsKey(actor))
            {
                try
                { animations[actor].OnStop(actor); }
                catch
                {
                }
            }

            try
            { OnStart(actor); }
            catch
            {
                return;
            }
            animations[actor] = this;
        }

        protected abstract void OnStart(TActor actor);
        protected abstract bool OnUpdate(TActor actor);
        protected abstract void OnStop(TActor actor);
    }
}