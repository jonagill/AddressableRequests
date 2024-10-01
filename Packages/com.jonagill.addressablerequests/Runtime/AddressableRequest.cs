using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AddressableRequests
{
    /// <summary>
    /// Static API for constructing and accessing Addressable wrapper classes
    /// </summary>
    public static class AddressableRequest
    {
#region Asset Loading
        internal static AddressableLoadRequest<T> LoadAsset<T>(object key) where T : UnityEngine.Object
        {
            return AddressableLoadRequest<T>.LoadInternal(key);
        }

        internal static AddressableLoadRequest<T> LoadAsset<T>(AssetReference assetReference) where T : UnityEngine.Object
        {
            return AddressableLoadRequest<T>.LoadInternal(assetReference);
        }

#endregion
        
#region Prefab Instantiation

        /// <summary>
        /// Load and instantiate the Addressable prefab asset with the given key.
        /// </summary>
        public static AddressablePrefabInstantiationRequest<T> InstantiatePrefab<T>(
            object key,
            Transform parent = null) where T : Component
        {
            return AddressablePrefabInstantiationRequest<T>.InstantiateInternal(key, null, null, parent, false);
        }
        
        /// <summary>
        /// Load and instantiate the Addressable prefab asset with the given key.
        /// Uses the new Object.InstantiateAsync() API to perform most of the instantiation off of the main thread.
        /// </summary>
        public static AddressablePrefabInstantiationRequest<T> InstantiatePrefabAsync<T>(
            object key,
            Transform parent = null) where T : Component
        {
            return AddressablePrefabInstantiationRequest<T>.InstantiateInternal(key, null, null, parent, true);
        }
        
        /// <summary>
        /// Load and instantiate the Addressable prefab asset with the given key.
        /// </summary>
        public static AddressablePrefabInstantiationRequest<T> InstantiatePrefab<T>(
            object key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null) where T : Component
        {
            return AddressablePrefabInstantiationRequest<T>.InstantiateInternal(key, position, rotation, parent, false);
        }
        
        /// <summary>
        /// Load and instantiate the Addressable prefab asset with the given key.
        /// Uses the new Object.InstantiateAsync() API to perform most of the instantiation off of the main thread.
        /// </summary>
        public static AddressablePrefabInstantiationRequest<T> InstantiatePrefabAsync<T>(
            object key,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null) where T : Component
        {
            return AddressablePrefabInstantiationRequest<T>.InstantiateInternal(key, position, rotation, parent, true);
        }
        
        /// <summary>
        /// Load and instantiate the Addressable prefab asset from the given AssetReference.
        /// </summary>
        public static AddressablePrefabInstantiationRequest<T> InstantiatePrefab<T>(
            AssetReference assetReference,
            Transform parent = null) where T : Component
        {
            return AddressablePrefabInstantiationRequest<T>.InstantiateInternal(assetReference, null, null, parent, false);
        }
        
        /// <summary>
        /// Load and instantiate the Addressable prefab asset from the given AssetReference.
        /// Uses the new Object.InstantiateAsync() API to perform most of the instantiation off of the main thread.
        /// </summary>
        public static AddressablePrefabInstantiationRequest<T> InstantiatePrefabAsync<T>(
            AssetReference assetReference,
            Transform parent = null) where T : Component
        {
            return AddressablePrefabInstantiationRequest<T>.InstantiateInternal(assetReference, null, null, parent, true);
        }

        /// <summary>
        /// Load and instantiate the Addressable prefab asset from the given AssetReference.
        /// </summary>
        public static AddressablePrefabInstantiationRequest<T> InstantiatePrefab<T>(
            AssetReference assetReference,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null) where T : Component
        {
            return AddressablePrefabInstantiationRequest<T>.InstantiateInternal(assetReference, position, rotation, parent, false);
        }
        
        /// <summary>
        /// Load and instantiate the Addressable prefab asset from the given AssetReference.
        /// Uses the new Object.InstantiateAsync() API to perform most of the instantiation off of the main thread.
        /// </summary>
        public static AddressablePrefabInstantiationRequest<T> InstantiatePrefabAsync<T>(
            AssetReference assetReference,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null) where T : Component
        {
            return AddressablePrefabInstantiationRequest<T>.InstantiateInternal(assetReference, position, rotation, parent, true);
        }
        
        /// <summary>
        /// Wrap a synchronous instantiation with our own API.
        /// Should generally only be used for editor tooling and error fallbacks.
        /// </summary>
        public static AddressablePrefabInstantiationRequest<T> InstantiateSynchronously<T>(
            GameObject prefab, 
            Transform parent = null) where T : Component
        {
            return AddressablePrefabInstantiationRequest<T>.InstantiateSynchronouslyInternal(prefab, null, null, parent);
        }

        /// <summary>
        /// Wrap a synchronous instantiation with our own API.
        /// Should generally only be used for editor tooling and error fallbacks.
        /// </summary>
        public static AddressablePrefabInstantiationRequest<T> InstantiateSynchronously<T>(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null) where T : Component
        {
            return AddressablePrefabInstantiationRequest<T>.InstantiateSynchronouslyInternal(prefab, position, rotation, parent);
        }
        
#endregion
    }
}
