# FunctionZero.CommandZeroAsync
Fully featured ICommand implementation


## Usage

CommandZeroAsync uses fluent API to build instances quickly and easily, like this:  
```csharp
ICommand RedPillCommand = new CommandBuilder()
                .SetExecute(async() => await DoSomething())
                .SetCanExecute(() => CanDoSomething())
                .AddGuard(this)
                .SetName("Take the Red Pill")
                // More builder methods can go here ...
                .Build(); 
```

Many Builder methods have sensible overloads, for example SetExecute and SetCanExecute can take a `CommandParameter`:
```csharp
RedPillCommand = new CommandBuilder()
                .SetExecute(async(obj) => await DoSomething(obj))
                .SetCanExecute((obj) => CanDoSomething(obj))
                ...
```

## IGuard
Every `Command` that shares the same `IGuard` implementation will be disabled if **any one of them** is performing a long-running task  
In the following example, assuming a `Button` is bound to `GetDataCommandExecute` and another `Button` is bound to `NextCommand`, 
clicking the 'Get Data' button will disable **both** Commands, and therefore **both** `Buttons`, for 5 seconds
```csharp
public class HomePageVm : BaseVm
{
        // UI binds to these commands ...
        public CommandZeroAsync GetDataCommand { get; }
        public CommandZeroAsync NextCommand { get; }

        private IPageServiceZero _pageService;
    
        public HomePageVm(PageServiceZero pageService)
        {
            _pageService = pageService;
            IGuard pageGuard = new BasicGuard();

            GetDataCommand = new CommandBuilder()
                                        .AddGuard(pageGuard)
                                        .SetExecute(GetDataCommandExecute)
                                        .SetName("Get Data")
                                        .Build();
            NextCommand = new CommandBuilder()
                                        .AddGuard(pageGuard)
                                        .SetExecute(NextCommandExecute)
                                        .SetName("Next")
                                        .Build();
        }

        private async Task GetDataCommandExecute()
        {
            // Simulate a long-running task ...
            await Task.Delay(5000);
        }

        private async Task NextCommandExecute()
        {
            // Subtle plug for FunctionZero.MvvmZero v2.0.0
            await _pageService.PushPageAsync<ResultsPage, ResultsPageVm>((vm)=>vm.SetState("Message from HomePageVm!!"));
        }
}
```

If your `ViewModel` implements IGuard, that simply becomes **`.AddGuard(this)`**

## Command FriendlyName
`.SetName(string name)` sets a `FriendlyName` property on the `Command` that the UI can bind to  
`.SetName(Func<string>)` sets a `FriendlyName` method on the `Command` that the UI can bind to
```xaml
<Button Command="{Binding NextCommand}" Text="{Binding NextCommand.FriendlyName}" />
```

## Automatically calling ChangeCanExecute
If there is need to re-evaluate the result of `CanExecute`, it is up to the developer to call `ChangeCanExecute` 
so UI (usually a `Button`) can update its `IsEnabled` flag. This is often done in an `OnPropertyChanged` overload on the `ViewModel`  
Alternatively, you can call `.AddObservedProperty` to specify the property or properties that will trigger such a re-evaluation
**Caution** May leak if you *recycle* your `ViewModel` and your `Commands` are built outside of your constructor, 
or if you specify a property on an object outside the scope of your `ViewModel`  
```csharp
// Any Buttons bound to this command will refresh their IsEnabled flag if IsBusy or IsFaulted changes. 
// Note: IsBusy must raise INotifyPropertyChanged
DoSomethingCommand = new CommandBuilder()
                            .SetCanExecute(() => IsBusy || IsFaulted)
                            .SetExecute(()=>{...})
                            .AddObservedProperty(nameof(IsBusy), nameof(IsFaulted))
                            .SetName("Do something")
                            .Build();
```

## Builder methods:
```csharp
AddGlobalGuard()
```
Adds a global guard implementation. Commands that share a guard cannot execute concurrently.  
Commands can be given multiple guard implementations, though individual guard implementations
can only be added once  
*CAUTION* Watch out for deadlock if you use the same Guard across multiple Pages.  
**Recommendation:** Implement `IGuard` in your ViewModel base class, e.g. by delegating to an instance of BasicGuard, so you can use the ViewModel ('this') as your Guard.   

```csharp
AddGuard(IGuard guard)
```
Adds a guard implementation. Commands that share a guard cannot execute concurrently.  
Commands can be given multiple guard implementations, though individual guard implementations
can only be added once  
*CAUTION* Watch out for deadlock if you use the same Guard across multiple Pages.  
**Recommendation:** Implement `IGuard` in your `ViewModel` base class, e.g. by delegating to an instance of `BasicGuard`, so you can use the '`this`' as your Guard.  
  
```csharp
AddObservedProperty(INotifyPropertyChanged propertySource, params string[] propertyNames)
```
The command can automatically re-evaluate the `CanExecute` delegate when a specified property changes,  
allowing any UI controls that are bound to the Command to update their IsEnabled status.  
**propertySource** : An object that supports `INotifyPropertyChanged`  
**propertyName** : The name of a property on `propertySource`  
**Caution** May leak if you *recycle* your `ViewModel` and your `Commands` are built outside of your constructor, 
or if you specify a property on an object outside the scope of your `ViewModel`
```csharp
AddObservedProperty(INotifyPropertyChanged propertySource, string propertyName)
```
The command can automatically re-evaluate the `CanExecute` delegate when a specified property changes,  
allowing any UI controls that are bound to the Command to update their IsEnabled status.  
**propertySource** : An object that supports `INotifyPropertyChanged`  
**propertyNames** : Comma separated list of `propertyNames` found on `propertySource`  
**Caution** May leak if you *recycle* your `ViewModel` and your `Commands` are built outside of your constructor, 
or if you specify a property on an object outside the scope of your `ViewModel`
```csharp
CommandZeroAsync Build();
```
Build the Command :)
```csharp
CommandBuilder SetCanExecute(Func<bool> canExecute)
```
Set a CanExecute callback that does not require a parameter
```csharp
SetCanExecute(Func<object, bool> canExecute)
```
Set a CanExecute callback that requires a parameter
```csharp
SetExecute(Action execute)
```
Set a synchonous Execute callback that does not require a parameter. Prefer the `async` overload!
```csharp
SetExecute(Action<object> execute)
```
Set a synchonous Execute callback that requires a parameter. Prefer the `async` overload!
```csharp
SetExecute(Func<object, Task> execute)
```
Set an asynchronous Execute callback that requires a parameter
```csharp
SetExecute(Func<Task> execute)
```
Set an asynchronous Execute callback that does not require a parameter
```csharp
SetName(Func<string> getName)
```
Sets a delegate that can be used to retrieve the name of the Command. The UI can then bind to the `FriendlyName` property
Useful for internationalisation
```csharp
SetName(string name)
```
Sets the name of the Command. The UI can then bind to the `FriendlyName` property
Useful for internationalisation
