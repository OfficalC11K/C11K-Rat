// Dosya: Modules/IRemoteModule.cs
using System;

namespace App.Service
{
    public interface IRemoteModule
    {
        void Initialize(ClientSession s, Action<string> send);
    }
}