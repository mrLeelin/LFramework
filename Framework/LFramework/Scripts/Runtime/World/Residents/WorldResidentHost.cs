using System;
using System.Collections.Generic;
using System.Linq;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>统一管理按 World 范围驻留的对象；所有操作应在主线程执行。</summary>
    /// <remarks>
    /// 自动注册具体类型及业务接口，排除注入、释放和更新接口。服务注册不转移所有权。
    /// 初始化通过注入访问候选服务；只有初始化成功后才向根容器发布。
    /// </remarks>
    public sealed class WorldResidentHost : GameFrameworkComponent
    {
        private readonly Dictionary<Type, Descriptor> _descriptors = new Dictionary<Type, Descriptor>();
        private List<Entry> _active = new List<Entry>();
        private Pending _pending;
        private bool _operating;
        private bool _dispatching;
        private bool _shutdown;

        /// <summary>已提交的活动对象数量；准备阶段新增对象不计入。</summary>
        public int ActiveCount => _active.Count;

        /// <summary>原子验证并缓存发现的类型，不创建实例；重复登记同一类型不会重复创建。</summary>
        public void RegisterTypes(IEnumerable<Type> types)
        {
            EnsureAvailable();
            if (types == null) throw new ArgumentNullException(nameof(types));
            var additions = new Dictionary<Type, Descriptor>();
            foreach (var type in types)
            {
                if (type == null) throw new ArgumentException("Resident type cannot be null.", nameof(types));
                if (_descriptors.ContainsKey(type) || additions.ContainsKey(type)) continue;
                var attribute = (WorldResidentAttribute)Attribute.GetCustomAttribute(type, typeof(WorldResidentAttribute), false);
                if (attribute == null) continue;
                if (!typeof(WorldResidentBase).IsAssignableFrom(type) || type.IsAbstract || type.ContainsGenericParameters ||
                    type.GetConstructor(Type.EmptyTypes) == null)
                    throw new ArgumentException("Resident must be concrete with a public parameterless constructor: " + type.FullName);
                var worlds = attribute.WorldTypes;
                if (worlds.Length == 0 || worlds.Any(world => world == null || world == typeof(WorldBase) ||
                    !typeof(WorldBase).IsAssignableFrom(world) || world.IsAbstract || world.ContainsGenericParameters))
                    throw new ArgumentException("Resident requires concrete WorldBase-derived World types: " + type.FullName);
                var aliases = new List<Type> { type };
                aliases.AddRange(type.GetInterfaces().Where(service => service != typeof(IInjectable) &&
                    service != typeof(IDisposable) && service != typeof(IWorldResidentUpdate) &&
                    service != typeof(IWorldResidentLateUpdate)).OrderBy(service => service.FullName, StringComparer.Ordinal));
                additions.Add(type, new Descriptor(type, worlds.Distinct().ToArray(), aliases.ToArray(), attribute.Sort));
            }
            foreach (var pair in additions) _descriptors.Add(pair.Key, pair.Value);
        }

        /// <summary>准备下一 World 的服务；null 表示退出全部 World。调用方成功进入后必须提交。</summary>
        public WorldResidentTransition BeginTransition(Type targetWorldType)
        {
            EnsureAvailable();
            if (_active.Any(entry => entry.World != null))
                throw new InvalidOperationException("Exit the current World before preparing another World.");
            if (targetWorldType != null && (targetWorldType == typeof(WorldBase) ||
                !typeof(WorldBase).IsAssignableFrom(targetWorldType) || targetWorldType.IsAbstract || targetWorldType.ContainsGenericParameters))
                throw new ArgumentException("Target must be a concrete World type.", nameof(targetWorldType));
            var descriptors = _descriptors.Values.Where(item => targetWorldType != null && item.Worlds.Contains(targetWorldType))
                .OrderByDescending(item => item.Sort).ThenBy(item => item.Type.FullName, StringComparer.Ordinal).ToList();
            ValidateAliases(descriptors);
            var transition = new WorldResidentTransition(this);
            var pending = new Pending(transition, targetWorldType);
            _pending = pending;
            _operating = true;
            try
            {
                using (var scope = LServices.CreateScope())
                {
                    foreach (var descriptor in descriptors)
                    {
                        var entry = _active.Find(item => item.Descriptor.Type == descriptor.Type);
                        if (entry == null)
                        {
                            entry = new Entry(descriptor, (WorldResidentBase)Activator.CreateInstance(descriptor.Type));
                            pending.Added.Add(entry);
                        }
                        pending.Target.Add(entry);
                        foreach (var alias in descriptor.Aliases) scope.Register(alias, entry.Instance);
                    }
                    foreach (var entry in pending.Added)
                        Injection.Inject((object)entry.Instance, new ResidentResolver(scope, entry.Descriptor, pending.Target, _active));
                    foreach (var entry in _active.Where(item => !pending.Target.Contains(item)))
                    foreach (var alias in entry.Descriptor.Aliases)
                    {
                        if (LServices.TryGet(alias, null, out var current) && ReferenceEquals(current, entry.Instance))
                        {
                            pending.Published.Add(new Binding(alias, current));
                            LServices.Unregister(alias);
                        }
                    }
                    foreach (var entry in pending.Added) entry.Instance.InitializeResident();
                    // 初始化回调可能注册外部服务，发布前再次验证所有权。
                    ValidateAliases(descriptors);
                    foreach (var entry in pending.Added)
                    foreach (var alias in entry.Descriptor.Aliases)
                    {
                        LServices.TryGet(alias, null, out var previous);
                        pending.Published.Add(new Binding(alias, previous));
                        LServices.Register(alias, entry.Instance);
                    }
                }
                return transition;
            }
            catch (Exception exception)
            {
                var errors = new List<Exception> { exception };
                Rollback(pending, errors);
                throw new AggregateException("Resident transition preparation failed.", errors);
            }
            finally { _operating = false; }
        }

        /// <summary>框架在进入、退出流程或关闭应用前检查，禁止生命周期回调中同步重入。</summary>
        public void EnsureLifecycleIdle()
        {
            if (_operating) throw new InvalidOperationException("Resident lifecycle operation is already running.");
        }

        internal void EnterWorld(WorldResidentTransition transition, WorldBase world)
        {
            EnsureLifecycleIdle();
            ValidateTransition(transition);
            if (world?.GetType() != _pending.WorldType)
                throw new InvalidOperationException("The World instance does not match the prepared transition.");
            if (world == null) return;
            if (_pending.Entered.Count > 0)
                throw new InvalidOperationException("World entry has already been attempted for this transition.");
            _operating = true;
            try
            {
                foreach (var entry in _pending.Target)
                {
                    if (entry.World != null)
                        throw new InvalidOperationException("Resident is still attached to a World.");
                    // 先记录尝试，确保回调部分执行后抛错也能补偿退出。
                    entry.World = world;
                    _pending.Entered.Add(entry);
                    entry.Instance.EnterWorldResident(world);
                }
            }
            finally { _operating = false; }
        }

        /// <summary>在 Provider/World 清理前退出当前附着；失败项保留，成功项不重复通知。</summary>
        public void ExitWorld()
        {
            EnsureLifecycleIdle();
            _operating = true;
            var errors = new List<Exception>();
            try
            {
                var entries = _pending == null ? _active : _active.Concat(_pending.Target).Distinct().ToList();
                entries = entries.OrderByDescending(entry => entry.Descriptor.Sort)
                    .ThenBy(entry => entry.Descriptor.Type.FullName, StringComparer.Ordinal).ToList();
                ExitEntries(entries, errors);
            }
            finally { _operating = false; }
            ThrowErrors(errors);
        }

        internal void CompleteTransition(WorldResidentTransition transition, bool commit)
        {
            if (_operating) throw new InvalidOperationException("Resident lifecycle operation is already running.");
            if (_pending == null || !ReferenceEquals(_pending.Transition, transition))
                throw new InvalidOperationException("Transition is not owned by this host.");
            if (commit) ValidateTransition(transition);
            _operating = true;
            var errors = new List<Exception>();
            try
            {
                var pending = _pending;
                if (commit)
                {
                    var outgoing = _active.Where(entry => !pending.Target.Contains(entry)).ToList();
                    _active = pending.Target;
                    _pending = null;
                    transition.Detach();
                    Release(outgoing, errors);
                }
                else Rollback(pending, errors);
            }
            finally { _operating = false; }
            ThrowErrors(errors);
        }

        internal void ValidateTransition(WorldResidentTransition transition)
        {
            if (_pending == null || !ReferenceEquals(_pending.Transition, transition))
                throw new InvalidOperationException("Transition is not owned by this host.");
            foreach (var entry in _pending.Target)
            foreach (var alias in entry.Descriptor.Aliases)
            {
                if (!LServices.TryGet(alias, null, out var current) || !ReferenceEquals(current, entry.Instance))
                    throw new InvalidOperationException("Resident service was replaced during World initialization: " + alias.FullName);
            }
        }

        /// <summary>清理当前加载会话及类型缓存，宿主保留供后续热更会话使用。</summary>
        public void ClearResidents()
        {
            ExitWorld();
            if (_operating) throw new InvalidOperationException("Resident lifecycle operation is already running.");
            _operating = true;
            var errors = new List<Exception>();
            try
            {
                if (_pending != null) Rollback(_pending, errors);
                var active = _active;
                _active = new List<Entry>();
                _descriptors.Clear();
                Release(active, errors);
            }
            finally { _operating = false; }
            ThrowErrors(errors);
        }

        public override void ShutDown()
        {
            if (_shutdown) return;
            if (_operating) throw new InvalidOperationException("Resident lifecycle operation is already running.");
            try { ClearResidents(); }
            finally { _shutdown = _pending == null && _active.Count == 0; }
        }

        public override void UpdateComponent(float elapseSeconds, float realElapseSeconds)
        {
            if (_shutdown || _pending != null || _operating || _dispatching) return;
            _dispatching = true;
            try
            {
                var snapshot = _active;
                foreach (var entry in snapshot)
                {
                    if (_shutdown || _pending != null) break;
                    if (entry.Instance.IsActive && entry.Instance is IWorldResidentUpdate update)
                        update.Update(elapseSeconds, realElapseSeconds);
                }
            }
            finally { _dispatching = false; }
        }

        public override void LateUpdate()
        {
            if (_shutdown || _pending != null || _operating || _dispatching) return;
            _dispatching = true;
            try
            {
                var snapshot = _active;
                foreach (var entry in snapshot)
                {
                    if (_shutdown || _pending != null) break;
                    if (entry.Instance.IsActive && entry.Instance is IWorldResidentLateUpdate update)
                        update.LateUpdate();
                }
            }
            finally { _dispatching = false; }
        }

        private void ValidateAliases(List<Descriptor> target)
        {
            var aliases = new HashSet<Type>();
            foreach (var descriptor in target)
            foreach (var alias in descriptor.Aliases)
            {
                if (!aliases.Add(alias)) throw new InvalidOperationException("Duplicate resident service: " + alias.FullName);
                if (LServices.TryGet(alias, null, out var existing) && !_active.Any(entry =>
                    ReferenceEquals(existing, entry.Instance) && entry.Descriptor.Aliases.Contains(alias)))
                    throw new InvalidOperationException("Resident service conflicts with an external registration: " + alias.FullName);
            }
        }

        private void Rollback(Pending pending, List<Exception> errors)
        {
            var exitErrors = new List<Exception>();
            ExitEntries(pending.Entered, exitErrors);
            if (exitErrors.Count > 0)
            {
                errors.AddRange(exitErrors);
                return;
            }
            _pending = null;
            pending.Transition.Detach();
            Release(pending.Added, errors);
            for (var i = pending.Published.Count - 1; i >= 0; i--)
            {
                var binding = pending.Published[i];
                try
                {
                    // 仅恢复自己替换过且未被第三方占用的绑定。
                    if (binding.Previous != null && !LServices.TryGet(binding.Alias, null, out _))
                        LServices.Register(binding.Alias, binding.Previous);
                }
                catch (Exception exception) { errors.Add(exception); }
            }
            // 目标 Provider/Helper 回滚时可能移除了保留对象的接口，补回缺失的注册。
            foreach (var entry in _active)
            foreach (var alias in entry.Descriptor.Aliases)
            {
                try
                {
                    if (!LServices.TryGet(alias, null, out _)) LServices.Register(alias, entry.Instance);
                }
                catch (Exception exception) { errors.Add(exception); }
            }
        }

        private static void ExitEntries(List<Entry> entries, List<Exception> errors)
        {
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                if (entry.World == null) continue;
                try
                {
                    entry.Instance.ExitWorldResident(entry.World);
                    entry.World = null;
                }
                catch (Exception exception) { errors.Add(exception); }
            }
        }

        private static void Release(List<Entry> entries, List<Exception> errors)
        {
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                try { entry.Instance.ShutdownResident(); }
                catch (Exception exception) { errors.Add(exception); }
                foreach (var alias in entry.Descriptor.Aliases)
                {
                    try
                    {
                        if (LServices.TryGet(alias, null, out var existing) && ReferenceEquals(existing, entry.Instance))
                            LServices.Unregister(alias);
                    }
                    catch (Exception exception) { errors.Add(exception); }
                }
            }
        }

        private void EnsureAvailable()
        {
            if (_shutdown) throw new ObjectDisposedException(nameof(WorldResidentHost));
            if (_operating || _pending != null) throw new InvalidOperationException("Finish the current resident operation first.");
        }

        private static void ThrowErrors(List<Exception> errors)
        {
            if (errors.Count > 0) throw new AggregateException("Resident cleanup failed.", errors);
        }

        private sealed class Descriptor
        {
            public readonly Type Type;
            public readonly Type[] Worlds;
            public readonly Type[] Aliases;
            public readonly int Sort;
            public Descriptor(Type type, Type[] worlds, Type[] aliases, int sort)
            { Type = type; Worlds = worlds; Aliases = aliases; Sort = sort; }
        }

        private sealed class Entry
        {
            public readonly Descriptor Descriptor;
            public readonly WorldResidentBase Instance;
            public WorldBase World;
            public Entry(Descriptor descriptor, WorldResidentBase instance) { Descriptor = descriptor; Instance = instance; }
        }

        private sealed class Binding
        {
            public readonly Type Alias;
            public readonly object Previous;
            public Binding(Type alias, object previous)
            { Alias = alias; Previous = previous; }
        }

        private sealed class Pending
        {
            public readonly WorldResidentTransition Transition;
            public readonly Type WorldType;
            public readonly List<Entry> Target = new List<Entry>();
            public readonly List<Entry> Entered = new List<Entry>();
            public readonly List<Entry> Added = new List<Entry>();
            public readonly List<Binding> Published = new List<Binding>();
            public Pending(WorldResidentTransition transition, Type worldType)
            { Transition = transition; WorldType = worldType; }
        }

        private sealed class ResidentResolver : IServiceResolver
        {
            private readonly LServiceScope _scope;
            private readonly Descriptor _owner;
            private readonly List<Entry> _target;
            private readonly List<Entry> _previous;
            public ResidentResolver(LServiceScope scope, Descriptor owner, List<Entry> target, List<Entry> previous)
            { _scope = scope; _owner = owner; _target = target; _previous = previous; }
            public T Get<T>() where T : class => (T)Get(typeof(T));
            public T Get<T>(object identifier) where T : class => (T)Get(typeof(T), identifier);
            public object Get(Type serviceType, object identifier = null)
            {
                if (TryGet(serviceType, identifier, out var service)) return service;
                throw new InvalidOperationException("Resident dependency unavailable in target World: " + serviceType.FullName);
            }
            public bool TryGet<T>(out T service) where T : class => TryGet(null, out service);
            public bool TryGet<T>(object identifier, out T service) where T : class
            {
                var found = TryGet(typeof(T), identifier, out var value);
                service = (T)value;
                return found;
            }
            public bool TryGet(Type serviceType, object identifier, out object service)
            {
                if (!_scope.TryGet(serviceType, identifier, out service)) return false;
                var resolved = service;
                var resident = _target.Find(entry => ReferenceEquals(entry.Instance, resolved));
                if (resident != null)
                {
                    if (_owner.Worlds.Any(world => !resident.Descriptor.Worlds.Contains(world)))
                        throw new InvalidOperationException("Resident dependency has a shorter World scope: " + serviceType.FullName);
                    return true;
                }
                if (_previous.Any(entry => ReferenceEquals(entry.Instance, resolved)))
                {
                    service = null;
                    return false;
                }
                if (resolved is IWorld || resolved is IWorldHelper || resolved is ISystemProvider)
                    throw new InvalidOperationException("Resident cannot retain a World, WorldHelper or Provider dependency: " + serviceType.FullName);
                return true;
            }
        }
    }
}
