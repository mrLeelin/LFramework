using System;
using UniRx;
using UnityEngine;

namespace LFramework.Runtime
{
    public abstract class ViewBehaviour : UnityEngine.MonoBehaviour, IView
    {
        private IViewModel _viewModel;
        private string _identifier;
        private CompositeDisposable _compositeDisposable;
        
        public virtual string Identifier
        {
            get
            {
                if (string.IsNullOrEmpty(_identifier))
                {
                    _identifier = Guid.NewGuid().ToString();
                }

                return _identifier;
            }
            set
            {
                if (_viewModel != null && _viewModel.Identifier == value)
                {
                    return;
                }

                _identifier = value;
            }
        }

        public CompositeDisposable CompositeDisposable => _compositeDisposable;

        public virtual IViewModel ViewModelObject
        {
            get => _viewModel;
            set
            {
                if (_viewModel == value)
                {
                    return;
                }

                _viewModel = value;
                if (_viewModel == null)
                {
                    //_bindingContext.Dispose();
           
                    UnBind();
                    return;
                }

                _identifier = value.Identifier;
             
                // TODO>>> lin: 确认是否使用UniRx
                /*
                _bindingContext = new BindingContext(this,
                    LFrameworkAspect.Instance.GetComponent<ViewComponent>().Binder);
                _bindingContext.DataContext = _viewModel;
                */
                
                OnInitViewModel(value);
                PreBind();
                Bind(value,_compositeDisposable);
                AfterBind();
            }
        }

        public virtual Type ViewModelType { get; } = null;
        public  void OnViewBeCreate()
        {
            _compositeDisposable = new CompositeDisposable();
        }

        public void OnViewBeDestroy()
        {
            if (_compositeDisposable is { IsDisposed: false })
            {
                _compositeDisposable.Dispose();
            }
        }


        protected virtual void OnInitViewModel(IViewModel viewModel)
        {
            
        }

        protected virtual void PreBind()
        {
        }

        protected virtual void Bind(IViewModel viewModel, CompositeDisposable compositeDisposable)
        {
          
        }

        protected virtual void AfterBind()
        {
            
        }

        protected virtual void UnBind()
        {
            
        }
        
        
        
    }
}