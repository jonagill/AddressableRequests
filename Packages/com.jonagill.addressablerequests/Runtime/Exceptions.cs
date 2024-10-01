using System;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressableRequests
{
    public class LoadException : Exception
    {
        public readonly AsyncOperationStatus Status;

        public LoadException(AsyncOperationStatus status)
        {
            Status = status;
        }
    }

    public class InstantiationException : Exception { }

    public class MismatchedTypeException : Exception { }
}
