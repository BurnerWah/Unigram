﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Unigram.Navigation.Services;
using Windows.UI.Xaml.Navigation;

namespace Unigram.Navigation
{
    // DOCS: https://github.com/Windows-XAML/Template10/wiki/Docs-%7C-MVVM
    public abstract class ViewModelBase : BindableBase
    {
        protected virtual Task OnNavigatedToAsync(object parameter, NavigationMode mode, NavigationState state)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnNavigatedFromAsync(NavigationState suspensionState, bool suspending)
        {
            return Task.CompletedTask;
        }

        public virtual void NavigatingFrom(NavigatingEventArgs args)
        {

        }

        public virtual INavigationService NavigationService { get; set; }

        public virtual IDispatcherContext Dispatcher { get; set; }

        public virtual IDictionary<string, object> SessionState { get; set; }
    }
}