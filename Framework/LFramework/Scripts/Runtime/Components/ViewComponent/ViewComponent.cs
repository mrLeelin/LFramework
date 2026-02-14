using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    public class ViewComponent : GameFrameworkComponent
    {
        private readonly HashSet<IView> _views = new HashSet<IView>();


        

        public override void AwakeComponent()
        {
            base.AwakeComponent();
        }

        public override void ShutDown()
        {
            base.ShutDown();
            _views.Clear();
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

            view.ViewModelObject = null;
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

            var bindingId = new BindingId()
            {
                Type = view.ViewModelType,
                Identifier = string.IsNullOrEmpty(view.Identifier) ? null : view.Identifier
            };
            IViewModel contextViewModel = null;
            if (LFrameworkAspect.Instance.DiContainer.HasBindingId(bindingId.Type, bindingId.Identifier))
            {
                contextViewModel = LFrameworkAspect.Instance.DiContainer.Resolve(bindingId) as IViewModel;
            }
            if (contextViewModel == null)
            {
                contextViewModel = CreateViewModel(view.ViewModelType, view.Identifier);
                if (contextViewModel != null)
                {
                    LFrameworkAspect.Instance.DiContainer.Bind(view.ViewModelType).WithId(bindingId.Identifier)
                        .FromInstance(contextViewModel);
                }
            }

            if (contextViewModel == null)
            {
                Log.Fatal(
                    $"Create View Model is null. ViewModelType:{view.ViewModelType} View :{view.GetType().FullName} Identifier : {view.Identifier}");
                return;
            }

            contextViewModel.References++;
            view.ViewModelObject = contextViewModel;
        }

        private IViewModel CreateViewModel(Type type, string identifier = null)
        {
            if (ReferencePool.Acquire(type) is not IViewModel vm)
            {
                return null;
            }
            LFrameworkAspect.Instance.DiContainer.Inject(vm);
            vm.Identifier = identifier;
            vm.Initialize();
            return vm;
        }

        private void ReleaseViewModel(IViewModel viewModel, IView view)
        {
            LFrameworkAspect.Instance.DiContainer.UnbindId(view.ViewModelType, viewModel.Identifier);
            ReferencePool.Release(viewModel);
        }

        #endregion
    }
}