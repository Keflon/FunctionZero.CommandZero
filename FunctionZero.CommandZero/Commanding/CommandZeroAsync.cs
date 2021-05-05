/*
MIT License

Copyright(c) 2019 - 2021 Function Zero Ltd

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FunctionZero.CommandZero
{
    public class CommandZeroAsync : ICommandZero, INotifyPropertyChanged
    {
        private readonly IEnumerable<IGuard> _guardList;
        private readonly Func<object, bool> _canExecute;
        private readonly Func<object, Task> _executeAsync;
        private readonly Action<ICommandZero, Exception> _exceptionHandler;
        private int _raisedGuardCount;
        private bool _nameCanChange;
        /// <summary>
        /// Occurs when the target of the Command should reevaluate whether or not the Command can be executed.
        /// </summary>
        private event EventHandler CanExecuteChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public Func<string> NameGetter { get; }

        private readonly IDictionary<INotifyPropertyChanged, HashSet<string>> _observables;

        [Obsolete("Please use the Text property instead")]
        public string FriendlyName => NameGetter();
        public string Text => NameGetter();

        public CommandZeroAsync(
            IEnumerable<IGuard> guardList,
            Func<object, Task> executeAsync,
            Func<object, bool> canExecute,
            Func<string> nameGetter,
            bool nameCanChange,
            IDictionary<INotifyPropertyChanged, HashSet<string>> observables,
            Action<ICommandZero, Exception> exceptionHandler
            )
        {
            _guardList = guardList ?? throw new ArgumentNullException(nameof(guardList));
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute ?? ((o) => true);
            NameGetter = nameGetter ?? (() => string.Empty);
            _nameCanChange = nameCanChange;
            _observables = observables ?? new Dictionary<INotifyPropertyChanged, HashSet<string>>();
            _exceptionHandler = exceptionHandler ?? ((sender, ex) => { });

            foreach (var guard in _guardList)
            {
                if (guard.IsGuardRaised)
                    _raisedGuardCount++;
                guard.GuardChanged += Guard_GuardChanged;
            }

            foreach (KeyValuePair<INotifyPropertyChanged, HashSet<string>> item in _observables)
                item.Key.PropertyChanged += ObservedPropertyChanged;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add
            {
                CanExecuteChanged += value;
            }

            remove
            {
                // Breakpoint here to confirm command bindings are unsubscribing when back button pressed.
                // (in response to the binding context being set to null.)
                CanExecuteChanged -= value;
            }
        }

        private void ObservedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_observables[(INotifyPropertyChanged)sender].Contains(e.PropertyName))
                ChangeCanExecute();
        }

        public bool CanExecute(object parameter)
        {
            return (_raisedGuardCount == 0) && _canExecute(parameter);
        }

        private void Guard_GuardChanged(object sender, GuardChangedEventArgs e)
        {
            if (e.NewValue == true)
            {
                _raisedGuardCount++;
                if (_raisedGuardCount == 1)
                    this.ChangeCanExecute();
            }
            else
            {
                _raisedGuardCount--;
                if (_raisedGuardCount == 0)
                    this.ChangeCanExecute();
            }
        }

        ~CommandZeroAsync()
        {
            foreach (KeyValuePair<INotifyPropertyChanged, HashSet<string>> item in _observables)
                item.Key.PropertyChanged -= ObservedPropertyChanged;

            foreach (var guard in _guardList)
                guard.GuardChanged -= Guard_GuardChanged;
        }

        /// <summary>
        /// Used by ICommand observers.
        /// </summary>
        /// <param name="parameter"></param>
        public async void Execute(object parameter)
        {
            await ExecuteAsync(parameter);
        }

        /// <summary>
        /// Used by grownups.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public async Task<bool> ExecuteAsync(object parameter)
        {
            if (_raisedGuardCount == 0)
            {
                try
                {
                    foreach (var guard in _guardList)
                        guard.IsGuardRaised = true;
                    await _executeAsync(parameter);
                    return true;
                }
                catch (Exception ex)
                {
                    // TODO: Re-throw a suitable exception. 
                    // TODO: Either the original exception or the original exception wrapped in a GuardCommandException
                    Debug.WriteLine($"GuardCommandAsync exception. Message: {ex.Message}");

                    _exceptionHandler(this, ex);
                }
                finally
                {
                    foreach (var guard in _guardList)
                        guard.IsGuardRaised = false;
                }
            }
            return false;
        }

        public void ChangeCanExecute()
        {
            if (_nameCanChange)
            {
                OnPropertyChanged(nameof(FriendlyName));
                OnPropertyChanged(nameof(Text));
            }
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() => NameGetter();
    }
}
