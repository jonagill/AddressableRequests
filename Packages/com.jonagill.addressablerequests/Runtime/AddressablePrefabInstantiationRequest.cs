#if UNITY_2023_3_OR_NEWER
#define ASYNC_INSTANTIATION_AVAILABLE
#endif

using System;
using Promises;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableRequests
{
    /// <summary>
    /// Wrapper around an Addressables request to load and instantiate a prefab. Allows use of
    /// Object.InstantiateAsync() on newer versions of Unity (which Addressables does not currently support), allowing for
    /// almost completely asynchronous loading and instantiation of a prefab instance.
    ///
    /// Must always have Dispose() called on it to clean up the loaded asset and unload any utilized assets.
    /// </summary>
    public class AddressablePrefabInstantiationRequest<T> : IDisposable where T : Component
    {
#region Static API

        internal static AddressablePrefabInstantiationRequest<T> InstantiateInternal(
            object key,
            Vector3? position,
            Quaternion? rotation,
            Transform parent,
            bool instantiateAsync)
        {
            return new AddressablePrefabInstantiationRequest<T>(key, position, rotation, parent, instantiateAsync);
        }
        
        internal static AddressablePrefabInstantiationRequest<T> InstantiateInternal(
            AssetReference assetReference,
            Vector3? position,
            Quaternion? rotation,
            Transform parent,
            bool instantiateAsync)
        {
#if UNITY_EDITOR
            if (!EditorValidateAssetReference(assetReference))
            {
                // Perform some additional logging to help us find errors in editor
                Debug.LogError($"No valid component of type {typeof(T).Name} found on AssetReference with backing asset: { assetReference.editorAsset }.");
            }

            if (!Application.isPlaying)
            {
                // Addressables doesn't load properly outside of Play mode, so fall back to loading synchronously
                return InstantiateSynchronouslyInternal(assetReference.editorAsset as GameObject, position, rotation, parent);
            }
#endif

            return new AddressablePrefabInstantiationRequest<T>(assetReference.RuntimeKey, position, rotation, parent, instantiateAsync);
        }

        internal static AddressablePrefabInstantiationRequest<T> InstantiateSynchronouslyInternal(
            GameObject prefab,
            Vector3? position,
            Quaternion? rotation,
            Transform parent)
        {
            if (!ValidatePrefab(prefab, out var prefabComponent))
            {
                return GetErroredRequest();
            }

            var request = new AddressablePrefabInstantiationRequest<T>();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // If we're not playing, instantiate using PrefabUtility to maintain the link to the source prefab
                request.Instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefabComponent, parent) as T;
                if (position.HasValue && rotation.HasValue && request.Instance != null)
                {
                    request.Instance.transform.position = position.Value;
                    request.Instance.transform.rotation = rotation.Value;
                }
            }
            else
#endif
            {
                if (position.HasValue && rotation.HasValue)
                {
                    request.Instance = UnityEngine.Object.Instantiate(prefabComponent, position.Value, rotation.Value, parent);
                }
                else
                {
                    request.Instance = UnityEngine.Object.Instantiate(prefabComponent, parent);
                }
            }

            request._loadPromise = new CancelablePromise<T>();
            request._loadPromise.Complete(request.Instance);

            return request;
        }

        public static AddressablePrefabInstantiationRequest<T> GetErroredRequest()
        {
            var request = new AddressablePrefabInstantiationRequest<T>();
            request._loadPromise = new CancelablePromise<T>();
            request._loadPromise.Throw(new LoadException(AsyncOperationStatus.Failed));
            return request;
        }

#endregion

        public T Instance { get; private set; }
        public IReadOnlyCancelablePromise<T> LoadPromise => _loadPromise;

        private bool _isDisposed;
        private object _key;
        private CancelablePromise<T> _loadPromise;
        private AsyncOperationHandle _addressablesLoadHandle;
        
#if ASYNC_INSTANTIATION_AVAILABLE
        private AsyncInstantiateOperation _asyncInstantiateOperation;
#endif

        private AddressablePrefabInstantiationRequest() { }

        private AddressablePrefabInstantiationRequest(
            object key,
            Vector3? position,
            Quaternion? rotation,
            Transform parent,
            bool instantiateAsync)
        {
            _key = key;
            _loadPromise = new CancelablePromise<T>();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Perform our instantiation immediately in editor
                RunInstantiation(position, rotation, parent, instantiateAsync);
                return;
            }
#endif

            // Wait until the end of the frame to begin loading
            // This gives other systems time to e.g. put up a loading screen
            // before we perform heavy load operations
            DelayInstantiation(position, rotation, parent, instantiateAsync);
        }

        private async void DelayInstantiation(
            Vector3? position,
            Quaternion? rotation,
            Transform parent,
            bool instantiateAsync)
        {
            await Awaitable.EndOfFrameAsync();
            
            if (_loadPromise.IsPending)
            {
                RunInstantiation(position, rotation, parent, instantiateAsync);
            }
        }
        
        private void RunInstantiation(Vector3? position, Quaternion? rotation, Transform parent, bool instantiateAsync)
        {
            _addressablesLoadHandle = Addressables.LoadAssetAsync<GameObject>(_key);
            _addressablesLoadHandle.Completed += loadHandle =>
            {
                if (!_loadPromise.IsPending)
                {
                    // We've been canceled -- don't attempt to spawn anything
                    return;
                }

                // Helper method used in both execution paths
                void AssignInstanceAndCompletePromise(GameObject instanceGameObject)
                {
                    Instance = instanceGameObject.GetComponent<T>();
                    if (_loadPromise.IsPending)
                    {
                        if (Instance != null)
                        {
                            _loadPromise.Complete(Instance);
                        }
                        else
                        {
                            // This GameObject didn't have the expected type -- destroy it immediately!
                            Debug.LogError($"No valid component of type {typeof(T).Name} found on loaded object {instanceGameObject}. Destroying!");
                            UnityEngine.Object.DestroyImmediate(instanceGameObject);
                            _loadPromise.Throw(new InstantiationException());
                        }
                    }
                    else
                    {
                        // We've been canceled but completed the instantiation for some reason -- destroy the object!
                        UnityEngine.Object.Destroy(instanceGameObject);
                    }
                }

                if (loadHandle.Status == AsyncOperationStatus.Succeeded &&
                     loadHandle.Result != null)
                {
                    var prefab = (GameObject) loadHandle.Result;
#if !ASYNC_INSTANTIATION_AVAILABLE
                    if (instantiateAsync)
                    {
                        Debug.LogError("Async prefab instantiation not available until Unity 2023.3");
                    }               
#endif
                    
#if ASYNC_INSTANTIATION_AVAILABLE
                    if (instantiateAsync)
                    {
                        if (position.HasValue && rotation.HasValue)
                        {
                            _asyncInstantiateOperation = UnityEngine.Object.InstantiateAsync(prefab, parent, position.Value, rotation.Value);
                        }
                        else
                        {
                            _asyncInstantiateOperation = UnityEngine.Object.InstantiateAsync(prefab, parent);
                        }

                        _asyncInstantiateOperation.completed += asyncOperation =>
                        {
                            var result = _asyncInstantiateOperation.Result;
                            if (result != null && result.Length > 0)
                            {
                                var instanceGameObject = (GameObject) result[0];
                                AssignInstanceAndCompletePromise(instanceGameObject);
                            }
                            else
                            {
                                if (_loadPromise.IsPending)
                                {
                                    _loadPromise.Throw(new InstantiationException());
                                }
                            }
                        };
                    }
                    else
#endif
                    {
                        GameObject instanceGameObject;
                        if (position.HasValue && rotation.HasValue)
                        {
                            instanceGameObject = UnityEngine.Object.Instantiate(prefab, position.Value, rotation.Value, parent);
                        }
                        else
                        {
                            instanceGameObject = UnityEngine.Object.Instantiate(prefab, parent);
                        }

                        AssignInstanceAndCompletePromise(instanceGameObject);
                    }
                }
                else
                {
                    _loadPromise.Throw(new LoadException(loadHandle.Status));
                }
            };
        }

        ~AddressablePrefabInstantiationRequest()
        {
            if (!_isDisposed)
            {
                Debug.LogError(
                    $"AddressablePrefabInstantiationRequest for instance {Instance} ({_key}) was garbage collected without being disposed. " +
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

            if (_loadPromise.IsPending)
            {
                _loadPromise.Cancel();
            }

#if ASYNC_INSTANTIATION_AVAILABLE
            if (_asyncInstantiateOperation != null && !_asyncInstantiateOperation.isDone)
            {
                _asyncInstantiateOperation.Cancel();
                _asyncInstantiateOperation = null;
            }
#endif

            if (Instance != null)
            {
                UnityEngine.Object.Destroy(Instance);
                Instance = null;
            }

            if (_addressablesLoadHandle.IsValid())
            {
                Addressables.Release(_addressablesLoadHandle);
            }

            _isDisposed = true;
        }

        private static bool ValidatePrefab(GameObject prefab, out T component)
        {
            if (prefab == null)
            {
                component = null;
                return false;
            }

            component = prefab.GetComponent<T>();
            if (component == null)
            {
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private static bool EditorValidateAssetReference(AssetReference assetReference)
        {
            return ValidatePrefab(assetReference.editorAsset as GameObject, out _);
        }
#endif
    }
}
