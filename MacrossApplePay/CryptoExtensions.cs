using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace Macross
{
    internal static class CryptoExtensions
    {
        public static byte[] DeriveKeyMaterial(
            this ECDiffieHellmanCng provider,
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            byte[] algorithmId,
            byte[] partyUInfo,
            byte[] partyVInfo)
        {
            Collection<IntPtr> ResourcesToFree = new Collection<IntPtr>();
            try
            {
                using SafeNCryptSecretHandle Agreement = provider.DeriveSecretAgreementHandle(otherPartyPublicKey);

                IntPtr AlgorithmIdPtr = Marshal.AllocHGlobal(algorithmId.Length);
                ResourcesToFree.Add(AlgorithmIdPtr);
                Marshal.Copy(algorithmId, 0, AlgorithmIdPtr, algorithmId.Length);

                IntPtr PartyUPtr = Marshal.AllocHGlobal(partyUInfo.Length);
                ResourcesToFree.Add(PartyUPtr);
                Marshal.Copy(partyUInfo, 0, PartyUPtr, partyUInfo.Length);

                IntPtr PartyVPtr = Marshal.AllocHGlobal(partyVInfo.Length);
                ResourcesToFree.Add(PartyVPtr);
                Marshal.Copy(partyVInfo, 0, PartyVPtr, partyVInfo.Length);

                NativeMethods.NCryptBuffer[] Buffers = new NativeMethods.NCryptBuffer[]
                {
                    new NativeMethods.NCryptBuffer
                    {
                        cbBuffer = (uint)algorithmId.Length,
                        BufferType = NativeMethods.BufferType.KDF_ALGORITHMID,
                        pvBuffer = AlgorithmIdPtr
                    },
                    new NativeMethods.NCryptBuffer
                    {
                        cbBuffer = (uint)partyUInfo.Length,
                        BufferType = NativeMethods.BufferType.KDF_PARTYUINFO,
                        pvBuffer = PartyUPtr
                    },
                    new NativeMethods.NCryptBuffer
                    {
                        cbBuffer = (uint)partyVInfo.Length,
                        BufferType = NativeMethods.BufferType.KDF_PARTYVINFO,
                        pvBuffer = PartyVPtr
                    }
                };

                IntPtr BufferPtr = Marshal.AllocHGlobal(Buffers.Length * Marshal.SizeOf(typeof(NativeMethods.NCryptBuffer)));

                ResourcesToFree.Add(BufferPtr);

                IntPtr Location = BufferPtr;
                for (int i = 0; i < Buffers.Length; i++)
                {
                    Marshal.StructureToPtr(Buffers[i], Location, false);
                    Location = new IntPtr(Location.ToInt64() + Marshal.SizeOf(typeof(NativeMethods.NCryptBuffer)));
                }

                NativeMethods.NCryptBufferDesc ParameterList = new NativeMethods.NCryptBufferDesc
                {
                    cBuffers = Buffers.Length,
                    pBuffers = BufferPtr
                };

                byte[] DerivedKey = new byte[32];

                NativeMethods.ErrorCode ErrorCode = NativeMethods.NCryptDeriveKey(
                    Agreement,
                    "SP800_56A_CONCAT",
                    ref ParameterList,
                    DerivedKey,
                    DerivedKey.Length,
                    out int NumberOfBytesDerived,
                    0);

                if (ErrorCode != NativeMethods.ErrorCode.Success)
                    throw new InvalidOperationException($"KeyMaterial could not be derived. ErrorCode [{ErrorCode}], Win32Error [{Marshal.GetLastWin32Error()}] returned.");

                if (NumberOfBytesDerived != 32)
                    throw new InvalidOperationException("KeyMaterial size was invalid.");

                return DerivedKey;
            }
            finally
            {
                foreach (IntPtr Resource in ResourcesToFree)
                {
                    Marshal.FreeHGlobal(Resource);
                }
            }
        }
    }
}
