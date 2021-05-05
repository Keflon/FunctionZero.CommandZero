/*
MIT License

Copyright(c) 2019 - 2020 Function Zero Ltd

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
using System.Threading.Tasks;

namespace FunctionZero.CommandZero
{
    /// <summary>
    /// A Builder for a CommandZeroAsync instance
    /// </summary>
    public class CommandBuilder : ICommandBuilder
    {
        private Func<object, Task> _execute;
        private Func<object, bool> _predicate;
        private IList<IGuard> _guardList;
        private Func<string> _getName;
        private bool _hasBuilt;
        private IDictionary<INotifyPropertyChanged, HashSet<string>> _observedProperties;
        private bool _nameCanChange;
        private Action<ICommandZero, Exception> _exceptionHandler;

        /// <summary>
        /// This is a global implementation if IGuard that can optionally be used by commands
        /// </summary>
        public static IGuard GlobalGuard { get; } = new BasicGuard();

        /// <summary>
        /// CommandBuilder ctor
        /// </summary>
        public CommandBuilder()
        {
            _guardList = new List<IGuard>();
            _observedProperties = new Dictionary<INotifyPropertyChanged, HashSet<string>>();
        }

        /// <summary>
        /// Build the Command! :)
        /// </summary>
        /// <returns></returns>
        public CommandZeroAsync Build()
        {
            if (_hasBuilt)
                throw new InvalidOperationException("This CommandBuilder has expired. You cannot call Build more than once.");
            _hasBuilt = true;
            return new CommandZeroAsync(_guardList, _execute, _predicate, _getName, _nameCanChange, _observedProperties, _exceptionHandler);
        }

        /// <summary>
        /// Set an asynchronous Execute callback that requires a parameter
        /// </summary>
        /// <param name="execute">An asynchronous Execute callback that requires a parameter</param>
        /// <returns></returns>
        public CommandBuilder SetExecuteAsync(Func<object, Task> execute)
        {
            if (_execute != null)
                throw new NotSupportedException("SetExecute cannot be called more than once");
            _execute = execute;
            return this;
        }

        /// <summary>
        /// Set an asynchronous Execute callback that does not require a parameter
        /// </summary>
        /// <param name="execute">An asynchronous Execute callback that requires a parameter</param>
        /// <returns></returns>
        public CommandBuilder SetExecuteAsync(Func<Task> execute)
        {
            if (_execute != null)
                throw new NotSupportedException("SetExecute cannot be called more than once");
            _execute = (o) => execute();
            return this;
        }

        /// <summary>
        /// Set a synchonous Execute callback that does not require a parameter
        /// Prefer SetExecuteAsync
        /// </summary>
        /// <param name="execute">A synchonous Execute callback that does not require a parameter</param>
        /// <returns></returns>
        public CommandBuilder SetExecute(Action execute)
        {
            if (_execute != null)
                throw new NotSupportedException("SetExecute cannot be called more than once");
            _execute = (o) => { execute(); return Task.CompletedTask; };
            return this;
        }

        /// <summary>
        /// Set a synchonous Execute callback that requires a parameter
        /// Prefer SetExecuteAsync
        /// </summary>
        /// <param name="execute">A synchonous Execute callback that requires a parameter</param>
        /// <returns></returns>
        public CommandBuilder SetExecute(Action<object> execute)
        {
            if (_execute != null)
                throw new NotSupportedException("SetExecute cannot be called more than once");
            _execute = (o) => { execute(o); return Task.CompletedTask; };
            return this;
        }

        /// <summary>
        /// Set a CanExecute callback that requires a parameter
        /// </summary>
        /// <param name="canExecute">A CanExecute callback that requires a parameter</param>
        /// <returns></returns>
        public CommandBuilder SetCanExecute(Func<object, bool> canExecute)
        {
            if (_predicate != null)
                throw new NotSupportedException("SetCanExecute cannot be called more than once");
            _predicate = canExecute;
            return this;
        }

        /// <summary>
        /// Set a CanExecute callback that does not require a parameter
        /// </summary>
        /// <param name="canExecute">A CanExecute callback that does not require a parameter</param>
        /// <returns></returns>
        public CommandBuilder SetCanExecute(Func<bool> canExecute)
        {
            if (_predicate != null)
                throw new NotSupportedException("SetCanExecute cannot be called more than once");
            _predicate = (o) => canExecute();
            return this;
        }

        /// <summary>
        /// Adds a global guard implementation. Commands that share a guard cannot execute concurrently.<br/>
        /// Commands can be given multiple guard implementations, though individual guard implementations
        /// can only be added once<br/>
        /// *CAUTION* Watch out for deadlock if you use the same Guard across multiple Pages.<br/>
        /// Recommendation: Implement IGuard in your ViewModel base class, e.g. by delegating to an instance of BasicGuard, so you can use the ViewModel as your Guard.<br/>
        /// </summary>
        /// <returns></returns>
        public CommandBuilder AddGlobalGuard()
        {
            if (_guardList.Contains(GlobalGuard))
                throw new ArgumentException("Cannot add the global guard to the same command twice");
            _guardList.Add(GlobalGuard);
            return this;
        }

        /// <summary>
        /// Adds a guard implementation. Commands that share a guard cannot execute concurrently.<br/>
        /// Commands can be given multiple guard implementations, though individual guard implementations
        /// can only be added once<br/>
        /// *CAUTION* Watch out for deadlock if you use the same Guard across multiple Pages.<br/>
        /// Recommendation: Implement IGuard in your ViewModel base class, e.g. by delegating to an instance of BasicGuard, so you can use the ViewModel as your Guard.<br/>
        /// </summary>
        /// <param name="guard">A guard implementation to add to the Command being built</param>
        /// <returns></returns>
        public CommandBuilder AddGuard(IGuard guard)
        {
            if (_guardList.Contains(guard))
                throw new ArgumentException("Cannot add the same guard to the same command twice");
            _guardList.Add(guard);
            return this;
        }

        /// <summary>
        /// Sets a delegate that can be used to retrieve the name of the Command. The UI can then bind to the <c>FriendlyName</c> property.
        /// Useful for swapping language at runtime
        /// </summary>
        /// <param name="getName">A delegate that returns a friendly name for the Command</param>
        /// <returns></returns>
        public CommandBuilder SetName(Func<string> getName)
        {
            if (_getName != null)
                throw new NotSupportedException("SetName cannot be called more than once");
            _getName = getName;
            _nameCanChange = true;
            return this;
        }

        /// <summary>
        /// Sets the name of the Command. The UI can then bind to the <c>FriendlyName</c> property.
        /// </summary>
        /// <param name="name">The friendly name for the Command</param>
        /// <returns></returns>
        public CommandBuilder SetName(string name)
        {
            if (_getName != null)
                throw new NotSupportedException("SetName cannot be called more than once");
            _getName = () => name;
            _nameCanChange = false;
            return this;
        }

        /// <summary>
        /// Optional.<br/>
        /// The command can automatically re-evaluate the <c>CanExecute</c> delegate when a specified property changes,<br/>
        /// allowing any UI controls that are bound to the Command to update their IsEnabled status.
        /// </summary>
        /// <param name="propertySource">An object that supports <c>INotifyPropertyChanged</c></param>
        /// <param name="propertyName">The name of a property on <c>propertySource</c></param>
        /// <returns></returns>
        public CommandBuilder AddObservedProperty(INotifyPropertyChanged propertySource, string propertyName)
        {
            return this.AddObservedProperty(propertySource, new string[] { propertyName });
        }

        /// <summary>
        /// The command can automatically re-evaluate the <c>CanExecute</c> delegate when a specified property changes,<br/>
        /// allowing any UI controls that are bound to the Command to update their IsEnabled status.
        /// </summary>
        /// <param name="propertySource">An object that supports <c>INotifyPropertyChanged</c></param>
        /// <param name="propertyNames">A comma separated list or string[] of property names on <c>propertySource</c></param>
        /// <returns></returns>
        public CommandBuilder AddObservedProperty(INotifyPropertyChanged propertySource, params string[] propertyNames)
        {
            if (propertySource == null)
                throw new ArgumentException("Cannot be null or empty", nameof(propertySource));

            if (propertyNames == null)
                throw new ArgumentException("Cannot be null or empty", nameof(propertyNames));

            if (_observedProperties.ContainsKey(propertySource) == false)
                _observedProperties.Add(propertySource, new HashSet<string>());

            foreach (string propertyName in propertyNames)
                _observedProperties[propertySource].Add(propertyName);

            return this;
        }

        public CommandBuilder SetExceptionHandler(Action<ICommandZero, Exception> exceptionHandler)
        {
            if (_exceptionHandler != null)
                throw new NotSupportedException("SetExceptionHandler cannot be called more than once");
            _exceptionHandler = exceptionHandler;
            return this;
        }

        [Obsolete("Please use SetExecuteAsync")]
        public CommandBuilder SetExecute(Func<object, Task> execute)
        {
            SetExecuteAsync(execute);
            return this;
        }

        [Obsolete("Please use SetExecuteAsync")]
        public CommandBuilder SetExecute(Func<Task> execute)
        {
            SetExecuteAsync(execute);
            return this;
        }
    }
}
