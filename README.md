# Addressable Requests
This library provides a helper API for interfacing with Unity Addressable asset requests. It provides the following benefits on top of interacting with `Addressables` directly:

* All load requests are backed by my [Promises](https://github.com/jonagill/Promises) library, providing a convenient method for getting notified when your assets are loaded and chaining additional behavior.
* All load requests will log errors if they fall out of scope without being disposed, helping make sure you release all of your requests to avoid memory leaks
* Supports intantiating prefabs with `Object.InstantiateAsync()` on Unity 2023.3 and newer, heavily reducing the amount of time instantiation spends on the main thread.

## Installation
Addresable Requests relies on my [Promises](https://github.com/jonagill/Promises) library for providing callbacks when routines complete. We recommend you install both libraries via [OpenUPM](https://openupm.com/packages/com.jonagill.addressablerequests/). Per OpenUPM's documentation:

1. Open `Edit/Project Settings/Package Manager`
2. Add a new Scoped Registry (or edit the existing OpenUPM entry) to read:
    * Name: `package.openupm.com`
    * URL: `https://package.openupm.com`
    * Scope(s): `com.jonagill.addressablerequests` and `com.jonagill.promises`
3. Click Save (or Apply)
4. Open Window/Package Manager
5. Click the + button
6. Select `Add package by name...` or `Add package from git URL...` 
7. Enter `com.jonagill.promises` and click Add
8. Repeat steps 6 and 7 with `com.jonagill.addressablerequests`

# Usage
All methods for access are exposed via static methods on the `AddressableRequest` class. You can then use `request.LoadPromise` with the entire [Promises](https://github.com/jonagill/Promises) API to get notified when your request completes.

All requests are `IDisposables`, so just call `request.Dispose()` when you're done to clean up all of your loaded and instantiated assets.