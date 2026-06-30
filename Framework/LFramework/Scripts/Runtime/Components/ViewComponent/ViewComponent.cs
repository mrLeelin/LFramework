using System;
using System.Collections.Generic;
using GameFramework;
using UnityGameFramework.Runtime;

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

            var viewModel = view.ViewModelObject;
            view.ViewModelObject = null;
            if (viewModel != null && --viewModel.References == 0)
            {
                ReleaseViewModel(viewModel, view);
            }

            var isRemoved = _views.Remove(view);
            if (!isRemoved)
            {
                Log.Error(
                    $"View '{view.GetType().FullName}' not exists in list of view. so it was not removed.");
            }

            view.OnViewBeDestroy();
        }

        private void FetchViewModel(IView view)
        {
            if (view.ViewModelObject != null)
            {
                return;
            }

            var identifier = string.IsNullOrEmpty(view.Identifier) ? null : view.Identifier;
            IViewModel contextViewModel = null;
            if (LServices.TryGet(view.ViewModelType, identifier, out var cachedViewModel))
            {
                contextViewModel = cachedViewModel as IViewModel;
            }

            if (contextViewModel == null)
            {
                contextViewModel = CreateViewModel(view.ViewModelType, view.Identifier);
                if (contextViewModel != null)
                {
                    LServices.Register(view.ViewModelType, identifier, contextViewModel);
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
            if (ReferencePool.Acquire(type) is not IViewModel viewModel)
            {
                return null;
            }

            LServices.Inject(viewModel);
            viewModel.Identifier = identifier;
            viewModel.Initialize();
            return viewModel;
        }

        private void ReleaseViewModel(IViewModel viewModel, IView view)
        {
            LServices.Unregister(view.ViewModelType, viewModel.Identifier);
            ReferencePool.Release(viewModel);
        }
    }
}
