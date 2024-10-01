using System;
using Promises;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableRequests
{
    /// <summary>
    /// Wrapper around an Addressables request to load an asset. Allows use of
    /// Must always have Dispose() called on it to unload the loaded asset and any dependencies.
    /// </summary>
    public class AddressableLoadRequest<T> : IDisposable where T : UnityEngine.Object
    {
#region Static API

        internal static AddressableLoadRequest<T> LoadInternal(object key)
        {
            return new AddressableLoadRequest<T>(key);
        }
        
        internal static AddressableLoadRequest<T> LoadInternal(AssetReference assetReference)
        {
#if UNITY_EDITOR
            if (!EditorValidateAssetReference(assetReference))
            {
                // Perform some additional logging to help us find errors in editor
                Debug.LogError($"No valid asset of type {typeof(T).Name} found on AssetReference with backing asset: { assetReference.editorAsset }.");
            }

            if (!Application.isPlaying)
            {
                // Addressables doesn't load properly outside of Play mode, so just return the underlying asset synchronously
                AddressableLoadRequest<T> request;
                var result = assetReference.editorAsset as T;
                if (result != null)
                {
                    request = new AddressableLoadRequest<T>();
                    request.Result = assetReference.editorAsset as T;
                    request._loadPromise = new CancelablePromise<T>();
                    request._loadPromise.Complete(request.Result);
                }
                else
                {
                    request = GetErroredRequest();
                }
                
                return request;
            }
#endif

            return new AddressableLoadRequest<T>(assetReference.RuntimeKey);
        }
        

        public static AddressableLoadRequest<T> GetErroredRequest()
        {
            var request = new AddressableLoadRequest<T>();
            request._loadPromise = new CancelablePromise<T>();
            request._loadPromise.Throw(new LoadException(AsyncOperationStatus.Failed));
            return request;
        }

#endregion

        public T Result { get; private set; }
        public IReadOnlyCancelablePromise<T> LoadPromise => _loadPromise;

        private bool _isDisposed;
        private object _key;
        private CancelablePromise<T> _loadPromise;
        private AsyncOperationHandle _addressablesLoadHandle;
        
        private AddressableLoadRequest() { }

        private AddressableLoadRequest(object key)
        {
            _key = key;
            _loadPromise = new CancelablePromise<T>();

            _addressablesLoadHandle = Addressables.LoadAssetAsync<T>(_key);
            _addressablesLoadHandle.Completed += loadHandle =>
            {
                if (!_loadPromise.IsPending)
                {
                    // We've been canceled -- don't attempt to spawn anything
                    return;
                }

                if (loadHandle.Status == AsyncOperationStatus.Succeeded &&
                    loadHandle.Result != null)
                {
                    var result = loadHandle.Result as T;
                    if (result != null)
                    {
                        Result = result;
                        _loadPromise.Complete(result);
                    }
                    else
                    {
                        _loadPromise.Throw(new MismatchedTypeException());
                    }
                }
                else
                {
                    _loadPromise.Throw(new LoadException(loadHandle.Status));
                }
            };
        }
        

        ~AddressableLoadRequest()
        {
            if (!_isDisposed)
            {
                Debug.LogError(
                    $"AddressableLoadRequest for asset {Result} ({_key}) was garbage collected without being disposed. " +
                            $"This will leak memory!");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _key = null;
            Result = null;

            if (_loadPromise.IsPending)
            {
                _loadPromise.Cancel();
            }

            if (_addressablesLoadHandle.IsValid())
            {
                Addressables.Release(_addressablesLoadHandle);
            }

            _isDisposed = true;
        }

#if UNITY_EDITOR
        private static bool EditorValidateAssetReference(AssetReference assetReference)
        {
            return assetReference.editorAsset != null && assetReference.editorAsset is T;
        }
#endif
    }
}
