using System;
using UnityEngine;
using VContainer.Unity;

namespace LFramework.Runtime
{
    /// <summary>
    /// Performs framework-controlled injection for runtime-created objects.
    /// </summary>
    public sealed class FrameworkInjector
    {
        private readonly FrameworkResolverContext _resolverContext;

        public FrameworkInjector(FrameworkResolverContext resolverContext)
        {
            _resolverContext = resolverContext ?? throw new ArgumentNullException(nameof(resolverContext));
        }

        public void Inject(object instance)
        {
            _resolverContext.ActiveResolver?.Inject(instance);
        }

        public void InjectGameObject(GameObject gameObject)
        {
            _resolverContext.ActiveResolver?.InjectGameObject(gameObject);
        }
    }
}
