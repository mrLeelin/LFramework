using System;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public class ViewComponent : GameFrameworkComponent
    {
        private readonly HashSet<IView> _views = new HashSet<IView>();
        private readonly Dictionary<(Type, string), IViewModel> _viewModelCache = new();

        public override void AwakeComponent()
        {
            base.AwakeComponent();
        }

        public override void ShutDown()
        {
            base.ShutDown();
            _views.Clear();
            _viewModelCache.Clear();
        }

        public void ViewCreated(IView view)
        {
            InternalViewCreated(view);
        }

        public void ViewDestroyed(IView view)
        {
            InternalViewDestroyed(view);
        }

        #region Private Method

        private void InternalViewCreated(IView view)
        {
            view.OnViewBeCreate();
            if (view.ViewModelObject == null && view.ViewModelType != null)
            {
                FetchViewModel(view);
            }

            if (!_views.Contains(view))
            {
                _views.Add(view);
            }
        }

        private void InternalViewDestroyed(IView view)
        {
            if (view == null)
            {
                Log.Fatal("View base is null.");
                return;
            }

            var vm = view.ViewModelObject;
            view.ViewModelObject = null;
            if (vm != null)
            {
                if (--vm.References == 0)
                {
                    ReleaseViewModel(vm, view);
                }
            }

            var isRemoved = _views.Remove(view);
            if (!isRemoved)
            {
                Log.Error(
                    $"View '{view.GetType().FullName}' not exists in list of view. so it was not removed.");
            }

            view.OnViewBeDestroy();
        }

        /// <summary>
        /// Find already exist view model
        /// </summary>
        /// <param name="view"></param>
        private void FetchViewModel(IView view)
        {
            if (view.ViewModelObject != null)
            {
                return;
            }

            var key = (view.ViewModelType,
                string.IsNullOrEmpty(view.Identifier) ? null : view.Identifier);

            if (_viewModelCache.TryGetValue(key, out var existing))
            {
                existing.References++;
                view.ViewModelObject = existing;
                return;
            }

            var vm = CreateViewModel(view.ViewModelType, view.Identifier);
            if (vm == null)
            {
                Log.Fatal(
                    $"Create View Model is null. ViewModelType:{view.ViewModelType} View :{view.GetType().FullName} Identifier : {view.Identifier}");
                return;
            }

            _viewModelCache[key] = vm;
            vm.References++;
            view.ViewModelObject = vm;
        }

        private IViewModel CreateViewModel(Type type, string identifier = null)
        {
            if (ReferencePool.Acquire(type) is not IViewModel vm)
            {
                return null;
            }

            LFrameworkAspect.Instance.FrameworkInjector.Inject(vm);
            vm.Identifier = identifier;
            vm.Initialize();
            return vm;
        }

        private void ReleaseViewModel(IViewModel viewModel, IView view)
        {
            var key = (view.ViewModelType,
                string.IsNullOrEmpty(viewModel.Identifier) ? null : viewModel.Identifier);
            _viewModelCache.Remove(key);
            ReferencePool.Release(viewModel);
        }

        #endregion
    }
}
